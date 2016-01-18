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
        readonly static Expression [] EmptyExprArray = { };

        readonly static ExitStatement FinalRet = new ExitStatement();
        readonly static PopExpression PopExpr  = new PopExpression();

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
                    p = (IntPtr)((long)ins + ins->Goto.Offset * 4L);

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

                if (instrs.Count == 0)
                    continue;

                if (i == jumpTo.Length - 1 && br > instr[instr.Length - 1]) // implicit 'ret' after last instruction
                    blocks.Add(new CodeBlock
                    {
                        Instructions = Utils.MPtrListToPtrArr(instrs),
                        BranchTo     = null,
                        Type         = BranchType.Unconditional
                    });
                else
                {
                    var lastI = (AnyInstruction*)(instrs[instrs.Count - 1]);

                    blocks.Add(new CodeBlock
                    {
                        Instructions = Utils.MPtrListToPtrArr(instrs),
                        BranchTo     = lastI->Kind() == InstructionKind.Goto ? (AnyInstruction*)((long)lastI + lastI->Goto.Offset * 4L) : nextAddr /* can be null */,
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

        public static Statement[] ParseStatements(GMFileContent content, RefData rdata, CodeInfo code, AnyInstruction*[] instr = null, Stack<Expression> stack = null, List<Expression> dupTars = null)
        {
            //! here be dragons

            stack   = stack   ?? new Stack<Expression>();
            dupTars = dupTars ?? new List <Expression>();
            instr   = instr   ?? code.Instructions;

            if (instr.Length == 0)
                return EmptyStmtArray;

            var stmts = new List<Statement>();

            var firstI = code.Instructions[0];

            //TODO: use locals

            Func<Expression> Pop  = () => stack.Count == 0 ? PopExpr : stack.Pop ();
          //Func<Expression> Peek = () => stack.Count == 0 ? PopExpr : stack.Peek();
            Func<int, IEnumerable<Expression>> PopMany = i =>
            {
                var ret = new List<Expression>();

                for (int j = 0; j < i; j++)
                    ret.Add(Pop());

                return ret;
            };
            #region Action FlushStack = () => { };
            Action FlushStack = () =>
            {
                var readd = new Stack<Expression>();

                //? not sure if this is a good idea (random 'push'es in the wild) (see TODO)
                stmts.AddRange(stack.PopAll().Where(e =>
                {
                    if (dupTars.Contains(e))
                    {
                        readd.Push(e);
                        return false;
                    }

                    return !(e is PopExpression); // 'push pop' is obviously stupid to emit
                }).Reverse().Select(e =>
                    e is UnaryOperatorExpression &&
                            ((UnaryOperatorExpression)e).Operator == UnaryOperator.Duplicate
                        ? (Statement)new DupStatement() : new PushStatement { Expr = e }));

                stack.PushRange(readd);
            };
            #endregion
            Action<Statement> AddStmt = s =>
            {
                FlushStack();

                stmts.Add(s);
            };
            Func<VariableType, Expression[]> TryGetIndices = vt =>
            {
                Expression index = null;

                var dimentions = 0;
                if (vt == VariableType.Array)
                {
                    index = Pop();

                    var arrInd = Pop();

                    if ((arrInd is LiteralExpression) && ((LiteralExpression)arrInd).Value is short)
                    {
                        var s = (short)((LiteralExpression)arrInd).Value;

                        switch (s)
                        {
                            case -1:
                                dimentions = 2;
                                break;
                            case -5:
                                dimentions = 1;
                                break;
                        }
                    }

                    if (dimentions == 0)
                    {
                        stack.Push(arrInd);
                        stack.Push(index);

                        index = null;
                    }
                }

                if (index == null)
                    return null;

                // analyse index for specified dimention
                switch (dimentions)
                {
                    case 2:
                        if (index is BinaryOperatorExpression && ((BinaryOperatorExpression)index).Operator == BinaryOperator.Addition)
                        {
                            var boe = (BinaryOperatorExpression)index;

                            var a = boe.Arg1;
                            var b = boe.Arg2;

                            if (a is BinaryOperatorExpression && ((BinaryOperatorExpression)a).Operator == BinaryOperator.Multiplication)
                            {
                                var a_ = (BinaryOperatorExpression)a;
                                var c = a_.Arg2;

                                if (c is LiteralExpression && ((LiteralExpression)c).ReturnType == DataType.Int32
                                        && (int /* should be */)((LiteralExpression)c).Value == 32000)
                                    return new[] { a_.Arg1, b };
                            }
                        }
                        break;
                }

                return new[] { index };
            };

            for (int i = 0; i < instr.Length; i++)
            {
                var ins = instr[i];

                #region stuff
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
                #endregion

                switch (ins->Code())
                {
                    #region dup, pop
                    case OpCode.Dup:
                        var normal = true;
                        if (i < instr.Length - 1 && instr[i + 1]->OpCode == OpCode.Push)
                        {
                            var n = &instr[i + 1]->Push;
                            var t = ((Reference*)&n->ValueRest)->Type;

                            if (t == VariableType.Array && stack.Count > 1)
                            {
                                normal = false;

                                stack.Push(stack.Skip(1).First()); // second item
                                stack.Push(stack.Skip(1).First()); // first  item (original stack top)
                            }
                        }

                        if (!normal)
                            break;

                        if (!dupTars.Contains(stack.Peek()))
                            dupTars.Add(stack.Peek());

                        if (stack.Peek().WalkExprTree(e => e is CallExpression).Any(_ => _))
                        {
                            stack.Push(new UnaryOperatorExpression
                            {
                                Input        = stack.Peek(),
                                Operator     = UnaryOperator.Duplicate,
                                OriginalType = stack.Peek().ReturnType,
                                ReturnType   = st.Type
                            });

                            //AddStmt(new DupStatement());
                        }
                        else
                            stack.Push(stack.Peek());
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
                    #endregion
                    #region env
                    //TODO: use actual '(with obj ...)' syntax
                    //! it might mess with the CFG structure
                    case OpCode.PushEnv:
                        AddStmt(new PushEnvStatement
                        {
                            Target       = (AnyInstruction*)((byte*)ins + pbr->Offset * 4L),
                            TargetOffset = (byte*)ins + pbr->Offset * 4L - (byte*)firstI,
                            Parent       = stack.Pop()
                        });
                        break;
                    case OpCode.PopEnv :
                        AddStmt(new PopEnvStatement
                        {
                            Target       = (AnyInstruction*)((byte*)ins + pbr->Offset * 4L),
                            TargetOffset = (byte*)ins + pbr->Offset * 4L - (byte*)firstI
                        });
                        break;
                    #endregion
                    #region branch
                    case OpCode.Brt:
                    case OpCode.Brf:
                    case OpCode.Br:
                        AddStmt(new BranchStatement
                        {
                            Type         = pbr->Type(),
                            Conditional  = pbr->Type() == BranchType.Unconditional ? null : Pop(),
                            Target       = (AnyInstruction*)((byte*)ins + pbr->Offset * 4L),
                            TargetOffset = (byte*)ins + pbr->Offset * 4L - (byte*)firstI
                        });
                        break;
                    #endregion
                    #region break, ret, exit
                    case OpCode.Break:
                        stack.Push(new AssertExpression
                        {
                            ControlValue = pbk->Signal,
                            ReturnType   = pbk->Type,
                            Expr         = Pop()
                        });
                        break;
                    case OpCode.Ret:
                        AddStmt(new ReturnStatement
                        {
                            ReturnType = pst->Type,
                            RetValue   = Pop()
                        });
                        break;
                    case OpCode.Exit:
                        AddStmt(new ExitStatement());
                        break;
                    #endregion
                    #region set
                    case OpCode.Set:
                        var ind = TryGetIndices(se.DestVar.Type); // call before Value's pop
                        AddStmt(new SetStatement
                        {
                            OriginalType = se.Types.Type1,
                            ReturnType   = se.Types.Type2,
                            Type         = se.DestVar.Type,
                            Owner        = se.Instance,
                            Target       = rdata.Variables[rdata.VarAccessors[(IntPtr)ins]],
                            Value        = Pop(),
                            ArrayIndices = ind ?? TryGetIndices(se.DestVar.Type)
                        });
                        break;
                    #endregion
                    default:
                        switch (ins->ExprType())
                        {
                            #region variable
                            case ExpressionType.Variable:
                                var vt = ((Reference*)&pps->ValueRest)->Type;

                                if (vt == VariableType.StackTop && (InstanceType)ps.Value == InstanceType.StackTopOrGlobal)
                                {
                                    stack.Push(new MemberExpression
                                    {
                                        Owner        = Pop(),
                                        ReturnType   = ps.Type,
                                        Type         = vt,
                                        OwnerType    = (InstanceType)ps.Value,
                                        Variable     = rdata.Variables[rdata.VarAccessors[(IntPtr)ins]],
                                        ArrayIndices = TryGetIndices(vt)
                                    });
                                }
                                else
                                    stack.Push(new VariableExpression
                                    {
                                        ReturnType   = ps.Type,
                                        Type         = vt,
                                        OwnerType    = (InstanceType)ps.Value,
                                        Variable     = rdata.Variables[rdata.VarAccessors[(IntPtr)ins]],
                                        ArrayIndices = TryGetIndices(vt)
                                    });
                                break;
                            #endregion
                            #region literal
                            case ExpressionType.Literal:
                                object v         = null;
                                var rest         = &pps->ValueRest;

                                #region get value
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
                                #endregion

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
                                    Arguments  = PopMany(cl.Arguments).Reverse().ToArray()
                                });
                                break;
                            #endregion
                            #region binaryop
                            case ExpressionType.BinaryOp:
                                var a1 = Pop();
                                var a2 = Pop();

                                stack.Push(new BinaryOperatorExpression
                                {
                                    OriginalType = t1,
                                    ReturnType   = t2,
                                    Arg1         = a2,
                                    Arg2         = a1,
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
                                    Input        = Pop(),
                                    Operator     = ins->UnaryOp()
                                });
                                break;
                            #endregion
                        }
                        break;
                }
            }

            FlushStack();

            return stmts.ToArray();
        }

        public static string DecompileCode(GMFileContent content, RefData rdata, CodeInfo code)
        {
            if (code.Instructions.Length == 0)
                return String.Empty;

            var sb = new StringBuilder();

            var stack = new Stack<Expression>();
            var dupts = new List <Expression>();

            var firstI = (long)code.Instructions[0];

            var graph = BuildCFGraph(content, code);

            //TODO: CFG kind recognition stuff (if, if/else, for, while, etc)
            var i = 0;
            foreach (var g in graph)
            {
                var stmts = ParseStatements(content, rdata, code, g.Instructions, stack);

                sb  .Append(SR.HEX_PRE)
                    .Append(((long)g.Instructions[0] - firstI).ToString(SR.HEX_FM6))
                    .AppendLine(SR.COLON);

                foreach (var s in stmts)
                    sb.Append(SR.INDENT4).AppendLine(s.ToString());

                i++;
            }

            sb.Append(SR.INDENT4).AppendLine(FinalRet.ToString());

            return sb.ToString();
        }
    }
}
