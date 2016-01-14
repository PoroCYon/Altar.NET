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
        readonly static Statement  [] EmptyStmtArray = { };

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

            ret.Sort(Utils.ComparePtrs);

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
                var ind = Utils.IndexOfPtr(instr, br);
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
                            Instructions = Utils.MPtrListToPtrArr(instrs),
                            BranchTo     = null,
                            Type         = BranchType.Unconditional
                        });
                }
                else
                {
                    var lastI = (AnyInstruction*)(instrs[instrs.Count - 1]);

                    blocks.Add(new CodeBlock
                    {
                        Instructions = Utils.MPtrListToPtrArr(instrs),
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
                        Type     = blk.Type.Invert()
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

        public static Statement[] ParseStatements(GMFileContent content, RefData rdata, AnyInstruction*[] instr)
        {
            if (instr.Length == 0)
                return EmptyStmtArray;

            var stack = new Stack<Expression>();
            var stmts = new List <Statement >();

            for (int i = 0; i < instr.Length; i++)
            {
                var ins = instr[i];

                var pst = (SingleTypeInstruction*)ins;
                var pdt = (DoubleTypeInstruction*)ins;
                var pcl = (CallInstruction      *)ins;
                var pps = (PushInstruction      *)ins;
                var pse = (SetInstruction       *)ins;
                var pbr = (GotoInstruction      *)ins;
                var pbk = (BreakInstruction     *)ins;

                var st = ins->SingleType;
                var dt = ins->DoubleType;
                var cl = ins->Call      ;
                var ps = ins->Push      ;
                var se = ins->Set       ;

                var t1 = ins->Kind() == InstructionKind.SingleType ? st.Type
                      : (ins->Kind() == InstructionKind.DoubleType ? dt.Types.Type1 : 0);
                var t2 = ins->Kind() == InstructionKind.DoubleType ? dt.Types.Type2
                      : (ins->Kind() == InstructionKind.SingleType ? st.Type        : 0);

                Action<Statement> AddStmt = s =>
                {
                    // flush stack
                    stmts.AddRange(stack.PopAll().Reverse().Select(e => new PushStatement { Expr = e }));

                    stmts.Add(s);
                };

                switch (ins->Code())
                {
                    #region stack stuff
                    case OpCode.Dup:
                        AddStmt(new DupStatement());
                        break;
                    case OpCode.Pop:
                        if (stack.Count > 0 && stack.Peek() is CallExpression)
                            AddStmt(new CallStatement
                            {
                                Call = stack.Pop() as CallExpression
                            });
                        else
                            AddStmt(new PopStatement());
                        break;
                    case OpCode.PushEnv:
                        AddStmt(new PushEnvStatement()); // ?
                        break;
                    case OpCode.PopEnv:
                        AddStmt(new PopEnvStatement());
                        break;
                    #endregion
                    #region branch
                    case OpCode.Brt:
                    case OpCode.Brf:
                    case OpCode.Br:
                        AddStmt(new BranchStatement
                        {
                            Type         = pbr->Type(),
                            Conditional  = pbr->Type() == BranchType.Unconditional ? null : stack.Pop(),
                            Target       = (AnyInstruction*)((byte*)ins + pbr->Offset),
                            TargetOffset = (byte*)ins - (byte*)instr[0]
                        });
                        break;
                    #endregion
                    #region break, ret, exit
                    case OpCode.Break:
                        AddStmt(new BreakStatement
                        {
                            Signal = pbk->Signal,
                            Type   = pbk->Type
                        });
                        break;
                    case OpCode.Ret:
                    case OpCode.Exit: // ?
                        AddStmt(new ReturnStatement
                        {
                            ReturnType = pst->Type,
                            RetValue   = stack.Pop()
                        });
                        break;
                    #endregion
                    case OpCode.Set:
                        AddStmt(new SetStatement
                        {
                            OriginalType = se.Types.Type1,
                            ReturnType   = se.Types.Type2,
                            Type         = se.DestVar.Type,
                            Owner        = se.Instance,
                            Target       = rdata.Variables[rdata.VarAccessors[(IntPtr)ins]],
                            Value        = stack.Pop()
                        });
                        break;
                    default:
                        switch (ins->ExprType())
                        {
                            #region variable
                            case ExpressionType.Variable:
                                stack.Push(new VariableExpression
                                {
                                    ReturnType   = ps.Type,
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
                                    ReturnType = ps.Type,
                                    Value      = v
                                });
                                break;
                            #endregion
                            #region call
                            case ExpressionType.Call:
                                stack.Push(new CallExpression
                                {
                                    ReturnType = cl.ReturnType,
                                    Type       = cl.Function.Type,
                                    Function   = rdata.Functions[rdata.FuncAccessors[(IntPtr)ins]],
                                    Arguments  = stack.PopMany(cl.Arguments).Reverse().ToArray()
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

            //var str = String.Join(Environment.NewLine, stmts.Select(s => s.ToString()));

            return stmts.ToArray();
        }

        public static string DecompileCode(GMFileContent content, RefData rdata, uint id)
        {
            var sb = new StringBuilder();

            var code  = Disassembler.DisassembleCode(content, id);
            var dasm  = Disassembler.DisplayInstructions(content, rdata, code); // for debugging
            var graph = BuildCFGraph(content, code);
            var stmts = ParseStatements(content, rdata, graph[0].Instructions /* ? */);

            // TODO

            return sb.ToString();
        }
    }
}
