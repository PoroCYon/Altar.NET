using Altar.Unpack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

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
            "i8*",    //Variable
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
            protected DataType dataType;

            public StackValue(DataType dt)
            {
                dataType = dt;
            }
            public override string ToString() => types[(int)dataType] + SR.SPACE_S + ShortString();
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

        static int RewriteBlock(Decompiler.CodeBlock block, long firstI, int tempReg, uint bcv, RefData rdata, GMFileContent content,
            out string s, out StackValue[] stackRemaining, out StackValue[] stackDebt)
        {
            var sb = new StringBuilder();
            var stack = new Stack<StackValue>();
            var debt = new Stack<StackValue>();
            sb.Append(((long)block.Instructions[0] - firstI).ToString(SR.HEX_FM6))
                .AppendLine(SR.COLON);
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
            string GetSrc(InstanceType instanceType, ReferenceDef v, DataType dataType, StackValue owner)
            {
                TempRegister addr;
                switch (instanceType)
                {
                    case InstanceType.Global:
                        return GLOBAL + v.Name;
                    case InstanceType.Local:
                        return REG + v.Name;
                    case InstanceType.Self:
                        addr = new TempRegister(dataType, tempReg++);
                        sb.Append(addr.ShortString())
                            .Append(" = getelementptr i8, i8* %self, i32 0, ")
                            .AppendLine(v.Name.Escape())
                            .Append(SR.INDENT4);
                        return addr.ShortString();
                    case InstanceType.StackTopOrGlobal:
                        if (owner is Constant<short> c && (InstanceType)c.value == InstanceType.Self)
                        {
                            addr = new TempRegister(dataType, tempReg++);
                            sb.Append(addr.ShortString())
                                .Append(" = getelementptr i8, i8* %self, i32 0, ")
                                .AppendLine(v.Name.Escape())
                                .Append(SR.INDENT4);
                            return addr.ShortString();
                        }
                        else
                        {
                            addr = new TempRegister(dataType, tempReg++);
                            sb.Append(addr.ShortString())
                                .Append(" = getelementptr i8, ")
                                .Append(owner)
                                .Append(", i32 0, ")
                                .AppendLine(v.Name.Escape())
                                .Append(SR.INDENT4);
                            return addr.ShortString();
                        }
                    default:
                        throw new NotImplementedException();
                }
            }

            foreach (var inst in block.Instructions)
            {
                if (inst->Kind(bcv) != InstructionKind.Push && inst->OpCode.General(bcv) != GeneralOpCode.Pop)
                    sb.Append(SR.INDENT4);
                StackValue dest, arg1, arg2;
                ReferenceDef v;
                Reference rv;
                StackValue owner, index;
                string src;
                switch (inst->Kind(bcv))
                {
                    case InstructionKind.SingleType:
                        if (inst->OpCode.General(bcv) == GeneralOpCode.Pop)
                        {
                            Pop();
                        }
                        else if (inst->OpCode.General(bcv) == GeneralOpCode.Dup)
                        {
                            stack.Push(stack.Peek());
                        }
                        else
                        {
                            arg1 = Pop();
                            dest = new TempRegister(inst->SingleType.Type, tempReg++);
                            sb.Append(dest.ShortString())
                                .Append(" = ")
                                .Append(inst->OpCode.ToPrettyString((int)bcv))
                                .Append(SR.SPACE_S)
                                .Append(types[(int)inst->SingleType.Type])
                                .Append(SR.SPACE_S)
                                .Append(arg1.ShortString());
                            stack.Push(dest);
                        }
                        break;
                    case InstructionKind.DoubleType:
                        dest = new TempRegister(inst->DoubleType.Types.Type2, tempReg++);
                        arg2 = null;
                        if (inst->OpCode.General(bcv) != GeneralOpCode.Conv &&
                            inst->OpCode.General(bcv) != GeneralOpCode.Not)
                            arg2 = Pop();
                        arg1 = Pop();
                        sb.Append(dest.ShortString())
                            .Append(" = ")
                            .Append(inst->OpCode.ToPrettyString((int)bcv))
                            .Append(SR.SPACE_S);
                        if (inst->OpCode.General(bcv) == GeneralOpCode.Cmp)
                            sb.Append(compares[(int)inst->DoubleType.ComparisonType]).Append(SR.SPACE_S);
                        sb.Append(types[(int)inst->DoubleType.Types.Type2])
                            .Append(SR.SPACE_S)
                            .Append(arg1.ShortString());
                        if (arg2 != null)
                            sb.Append(SR.COMMA_S)
                                .Append(arg2.ShortString());
                        stack.Push(dest);
                        break;
                    case InstructionKind.Goto:
                        var a = inst->Goto.Offset.UValue * 4;
                        if ((a & 0xFF000000) != 0)
                        {
                            a &= 0x00FFFFFF;
                            a -= 0x01000000;
                        }
                        var jumpdest = (((long)inst + unchecked((int)a)) - firstI).ToString(SR.HEX_FM6);
                        var nextdest = (((long)inst + DisasmExt.Size(inst, bcv) * 4) - firstI).ToString(SR.HEX_FM6);
                        var iftrue = inst->OpCode.General(bcv) == GeneralOpCode.Brf ? nextdest : jumpdest;
                        var iffalse = inst->OpCode.General(bcv) == GeneralOpCode.Brf ? jumpdest : nextdest;
                        sb.Append("br ");
                        if (inst->OpCode.General(bcv) != GeneralOpCode.Br)
                            sb.Append(Pop())
                                .Append(", ");
                        sb.Append("label %")
                            .Append(iftrue);
                        if (inst->OpCode.General(bcv) != GeneralOpCode.Br)
                            sb.Append(", label %")
                                .Append(iffalse);
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

                        sb.Append("store ")
                            .Append(value)
                            .Append(SR.COMMA_S)
                            .Append(types[(int)inst->Set.Types.Type1])
                            .Append(SR.SPACE_S)
                            .Append(src);
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

                                sb.Append(SR.INDENT4);
                                src = GetSrc((InstanceType)inst->Push.Value, v, inst->Push.Type, owner);
                                dest = new TempRegister(inst->Push.Type, tempReg++);
                                sb.Append(dest.ShortString())
                                    .Append(" = load ")
                                    .Append(types[(int)inst->Push.Type])
                                    .Append(SR.COMMA_S)
                                    .Append(types[(int)inst->Push.Type])
                                    .Append(SR.SPACE_S)
                                    .AppendLine(src);
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
                        sb.Append(dest.ShortString())
                            .Append(" = call ")
                            .Append(types[(int)inst->Call.ReturnType])
                            .Append(" @")
                            .Append(rdata.Functions[rdata.FuncAccessors[(IntPtr)inst]].Name)
                            .Append(SR.O_PAREN);
                        int i = 0;
                        foreach (var arg in args)
                        {
                            sb.Append(arg);
                            if (i != inst->Call.Arguments - 1)
                                sb.Append(SR.COMMA_S);
                            i++;
                        }
                        sb.Append(SR.C_PAREN);
                        stack.Push(dest);
                        break;
                    case InstructionKind.Break:
                    case InstructionKind.Environment:
                    default:
                        sb.Append(inst->OpCode.ToPrettyString((int)bcv));
                        break;
                }
                if (inst->Kind(bcv) != InstructionKind.Push && inst->OpCode.General(bcv) != GeneralOpCode.Pop)
                    sb.AppendLine();
            }
            var lastinst = block.Instructions[block.Instructions.Length - 1];
            if (lastinst->Kind(bcv) != InstructionKind.Goto &&
                !(bcv > 0xE && lastinst->OpCode.VersionF == FOpCode.Ret ||
                    lastinst->OpCode.VersionE == EOpCode.Ret))
            {
                sb.Append(SR.INDENT4)
                    .Append("br label %")
                    .AppendLine((((long)lastinst + DisasmExt.Size(lastinst, bcv) * 4) - firstI).ToString(SR.HEX_FM6));
            }
            s = sb.ToString();
            stackRemaining = stack.ToArray();
            stackDebt = debt.ToArray();
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
                }
            }

            sb.AppendFormat("define ")
                .Append(returntype)
                .Append(" @")
                .Append(code.Name)
                .Append("(i8* self) {")
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
                        .Append(" = alloca i8")
                        .AppendLine();
                }
            }

            int tempReg = 0;

            foreach (var block in blocks)
            {
                string s;
                StackValue[] stackRemaining;
                StackValue[] stackDebt;
                tempReg = RewriteBlock(block, (long)code.Instructions[0], tempReg, bcv, rdata, gm.Content,
                    out s, out stackRemaining, out stackDebt);
                sb.Append(s);
            }

            if (returntype == "void")
            {
                sb.Append(code.Size.ToString(SR.HEX_FM6))
                    .AppendLine(SR.COLON)
                    .Append(SR.INDENT4)
                    .AppendLine("ret void");
            }

            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}