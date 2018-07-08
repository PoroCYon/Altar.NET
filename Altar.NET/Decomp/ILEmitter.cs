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

        struct StackValue
        {
            DataType dataType;
            public string name;
            public StackValue(DataType dt, string n)
            {
                dataType = dt;
                name = n;
            }
            public override string ToString() => types[(int)dataType] + SR.SPACE_S + name;
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
                .Append("() {")
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

            var firstI = (long)code.Instructions[0];
            int tempReg = 0;

            foreach (var block in blocks)
            {
                var stack = new Stack<StackValue>();
                sb.Append(((long)block.Instructions[0] - firstI).ToString(SR.HEX_FM6))
                    .AppendLine(SR.COLON);
                foreach (var inst in block.Instructions)
                {
                    if (inst->Kind(bcv) != InstructionKind.Push && inst->OpCode.General(bcv) != GeneralOpCode.Pop)
                        sb.Append(SR.INDENT4);
                    string dest;
                    switch (inst->Kind(bcv))
                    {
                        case InstructionKind.SingleType:
                            if (inst->OpCode.General(bcv) == GeneralOpCode.Pop)
                            {
                                stack.Pop();
                            }
                            else
                            {
                                dest = "%" + (tempReg++).ToString();
                                sb.Append(dest)
                                    .Append(" = ")
                                    .Append(inst->OpCode.ToPrettyString((int)bcv))
                                    .Append(SR.SPACE_S)
                                    .Append(types[(int)inst->SingleType.Type])
                                    .Append(SR.SPACE_S)
                                    .Append(stack.Pop().name);
                                stack.Push(new StackValue(inst->SingleType.Type, dest));
                            }
                            break;
                        case InstructionKind.DoubleType:
                            dest = "%" + (tempReg++).ToString();
                            StackValue arg2 = new StackValue(DataType.Variable, null);
                            if (inst->OpCode.General(bcv) != GeneralOpCode.Conv &&
                                inst->OpCode.General(bcv) != GeneralOpCode.Not)
                                arg2 = stack.Pop();
                            StackValue arg1 = stack.Pop();
                            sb.Append(dest)
                                .Append(" = ")
                                .Append(inst->OpCode.ToPrettyString((int)bcv))
                                .Append(SR.SPACE_S);
                            if (inst->OpCode.General(bcv) == GeneralOpCode.Cmp)
                                sb.Append(compares[(int)inst->DoubleType.ComparisonType]).Append(SR.SPACE_S);
                            sb.Append(types[(int)inst->DoubleType.Types.Type2])
                                .Append(SR.SPACE_S)
                                .Append(arg1.name);
                            if (arg2.name != null)
                                sb.Append(SR.COMMA_S)
                                    .Append(arg2.name);
                            stack.Push(new StackValue(inst->DoubleType.Types.Type2, dest));
                            break;
                        case InstructionKind.Goto:
                            var a = inst->Goto.Offset.UValue * 4;
                            if ((a & 0xFF000000) != 0)
                            {
                                a &= 0x00FFFFFF;
                                a -= 0x01000000;
                            }
                            sb.Append(inst->OpCode.ToPrettyString((int)bcv))
                                .Append(SR.SPACE_S);
                            if (inst->OpCode.General(bcv) != GeneralOpCode.Br)
                                sb.Append(stack.Pop())
                                    .Append(", ");
                            sb.Append("label %")
                                .Append((((long)inst + unchecked((int)a)) - firstI).ToString(SR.HEX_FM6));
                            if (inst->OpCode.General(bcv) != GeneralOpCode.Br)
                                sb.Append(", label %")
                                    .Append((((long)inst + DisasmExt.Size(inst, bcv)*4) - firstI).ToString(SR.HEX_FM6));
                            break;
                        case InstructionKind.Set:
                            sb.Append("store ")
                                .Append(stack.Pop())
                                .Append(SR.COMMA_S)
                                .Append(types[(int)inst->Set.Types.Type1])
                                .Append(SR.SPACE_S)
                                .Append("%" + rdata.Variables[rdata.VarAccessors[(IntPtr)inst]].Name);
                            break;
                        case InstructionKind.Push:
                            var r = inst->Push.ValueRest;
                            switch (inst->Push.Type)
                            {
                                case DataType.Int16:
                                    stack.Push(new StackValue(inst->Push.Type, inst->Push.Value.ToString(CultureInfo.InvariantCulture)));
                                    break;
                                case DataType.Variable:
                                    var rv = *(Reference*)&r;

                                    /*var instType = (InstanceType)inst->Push.Value;

                                    if (instType <= InstanceType.StackTopOrGlobal)
                                        sb.Append(instType.ToPrettyString());
                                    else
                                    {
                                        var o = SectionReader.GetObjectInfo(gm.Content, (uint)instType, true);

                                        sb.Append('[').Append(o.Name).Append(']');
                                    }
                                    sb.Append(':');*/

                                    var v = rdata.Variables[rdata.VarAccessors[(IntPtr)inst]];

                                    if (rv.Type == VariableType.Array)
                                    {
                                        var index = stack.Pop();
                                        var instance = stack.Pop();
                                        dest = "%" + (tempReg++).ToString();
                                        sb.Append(SR.INDENT4)
                                            .Append(dest)
                                            .Append(" = extractvalue %")
                                            .Append(v.Name)
                                            .Append(SR.SPACE_S)
                                            .Append(index)
                                            .AppendLine();
                                        stack.Push(new StackValue(inst->Push.Type, dest));
                                    }
                                    else
                                    {
                                        stack.Push(new StackValue(inst->Push.Type, "%" + v.Name));
                                    }

                                    /*sb.Append(rv.Type.ToPrettyString());

                                    if (true)
                                    {
                                        sb.Append(' ');
                                        sb.Append(rdata.VarAccessors[(IntPtr)inst]);
                                    }*/

                                    break;
                                case DataType.Boolean:
                                    stack.Push(new StackValue(inst->Push.Type, ((DwordBool*)&r)->ToPrettyString()));
                                    break;
                                case DataType.Double:
                                    stack.Push(new StackValue(inst->Push.Type, ((double*)&r)->ToString(SR.DOUBLE_FMT, CultureInfo.InvariantCulture)));
                                    break;
                                case DataType.Single:
                                    stack.Push(new StackValue(inst->Push.Type, ((float*)&r)->ToString(SR.SINGLE_FMT, CultureInfo.InvariantCulture)));
                                    break;
                                case DataType.Int32:
                                    stack.Push(new StackValue(inst->Push.Type, unchecked((int)r).ToString(CultureInfo.InvariantCulture)));
                                    break;
                                case DataType.Int64:
                                    stack.Push(new StackValue(inst->Push.Type, ((long*)&r)->ToString(CultureInfo.InvariantCulture)));
                                    break;
                                case DataType.String:
                                    stack.Push(new StackValue(inst->Push.Type, SectionReader.GetStringInfo(gm.Content, (uint)r).Escape()));
                                    break;
                            }
                            break;
                        case InstructionKind.Call:
                            dest = "%" + (tempReg++).ToString();
                            sb.Append(dest)
                                .Append(" = call ")
                                .Append(types[(int)inst->Call.ReturnType])
                                .Append(" @")
                                .Append(rdata.Functions[rdata.FuncAccessors[(IntPtr)inst]].Name)
                                .Append(SR.O_PAREN);
                            for (int i = 0; i < inst->Call.Arguments; i++)
                            {
                                sb.Append(stack.Pop());
                                if (i != inst->Call.Arguments-1)
                                    sb.Append(SR.COMMA_S);
                            }
                            sb.Append(SR.C_PAREN);
                            stack.Push(new StackValue(inst->Call.ReturnType, dest));
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
                        .AppendLine((((long)lastinst + DisasmExt.Size(lastinst, bcv)*4) - firstI).ToString(SR.HEX_FM6));
                }
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