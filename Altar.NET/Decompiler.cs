using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Altar
{
    public unsafe static class Decompiler
    {
        struct CodeBlock
        {
            public AnyInstruction*[] Instructions;
            public BranchType Type;
            public AnyInstruction* BranchTo;
        }

        readonly static GraphBranch[] EmptyGBArray   = { };
        readonly static GraphVertex[] EmptyGVArray   = { };
        readonly static Expression [] EmptyExprArray = { };

        static int ComparePtrs(IntPtr a, IntPtr b) => a.ToInt64().CompareTo(b.ToInt64());
        static int IndexOfPtr(AnyInstruction*[] arr, AnyInstruction* elem)
        {
            for (int i = 0; i < arr.Length; i++)
                if (elem == arr[i])
                    return i;

            return -1;
        }
        static int IndexOfPtr(AnyInstruction*[] arr, IntPtr ptr) => IndexOfPtr(arr, (AnyInstruction*)ptr);
        static AnyInstruction*[] MPtrListToPtrArr(IList<IntPtr> l)
        {
            var r = new AnyInstruction*[l.Count];

            for (int i = 0; i < r.Length; i++)
                r[i] = (AnyInstruction*)l[i];

            return r;
        }

        static AnyInstruction*[] FindJumpOffsets(CodeInfo code)
        {
            var blocks = new List<CodeBlock>();
            var instr  = code.Instructions;

            var ret = new List<IntPtr>();

            ret.Add((IntPtr)instr[0]);

            for (int i = 0; i < instr.Length; i++)
            {
                var ins = instr[i];
                if (ins->Kind() == InstructionKind.Goto)
                {
                    IntPtr p;
                    // instructions after a 'goto'
                    if (i < instr.Length - 1)
                    {
                        p = (IntPtr)instr[i + 1];

                        if (!ret.Contains(p))
                            ret.Add(p);
                    }

                    // goto targets
                    p = (IntPtr)((long)ins + ins->Goto.Offset);

                    if (!ret.Contains(p))
                        ret.Add(p);
                }
            }

            ret.Sort(ComparePtrs);

            var ret_ = new AnyInstruction*[ret.Count];

            for (int i = 0; i < ret_.Length; i++)
                ret_[i] = (AnyInstruction*)ret[i];

            return ret_;
        }
        static CodeBlock[] SplitBlocks (CodeInfo code)
        {
            var blocks = new List<CodeBlock>();
            var instr = code.Instructions;

            var jumpTo = FindJumpOffsets(code);

            for (int i = 0; i < jumpTo.Length; i++)
            {
                var instrs = new List<IntPtr>();
                var br = jumpTo[i];
                var ind = IndexOfPtr(instr, br);
                AnyInstruction* nextAddr = null;

                for (var j = ind; j != -1 && j < instr.Length; j++)
                {
                    var ins = instr[j];

                    if (i != jumpTo.Length - 1 && ins == jumpTo[i + 1]) // sorted
                    {
                        nextAddr = ins;
                        break;
                    }

                    instrs.Add((IntPtr)ins);
                }

                if (i == jumpTo.Length - 1 && br > instr[instr.Length - 1]) // implicit 'ret' after last instruction
                {
                    if (instrs.Count != 0)
                        blocks.Add(new CodeBlock
                        {
                            Instructions = MPtrListToPtrArr(instrs),
                            BranchTo     = null,
                            Type         = BranchType.Unconditional
                        });
                }
                else
                {
                    var lastI = (AnyInstruction*)(instrs[instrs.Count - 1]);

                    blocks.Add(new CodeBlock
                    {
                        Instructions = MPtrListToPtrArr(instrs),
                        BranchTo     = lastI->Kind() == InstructionKind.Goto ? (AnyInstruction*)((long)lastI + lastI->Goto.Offset) : nextAddr /* can be null */,
                        Type         = lastI->Kind() == InstructionKind.Goto ? lastI->Goto.Type() : BranchType.Unconditional
                    });
                }
            }

            return blocks.ToArray();
        }
        static GraphVertex[] CreateVertices(GMFileContent content, CodeInfo code)
        {
            var blocks = SplitBlocks(code);
            var instr = code.Instructions;
            var firstI = (long)instr[0];

            // only one block -> just return it as a single vertex
            if (blocks.Length == 1)
                return new[]
                {
                    new GraphVertex
                    {
                        Branches     = EmptyGBArray,
                        Instructions = blocks[0].Instructions
                    }
                };

            var vertices = new GraphVertex[blocks.Length];

            // create list of vertices
            for (int i = 0; i < blocks.Length; i++)
            {
                var blk     = blocks[i];
                var hasNext = i < blocks.Length - 1 && blk.Type != BranchType.Unconditional /* no need to check if uncond */;

                vertices[i] = new GraphVertex
                {
                    Instructions = blk.Instructions,
                    Branches     = new GraphBranch[hasNext ? 2 : 1]
                };

                vertices[i].Branches[0] = new GraphBranch
                {
                    BranchTo = blk.BranchTo,
                    Type     = blk.Type
                };

                if (hasNext)
                    vertices[i].Branches[1] = new GraphBranch
                    {
                        BranchTo = blocks[i + 1].Instructions[0],
                        Type     = blk.Type.Invert(),
                    };
            }

            // connect vertex branches to target vertices
            for (int i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i];

                for (int j = 0; j < v.Branches.Length; j++)
                    v.Branches[j].ToVertex =
                        vertices.FirstOrDefault(ve => ve.Instructions[0] == v.Branches[j].BranchTo)
                            ?? (i == vertices.Length - 1 ? null : vertices[i + 1]);
            }

            return vertices;
        }

        public static GraphVertex[] BuildCFGraph(GMFileContent content, CodeInfo code)
        {
            if (code.Instructions.Length == 0)
                return EmptyGVArray;

            return CreateVertices(content, code);
        }

        public static Expression[] ParseExpressions(GMFileContent content, RefData rdata, GraphVertex vertex)
        {
            if (vertex.Instructions.Length == 0)
                return EmptyExprArray;

            var stack = new Stack<Expression>();
            var instr = vertex.Instructions;

            for (int i = 0; i < instr.Length; i++)
            {
                var ins = instr[i];

                var pst = (SingleTypeInstruction*)ins;
                var pdt = (DoubleTypeInstruction*)ins;
                var pcl = (CallInstruction      *)ins;
                var pps = (PushInstruction      *)ins;
                var pse = (SetInstruction       *)ins;

                var st = ins->SingleType;
                var dt = ins->DoubleType;
                var cl = ins->Call      ;
                var ps = ins->Push      ;
                var se = ins->Set       ;

                var t1 = ins->Kind() == InstructionKind.SingleType ? st.Type
                      : (ins->Kind() == InstructionKind.DoubleType ? dt.Types.Type1 : 0);
                var t2 = ins->Kind() == InstructionKind.DoubleType ? dt.Types.Type2
                      : (ins->Kind() == InstructionKind.SingleType ? st.Type        : 0);

                switch (ins->Code())
                {
                    case OpCode.Dup:
                        stack.Push(stack.Peek());
                        break;
                    case OpCode.Pop:
                        //stack.Pop();
                        break;
                    case OpCode.Pushenv: // ...?
                    case OpCode.Popenv :
                        break;
                    default:
                        switch (ins->ExprType())
                        {
                            #region variable
                            case ExpressionType.Variable:
                                stack.Push(new VariableExpression
                                {
                                    OriginalType = t1,
                                    ReturnType   = t2,
                                    Type         = ((Reference*)&pps->ValueRest)->Type,
                                    Owner        = (InstanceType)ps.Value,
                                    Variable     = rdata.Variables[rdata.VarAccessors[(IntPtr)ins]]
                                });
                                break;
                            #endregion
                            #region literal
                            case ExpressionType.Literal:
                                object v         = null;
                                var rest         = &pps->ValueRest;

                                switch (ps.Type)
                                {
                                    case DataType.Int16:
                                        v = ps.Value;
                                        break;
                                    case DataType.Boolean:
                                        v = ((DwordBool*)rest)->IsTrue();
                                        break;
                                    case DataType.Double:
                                        v = *(double*)rest;
                                        break;
                                    case DataType.Single:
                                        v = *(float*)rest;
                                        break;
                                    case DataType.Int32:
                                        v = *(int*)rest;
                                        break;
                                    case DataType.Int64:
                                        v = *(long*)rest;
                                        break;
                                    case DataType.String:
                                        v = SectionReader.GetStringInfo(content, ps.ValueRest);
                                        break;
                                }

                                stack.Push(new LiteralExpression
                                {
                                    OriginalType = t1,
                                    ReturnType   = t2,
                                    Value = v
                                });
                                break;
                            #endregion
                            #region set
                            case ExpressionType.Set:
                                stack.Push(new SetExpression
                                {
                                    OriginalType = t1,
                                    ReturnType   = t2,
                                    Type         = se.DestVar.Type,
                                    Owner        = se.Instance,
                                    Target       = rdata.Variables[rdata.VarAccessors[(IntPtr)ins]],
                                    Value        = stack.Pop()
                                });
                                break;
                            #endregion
                            #region call
                            case ExpressionType.Call:
                                stack.Push(new CallExpression
                                {
                                    ReturnType = t1,
                                    Type       = cl.Function.Type,
                                    Function   = rdata.Functions[rdata.FuncAccessors[(IntPtr)ins]],
                                    Arguments  = stack.PopMany(cl.Arguments).ToArray()
                                });
                                break;
                            #endregion
                            #region binaryop
                            case ExpressionType.BinaryOp:
                                stack.Push(new BinaryOperatorExpression
                                {
                                    OriginalType = t1,
                                    ReturnType   = t2,
                                    Arg1         = stack.Pop(),
                                    Arg2         = stack.Pop(),
                                    Operator     = ins->BinaryOp()
                                });
                                break;
                            #endregion
                            #region unaryop
                            case ExpressionType.UnaryOp:
                                stack.Push(new UnaryOperatorExpression
                                {
                                    OriginalType = t1,
                                    ReturnType   = t2,
                                    Input        = stack.Pop(),
                                    Operator     = ins->UnaryOp()
                                });
                                break;
                            #endregion
                        }
                        break;
                }
            }

            return stack.Reverse().ToArray();
        }

        /*static void DecompileVertex(GMFileContent content, RefData rdata, GraphVertex g, StringBuilder sb, List<uint> visited)
        {
            var exprs = ParseExpressions(content, rdata, g);

            sb.Append("GML_").Append(g.FirstInstrAddress.ToString(SR.HEX_FM8)).AppendLine(": ");

            foreach (var e in exprs)
                sb.Append("    ").AppendLine(e.ToString());

            visited.Add(g.FirstInstrAddress);

            if (g.Branches.Length == 0)
                return;

            for (int j = 0; j < g.Branches.Length; j++)
            {
                var b = g.Branches[j];

                switch (b.Type)
                {
                    case BranchType.IfFalse:
                        sb.Append("if false ");
                        break;
                    case BranchType.IfTrue:
                        sb.Append("if true ");
                        break;
                }

                sb.Append("goto GML_").AppendLine(b.BranchToAddress.ToString(SR.HEX_FM8));

                if (j != g.Branches.Length - 1)
                    sb.Append("else ");
            }

            for (int j = 0; j < g.Branches.Length; j++)
            {
                var next = g.Branches[j].ToVertex;

                if (next == null)
                    sb.AppendLine("    ret");
                else if (!visited.Contains(next.FirstInstrAddress))
                    DecompileVertex(content, rdata, next, sb, visited);

                //visited.Add(next);
            }
        }*/

        public static string DecompileCode(GMFileContent content, RefData rdata, uint id)
        {
            var sb = new StringBuilder();

            var code = Disassembler.DisassembleCode(content, id);
            var graph = BuildCFGraph(content, code);
            var dasm = Disassembler.DisplayInstructions(content, rdata, code);

            //DecompileVertex(content, rdata, graph[0], sb, new List<uint>());

            //for (uint i = 0; i < graph.Length; i++)
            //{
            //    var g = graph[i];
            //    var exprs = ParseExpressions(content, rdata, g);

            //    sb.Append("GML_").Append(g.FirstInstrAddress.ToString(SR.HEX_FM8)).AppendLine(": ");

            //    foreach (var e in exprs)
            //        sb.Append("    ").AppendLine(e.ToString());

            //    if (g.Branches.Length == 0)
            //        continue;

            //    // * goto 0xFFFFFF
            //    // else goto \n else

            //    for (int j = 0; j < g.Branches.Length; j++)
            //    {
            //        var b = g.Branches[j];

            //        //if (j != g.Branches.Length - 1)
            //        //{
            //            switch (b.Type)
            //            {
            //                case BranchType.IfFalse:
            //                    sb.Append("if false ");
            //                    break;
            //                case BranchType.IfTrue:
            //                    sb.Append("if true " );
            //                    break;
            //            }

            //            sb.Append("goto GML_").AppendLine(b.BranchToAddress.ToString(SR.HEX_FM8))
            //              .Append("else ");
            //        //}
            //        //else
            //        if (b.BranchToAddress == 0xFFFFFF)
            //            sb.Append("    ret");
            //    }

            //    sb.AppendLine();
            //}

            return sb.ToString();
        }
    }
}
