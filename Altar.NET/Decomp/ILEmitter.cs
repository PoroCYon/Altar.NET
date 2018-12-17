using Altar.Unpack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Linq;

namespace Altar.Decomp
{
    public unsafe static class ILEmitter
    {
        static string[] types =
        {
            "double", //Double
            "float",  //Single
            "i32",    //Int32
            "i64",    //Int64
            "i1",     //Boolean
            "%var",   //Variable
            "i8*",    //String
            "i32",    //Instance
            null, null, null, null, null, null, null,
            "i16",    //Int16
        };

        static string[] compares =
        {
            "",
            "slt", //LowerThan
            "sle", //LTOrEqual
            "eq",  //Equality
            "ne",  //Inequality
            "sge", //GTOrEqual
            "sgt", //GreaterThan
        };

        static readonly string REG = "%";
        static readonly string GLOBAL = "@";

        abstract class StackValue
        {
            public DataType dataType;
            public bool pointer;

            protected StackValue(DataType dt)
            {
                dataType = dt;
            }
            public override string ToString() => types[(int)dataType] + (pointer ? SR.ASTERISK : "") + SR.SPACE_S + ShortString();
            public abstract string ShortString();
        }
        class TempRegister : StackValue
        {
            int name;

            public TempRegister(DataType dt, int n) : base(dt)
            {
                name = n;
            }
            public override string ShortString() => REG + name.ToString();
        }
        class NamedRegister : StackValue
        {
            string name;

            public NamedRegister(DataType dt, string n) : base(dt)
            {
                name = n;
                pointer = true;
            }
            public override string ShortString() => REG + name;
        }
        class Global : StackValue
        {
            string name;

            public Global(DataType dt, string n) : base(dt)
            {
                name = n;
                pointer = true;
            }
            public override string ShortString() => GLOBAL + name;
        }
        class Constant<T> : StackValue where T : IConvertible
        {
            public T value;

            public Constant(DataType dt, T v) : base(dt)
            {
                value = v;
            }
            public override string ShortString() => value.ToString(CultureInfo.InvariantCulture);
        }
        class BoolConstant : Constant<DwordBool>
        {
            public BoolConstant(DataType dt, DwordBool v) : base(dt, v) { }
            public override string ShortString() => value.ToPrettyString();
        }
        class FloatConstant : Constant<float>
        {
            public FloatConstant(DataType dt, float v) : base(dt, v) { }
            public override string ShortString() => value.ToString(SR.SINGLE_FMT, CultureInfo.InvariantCulture);
        }
        class DoubleConstant : Constant<double>
        {
            public DoubleConstant(DataType dt, double v) : base(dt, v) { }
            public override string ShortString() => value.ToString(SR.DOUBLE_FMT, CultureInfo.InvariantCulture);
        }
        class StringConstant : Constant<string>
        {
            public StringConstant(DataType dt, string v) : base(dt, v) { }
            public override string ShortString() => value.Escape();
        }

        class Instruction
        {
            protected TempRegister result;
            protected string name;
            protected StackValue[] operands;

            public Instruction(TempRegister r, string n, params StackValue[] o)
            {
                result = r;
                name = n;
                operands = o;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                if (result != null)
                {
                    sb.Append(result.ShortString());
                    sb.Append(SR.EQ_S);
                }
                sb.Append(name);
                foreach (var o in operands)
                {
                    sb.Append(SR.SPACE_S);
                    sb.Append(o.ToString());
                    if (o != operands[operands.Length-1])
                    {
                        sb.Append(",");
                    }
                }
                return sb.ToString();
            }
        }

        class CallInstruction : Instruction
        {
            string function;

            public CallInstruction(TempRegister r, string f, params StackValue[] o) : base(r, "call", o)
            {
                function = f;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                if (result != null)
                {
                    sb.Append(result.ShortString());
                    sb.Append(SR.EQ_S);
                }
                sb.Append(name);
                sb.Append(SR.SPACE_S);
                sb.Append(GLOBAL);
                sb.Append(function);
                sb.Append(SR.O_PAREN);
                foreach (var o in operands)
                {
                    sb.Append(o.ToString());
                    if (o != operands[operands.Length - 1])
                    {
                        sb.Append(SR.COMMA_S);
                    }
                }
                sb.Append(SR.C_PAREN);
                return sb.ToString();
            }
        }

        class JumpInstruction : Instruction
        {
            string[] labels;

            public JumpInstruction(StackValue o, params string[] l) : base(null, "br", o)
            {
                labels = l;
            }

            public JumpInstruction(params string[] l) : base(null, "br")
            {
                labels = l;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder(base.ToString());
                sb.Append(" ");
                foreach (var l in labels)
                {
                    sb.Append("label");
                    sb.Append(SR.SPACE_S);
                    sb.Append(SR.MOD);
                    sb.Append(l);
                    if (l != labels[labels.Length - 1])
                    {
                        sb.Append(SR.COMMA_S);
                    }
                }
                return sb.ToString();
            }
        }

        class CompareInstruction : Instruction
        {
            ComparisonType compareType;

            public CompareInstruction(TempRegister dest, ComparisonType comparisonType, params StackValue[] args) : base(dest, "icmp", args)
            {
                compareType = comparisonType;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                if (result != null)
                {
                    sb.Append(result.ShortString());
                    sb.Append(SR.EQ_S);
                }
                sb.Append(name);
                sb.Append(SR.SPACE_S);
                sb.Append(compares[(int)compareType]);
                foreach (var o in operands)
                {
                    sb.Append(SR.SPACE_S);
                    sb.Append(o.ToString());
                    if (o != operands[operands.Length - 1])
                    {
                        sb.Append(",");
                    }
                }
                return sb.ToString();
            }
        }

        class GetElementPtr : Instruction
        {
            // TODO: indexize
            string elementName;

            public GetElementPtr(TempRegister r, string n, params StackValue[] o) : base(r, "getelementptr", o)
            {
                elementName = n;
            }

            public override string ToString()
            {
                return base.ToString()+SR.COMMA_S+elementName;
            }
        }

        struct ILBlock {
            public string label;
            public Instruction[] instructions;
            public StackValue[] stackRemaining;
            public TempRegister[] stackDebt;

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.Append(label);
                sb.AppendLine(SR.COLON);
                foreach (var inst in instructions)
                {
                    sb.Append(SR.INDENT4);
                    sb.AppendLine(inst.ToString());
                }
                return sb.ToString();
            }
        }

        static int RewriteBlock(Decompiler.CodeBlock block, long firstI, int tempReg, uint bcv, RefData rdata, GMFileContent content,
            out ILBlock il)
        {
            var code = new List<Instruction>();
            var stack = new Stack<StackValue>();
            var debt = new Stack<TempRegister>();

            StackValue Pop()
            {
                if (stack.Count > 0)
                {
                    return stack.Pop();
                }
                else
                {
                    var dest = new TempRegister(DataType.Variable, tempReg++);
                    debt.Push(dest);
                    return dest;
                }
            }
            IEnumerable<StackValue> PopMany(int amount)
            {
                foreach (var x in stack.PopMany(Math.Min(amount, stack.Count)))
                {
                    yield return x;
                    amount--;
                }
                while (amount > 0)
                {
                    var dest = new TempRegister(DataType.Variable, tempReg++);
                    debt.Push(dest);
                    yield return dest;
                }
                yield break;
            }
            StackValue GetSrc(InstanceType instanceType, ReferenceDef v, DataType dataType, StackValue owner)
            {
                TempRegister addr;
                switch (instanceType)
                {
                    case InstanceType.Global:
                        // TODO store/cache these
                        return new Global(dataType, v.Name);
                    case InstanceType.Local:
                        // TODO store/cache these
                        return new NamedRegister(dataType, v.Name);
                    case InstanceType.Self:
                        addr = new TempRegister(dataType, tempReg++);
                        addr.pointer = true;
                        code.Add(new GetElementPtr(addr, v.Name,
                            new NamedRegister(DataType.Variable, "self"),
                            new Constant<int>(DataType.Int32, 0)));
                        return addr;
                    case InstanceType.StackTopOrGlobal:
                        if (owner is Constant<short> c && (InstanceType)c.value == InstanceType.Self)
                        {
                            addr = new TempRegister(dataType, tempReg++);
                            addr.pointer = true;
                            code.Add(new GetElementPtr(addr, v.Name,
                                new NamedRegister(DataType.Variable, "self"),
                                new Constant<int>(DataType.Int32, 0)));
                            return addr;
                        }
                        else
                        {
                            addr = new TempRegister(dataType, tempReg++);
                            addr.pointer = true;
                            code.Add(new GetElementPtr(addr, v.Name, owner,
                                new Constant<int>(DataType.Int32, 0)));
                            return addr;
                        }
                    default:
                        throw new NotImplementedException();
                }
            }

            foreach (var inst in block.Instructions)
            {
                TempRegister dest;
                StackValue arg1, arg2;
                ReferenceDef v;
                Reference rv;
                StackValue owner, index;
                StackValue src;
                switch (inst->Kind(bcv))
                {
                    case InstructionKind.SingleType:
                        if (inst->OpCode.General(bcv) == GeneralOpCode.Pop)
                        {
                            Pop();
                        }
                        else if (inst->OpCode.General(bcv) == GeneralOpCode.Dup)
                        {
                            var dup = PopMany(inst->SingleType.DupExtra+1);
                            stack.PushRange(dup);
                            stack.PushRange(dup);
                        }
                        else
                        {
                            arg1 = Pop();
                            dest = new TempRegister(inst->SingleType.Type, tempReg++);
                            code.Add(new Instruction(dest, inst->OpCode.ToPrettyString((int)bcv), arg1));
                            stack.Push(dest);
                        }
                        break;
                    case InstructionKind.DoubleType:
                        dest = new TempRegister(inst->DoubleType.Types.Type2, tempReg++);
                        arg2 = null;
                        if (inst->OpCode.General(bcv) == GeneralOpCode.Conv ||
                            inst->OpCode.General(bcv) == GeneralOpCode.Not)
                        {
                            code.Add(new Instruction(dest, inst->OpCode.ToPrettyString((int)bcv), Pop()));
                        }
                        else
                        {
                            arg2 = Pop();
                            arg1 = Pop();
                            if (inst->OpCode.General(bcv) == GeneralOpCode.Cmp)
                            {
                                code.Add(new CompareInstruction(dest, inst->DoubleType.ComparisonType, arg1, arg2));
                            }
                            else
                            {
                                code.Add(new Instruction(dest, inst->OpCode.ToPrettyString((int)bcv), arg1, arg2));
                            }
                        }
                        stack.Push(dest);
                        break;
                    case InstructionKind.Goto:
                        var a = inst->Goto.Offset.UValue * 4;
                        if ((a & 0xFF000000) != 0)
                        {
                            a &= 0x00FFFFFF;
                            a -= 0x01000000;
                        }
                        var jumpdest = "label"+(((long)inst + unchecked((int)a)) - firstI).ToString(SR.HEX_FM6);
                        if (inst->OpCode.General(bcv) == GeneralOpCode.Br)
                        {
                            code.Add(new JumpInstruction(jumpdest));
                        }
                        else
                        {
                            var nextdest = "label"+(((long)inst + DisasmExt.Size(inst, bcv) * 4) - firstI).ToString(SR.HEX_FM6);
                            var iftrue = inst->OpCode.General(bcv) == GeneralOpCode.Brf ? nextdest : jumpdest;
                            var iffalse = inst->OpCode.General(bcv) == GeneralOpCode.Brf ? jumpdest : nextdest;
                            code.Add(new JumpInstruction(Pop(), iftrue, iffalse));
                        }
                        break;
                    case InstructionKind.Set:
                        rv = inst->Set.DestVar;
                        v = rdata.Variables[rdata.VarAccessors[(IntPtr)inst]];

                        owner = null;
                        index = null;
                        if (rv.Type == VariableType.Array)
                        {
                            index = Pop();
                            owner = Pop();
                        }
                        else if (rv.Type == VariableType.StackTop)
                        {
                            owner = Pop();
                        }

                        var value = Pop();

                        src = GetSrc(inst->Set.Instance, v, inst->Set.Types.Type2, owner);

                        code.Add(new Instruction(null, "store", value, src));
                        break;
                    case InstructionKind.Push:
                        var r = inst->Push.ValueRest;
                        switch (inst->Push.Type)
                        {
                            case DataType.Int16:
                                stack.Push(new Constant<short>(inst->Push.Type, inst->Push.Value));
                                break;
                            case DataType.Variable:
                                rv = *(Reference*)&r;
                                v = rdata.Variables[rdata.VarAccessors[(IntPtr)inst]];

                                owner = null;
                                index = null;
                                if (rv.Type == VariableType.Array)
                                {
                                    index = Pop();
                                    owner = Pop();
                                }
                                else if (rv.Type == VariableType.StackTop)
                                {
                                    owner = Pop();
                                }

                                src = GetSrc((InstanceType)inst->Push.Value, v, inst->Push.Type, owner);
                                dest = new TempRegister(inst->Push.Type, tempReg++);
                                code.Add(new Instruction(dest, "load", src));
                                stack.Push(dest);

                                break;
                            case DataType.Boolean:
                                stack.Push(new BoolConstant(inst->Push.Type, *((DwordBool*)&r)));
                                break;
                            case DataType.Double:
                                stack.Push(new DoubleConstant(inst->Push.Type, *((double*)&r)));
                                break;
                            case DataType.Single:
                                stack.Push(new FloatConstant(inst->Push.Type, *((float*)&r)));
                                break;
                            case DataType.Int32:
                                stack.Push(new Constant<int>(inst->Push.Type, unchecked((int)r)));
                                break;
                            case DataType.Int64:
                                stack.Push(new Constant<long>(inst->Push.Type, *((long*)&r)));
                                break;
                            case DataType.String:
                                stack.Push(new StringConstant(inst->Push.Type, SectionReader.GetStringInfo(content, (uint)r)));
                                break;
                        }
                        break;
                    case InstructionKind.Call:
                        var args = PopMany(inst->Call.Arguments);
                        dest = new TempRegister(inst->Call.ReturnType, tempReg++);
                        code.Add(new CallInstruction(dest, rdata.Functions[rdata.FuncAccessors[(IntPtr)inst]].Name, args.ToArray()));
                        stack.Push(dest);
                        break;
                    case InstructionKind.Break:
                    case InstructionKind.Environment:
                    default:
                        code.Add(new Instruction(null, inst->OpCode.ToPrettyString((int)bcv)));
                        break;
                }
            }
            var lastinst = block.Instructions[block.Instructions.Length - 1];
            if (lastinst->Kind(bcv) != InstructionKind.Goto &&
                !(bcv > 0xE && lastinst->OpCode.VersionF == FOpCode.Ret ||
                    lastinst->OpCode.VersionE == EOpCode.Ret))
            {
                code.Add(new JumpInstruction("label"+(((long)lastinst + DisasmExt.Size(lastinst, bcv) * 4) - firstI).ToString(SR.HEX_FM6)));
            }
            il = new ILBlock
            {
                label = "label"+((long)block.Instructions[0] - firstI).ToString(SR.HEX_FM6),
                instructions = code.ToArray(),
                stackRemaining = stack.ToArray(),
                stackDebt = debt.ToArray()
            };
            return tempReg;
        }

        public static string RewriteCode(GMFile gm, RefData rdata, CodeInfo code)
        {
            var sb = new StringBuilder();
            uint bcv = gm.General.BytecodeVersion;

            var blocks = Decompiler.SplitBlocks(code, bcv);

            string returntype = "void";
            foreach (var block in blocks)
            {
                if (block.Instructions.Length == 0)
                    continue;
                var lastinst = block.Instructions[block.Instructions.Length - 1];
                if (bcv > 0xE && lastinst->OpCode.VersionF == FOpCode.Ret ||
                    lastinst->OpCode.VersionE == EOpCode.Ret)
                {
                    returntype = types[(int)lastinst->SingleType.Type];
                    break;
                }
            }

            sb.AppendFormat("define ")
                .Append(returntype)
                .Append(" @")
                .Append(code.Name)
                .Append("(%var* self) {")
                .AppendLine();

            var locals = new string[0];
            if (gm.FunctionLocals != null)
            {
                foreach (var funclocals in gm.FunctionLocals)
                {
                    if (funclocals.FunctionName == code.Name)
                    {
                        locals = funclocals.LocalNames;
                        break;
                    }
                }
            }

            if (locals.Length > 0)
            {
                sb.AppendLine("alloca:");
                foreach (var localname in locals)
                {
                    sb.Append(SR.INDENT4)
                        .Append("%")
                        .Append(localname)
                        .Append(" = alloca %var")
                        .AppendLine();
                }
            }

            int tempReg = 0;
            ILBlock[] ilBlocks = new ILBlock[blocks.Length];

            for (int i = 0; i < blocks.Length; i++)
            {
                tempReg = RewriteBlock(blocks[i], (long)code.Instructions[0], tempReg, bcv, rdata, gm.Content,
                    out ilBlocks[i]);
            }
            for (int i = 0; i < blocks.Length; i++)
            {
                if (ilBlocks[i].stackDebt.Length > 0)
                {
                    var fromBlocks = new List<int>();
                    for (int j = 0; j < blocks.Length; j++)
                    {
                        if (blocks[j].BranchTo == blocks[i].Instructions[0])
                        {
                            fromBlocks.Add(j);
                        }
                    }

                    var phi = new List<Instruction>(ilBlocks[i].stackDebt.Length);
                    for (int j = 0; j < ilBlocks[i].stackDebt.Length; j++)
                    {
                        phi.Add(new Instruction(ilBlocks[i].stackDebt[j], "phi"));
                    }
                    ilBlocks[i].instructions = phi.Concat(ilBlocks[i].instructions).ToArray();
                }
                sb.Append(ilBlocks[i].ToString());
            }

            if (returntype == "void")
            {
                sb.Append("label")
                    .Append(code.Size.ToString(SR.HEX_FM6))
                    .AppendLine(SR.COLON)
                    .Append(SR.INDENT4)
                    .AppendLine("ret void");
            }

            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}