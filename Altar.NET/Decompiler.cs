using System;
using System.Collections.Generic;
using System.Linq;

namespace Altar
{
    public unsafe static class Decompiler
    {
        struct CodeBlock
        {
            public List<IntPtr> Instructions;
            public BranchType Type;
            public uint BranchToAddress;
        }

        readonly static GraphBranch[] EmptyGBArray   = { };
        readonly static GraphVertex[] EmptyGVArray   = { };
        readonly static Expression [] EmptyExprArray = { };

        static List<CodeBlock> SplitBlocks (GMFileContent content, CodeInfo code)
        {
            var blocks = new List<CodeBlock>();
            var curBl  = new List<IntPtr>();
            var instr  = code.Instructions;
            var firstI = (long)instr[0];

            // find all branch-to instructions
            var targets = new List<long>();
            for (int i = 0; i < instr.Length; i++)
                if (instr[i]->Kind() == InstructionKind.Goto)
                    targets.Add((long)instr[i] - firstI + instr[i]->Goto.Offset);

            // split code into blocks separated by br[tf]? instructions
            for (int i = 0; i < instr.Length; i++)
            {
                curBl.Add((IntPtr)instr[i]);

                if (instr[i]->Kind() == InstructionKind.Goto)
                {
                    blocks.Add(new CodeBlock
                    {
                        Instructions = curBl,
                        Type = instr[i]->Goto.Type(),
                        BranchToAddress = (uint)((long)instr[i] - firstI) + instr[i]->Goto.Offset
                    });

                    curBl = new List<IntPtr>();
                }
                else if (targets.Contains((long)instr[i] - firstI))
                {
                    blocks.Add(new CodeBlock
                    {
                        Instructions = curBl,
                        Type = BranchType.Unconditional,
                        BranchToAddress = i == instr.Length - 1 ? 0xFFFFFFFF : (uint)((long)instr[i + 1] - firstI)
                    });
                }
            }

            if (curBl.Count != 0)
                blocks.Add(new CodeBlock
                {
                    Type = BranchType.Unconditional,
                    BranchToAddress = 0xFFFFFFFF,
                    Instructions = curBl
                });

            return blocks;
        }
        static GraphVertex[] CreateVertices(GMFileContent content, CodeInfo code)
        {
            var blocks = SplitBlocks(content, code);
            var instr = code.Instructions;
            var firstI = (long)instr[0];

            // only one block -> just return it as a single vertex
            if (blocks.Count == 1)
            {
                var bis = blocks[0].Instructions;
                var r = new GraphVertex
                {
                    Branches = EmptyGBArray,
                    FirstInstrAddress = 0,
                    Instructions = new AnyInstruction*[bis.Count]
                };

                for (int i = 0; i < bis.Count; i++)
                    r.Instructions[i] = (AnyInstruction*)bis[i];

                return new[] { r };
            }

            // find all branch-to instructions
            var targets = new List<long>();
            for (int i = 0; i < instr.Length; i++)
                if (instr[i]->Kind() == InstructionKind.Goto)
                    targets.Add((long)instr[i] - firstI + instr[i]->Goto.Offset);

            var vertices = new GraphVertex[blocks.Count];

            // create list of vertices
            for (int i = 0; i < blocks.Count; i++)
            {
                var blk = blocks[i];
                var ins = blk.Instructions;
                var hasNext = i < blocks.Count - 1 && blk.Type != BranchType.Unconditional /* no need to check if uncond */;

                vertices[i] = new GraphVertex
                {
                    FirstInstrAddress = (uint)((long)ins[0] - firstI),
                    Instructions = new AnyInstruction*[ins.Count],
                    Branches = new GraphBranch[hasNext ? 2 : 1]
                };

                vertices[i].Branches[0] = new GraphBranch
                {
                    BranchToAddress = blk.BranchToAddress,
                    Type = blk.Type
                };

                if (hasNext)
                    vertices[i].Branches[1] = new GraphBranch
                    {
                        BranchToAddress = (uint)((long)blocks[i + 1].Instructions[0] - firstI),
                        Type = blk.Type.Invert()
                    };

                for (int j = 0; j < ins.Count; j++)
                    vertices[i].Instructions[j] = (AnyInstruction*)ins[j];
            }

            // connect vertex branches to target vertices
            for (int i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i];

                for (int j = 0; j < v.Branches.Length; j++)
                    v.Branches[j].ToVertex =
                        vertices.FirstOrDefault(ve => ve.FirstInstrAddress == v.Branches[j].BranchToAddress)
                            ?? (i == vertices.Length - 1 ? null : vertices[i + 1]);
            }

            return vertices;
        }

        /// <summary>
        /// Builds a control flow graph from bytecode.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="code"></param>
        /// <returns>the entry vertex of the code.</returns>
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
    }
}
