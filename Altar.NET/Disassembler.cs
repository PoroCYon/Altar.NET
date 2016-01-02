using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Altar
{
    using static SR;

    // http://undertale.rawr.ws/decompilation

    public unsafe static class Disassembler
    {
        public static CodeInfo DisassembleCode(GMFileContent content, uint id)
        {
            if (id >= content.Code->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var re = (CodeEntry*)GMFile.PtrFromOffset(content, (&content.Code->Offsets)[id]);
            var len = re->Length;
            var bc = &re->Bytecode;

            var ret = new List<IntPtr>(); // doesn't like T* as type arg

            var bcb = (uint*)bc;

            var l = Utils.PadTo(len, 4);
            AnyInstruction* instr;

            for (uint i = 0; i * 4 < l; )
            {
                instr = (AnyInstruction*)(bcb + i);

                ret.Add((IntPtr)instr);

                uint blocks = 0;

                switch (instr->Kind())
                {
                    case InstructionKind.SingleType:
                    case InstructionKind.DoubleType:
                    case InstructionKind.Goto:
                    case InstructionKind.Break:
                        blocks = 1;
                        break;
                    case InstructionKind.Call:
                    case InstructionKind.Set:
                        blocks = 2;
                        break;
                    case InstructionKind.Push:
                        var pInstr = (PushInstruction*)instr;

                        switch (pInstr->Type)
                        {
                            case DataType.Int16:
                                blocks = 1;
                                break;
                            case DataType.Variable:
                                blocks = 2;
                                break;
                            default:
                                blocks = pInstr->Type.Size() / sizeof(uint) + 1;
                                break;
                        }
                        break;
                }

                i += blocks;
            }

            var retarr = new AnyInstruction*[ret.Count];

            for (int i = 0; i < retarr.Length; i++)
                retarr[i] = (AnyInstruction*)ret[i];

            var retfinal = new CodeInfo();

            retfinal.Name = SectionReader.StringFromOffset(content, re->Name);
            retfinal.Instructions = retarr;

            return retfinal;
        }

        public static Dictionary<IntPtr, int> GetReferenceTable(GMFileContent content, ReferenceDef[] defs)
        {
            var ret = new Dictionary<IntPtr, int>(defs.Length);

            for (int i = 0; i < defs.Length; i++)
            {
                var offTotal = defs[i].FirstOffset;
                var addr     = (Instruction*)GMFile.PtrFromOffset(content, offTotal);

                for (int j = 0; j < defs[i].Occurrences /*&& curOffset != 0*/; j++)
                {
                    ret.Add((IntPtr)addr, i);

                    if (j < defs[i].Occurrences - 1) // at least one more iteration afterwards
                    {
                        var off = ((uint*)addr)[1] & 0x00FFFFFF;

                        addr = (Instruction*)GMFile.PtrFromOffset(content, offTotal += off); //! '+=', not '+'
                    }
                }
            }

            return ret;
        }

        public static string DisplayInstructions(GMFileContent content, Dictionary<IntPtr, int> varAccs, Dictionary<IntPtr, int> fnAccs, CodeInfo code, AnyInstruction*[] instructions = null)
        {
            var instrs = instructions ?? code.Instructions;

            if (instrs.Length == 0)
                return String.Empty;

            var vars = SectionReader.GetRefDefs(content, content.Variables);
            var fns  = SectionReader.GetRefDefs(content, content.Functions);

            var sb = new StringBuilder();

            var firstInstr = code.Instructions[0];

            for (int i = 0; i < instrs.Length; i++)
            {
                var iptr = instrs[i];
                var relInstr = (long)iptr - (long)firstInstr;

                sb  .Append(HEX_PRE).Append(relInstr.ToString(HEX_FM8))
                    .Append(' ').Append(iptr->Code().ToPrettyString()).Append(' ');

                switch (iptr->Kind())
                {
                    case InstructionKind.SingleType:
                        var st = iptr->SingleType;

                        sb.Append(st.Type.ToPrettyString());
                        break;
                    case InstructionKind.DoubleType:
                        var dt = iptr->DoubleType;

                        sb.Append(dt.Types);
                        break;
                    case InstructionKind.Goto:
                        var g = iptr->Goto;

                        sb.Append(HEX_PRE).Append((relInstr + g.Offset).ToString(HEX_FM6));
                        break;

                    #region set
                    case InstructionKind.Set:
                        var s = iptr->Set;

                        sb.Append(s.Types).Append(' ');

                        if (s.Instance <= InstanceType.StackTopOrGlobal)
                            sb.Append(s.Instance.ToPrettyString());
                        else
                        {
                            var o = SectionReader.GetObjectInfo(content, (uint)s.Instance);

                            sb.Append('[').Append(o.Name).Append(']');
                        }

                        sb.Append(':');

                        sb.Append(vars[varAccs[(IntPtr)iptr]].Name);
                        sb.Append(s.DestVar.Type.ToPrettyString());
                        break;
                    #endregion
                    #region push
                    case InstructionKind.Push:
                        var pp = (PushInstruction*)iptr;
                        var p = iptr->Push;

                        sb.Append(p.Type.ToPrettyString()).Append(' ');

                        var r = p.ValueRest;

                        switch (p.Type)
                        {
                            case DataType.Int16:
                                sb.Append(p.Value.ToString(CultureInfo.InvariantCulture));
                                break;
                            case DataType.Variable:
                                var rv = *(Reference*)&r;

                                var inst = (InstanceType)p.Value;

                                if (inst <= InstanceType.StackTopOrGlobal)
                                    sb.Append(inst.ToPrettyString());
                                else
                                {
                                    var o = SectionReader.GetObjectInfo(content, (uint)inst);

                                    sb.Append('[').Append(o.Name).Append(']');
                                }
                                sb.Append(':');

                                sb.Append(vars[varAccs[(IntPtr)iptr]].Name);
                                sb.Append(rv.Type.ToPrettyString());
                                break;
                            case DataType.Boolean:
                                sb.Append(((DwordBool*)&r)->ToPrettyString());
                                break;
                            case DataType.Double:
                                sb.Append(((double*)&r)->ToString(CultureInfo.InvariantCulture));
                                break;
                            case DataType.Single:
                                sb.Append(((float*)&r)->ToString(CultureInfo.InvariantCulture));
                                break;
                            case DataType.Int32:
                                sb.Append(unchecked((int)r).ToString(CultureInfo.InvariantCulture));
                                break;
                            case DataType.Int64:
                                sb.Append(((long*)&pp->ValueRest)->ToString(CultureInfo.InvariantCulture));
                                break;
                            case DataType.String:
                                sb.Append('"').Append(SectionReader.GetStringInfo(content, p.ValueRest)).Append('"');
                                break;
                        }
                        break;
                    #endregion
                    #region call
                    case InstructionKind.Call:
                        var c = iptr->Call;

                        sb.Append(c.ReturnType.ToPrettyString()).Append(':')
                            .Append(c.Arguments).Append(' ');

                        sb.Append(fns[fnAccs[(IntPtr)iptr]].Name);
                        sb.Append(c.Function.Type.ToPrettyString());
                        break;
                    #endregion

                    case InstructionKind.Break:
                        var b = iptr->Break;

                        sb.Append(b.Type.ToPrettyString()).Append(' ').Append(b.Signal);
                        break;
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
