using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Altar.NET
{
    // http://undertale.rawr.ws/decompilation

    public static class Disassembler
    {
        public unsafe static Instruction*[] DisassembleCode(GMFileContent content, uint id)
        {
            if (id >= content.Code->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var re = (CodeEntry*)GMFile.PtrFromOffset(content, (&content.Code->Offset)[id]);
            var len = re->Length;
            var bc = (byte*)&re->Bytecode;

            var ret = new List<IntPtr>(); // doesn't like T* as type arg

            var bcb = (uint*)bc;

            var l = Utils.PadTo(len, 4);
            Instruction* instr;

            for (uint i = 0; i * 4 < l; )
            {
                instr = (Instruction*)(bcb + i);

                ret.Add((IntPtr)instr);

                uint blocks = 0;

                switch (instr->Kind)
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

            var retarr = new Instruction*[ret.Count];

            for (int i = 0; i < retarr.Length; i++)
                retarr[i] = (Instruction*)ret[i];

            return retarr;
        }

        public unsafe static string DisplayInstructions(GMFileContent content, Instruction*[] instrs)
        {
            var vars = SectionReader.GetRefDefs(content, content.Variables);
            var fns  = SectionReader.GetRefDefs(content, content.Functions);

            var varAccs = new uint[vars.Length][];
            var  fnAccs = new uint[fns .Length][];

            for (int i = 0; i < vars.Length; i++)
            {
                var arr = new uint[vars[i].Occurrences];
                int j = 0;

                var p = (Instruction*)GMFile.PtrFromOffset(content, vars[i].FirstAddress);
                var dest = ((&p->InstrData)[1] & 0xFFFFFF00) >> 8;

                while (dest != 0 && j < vars[i].Occurrences)
                {
                    arr[j++] = dest;

                    p = (Instruction*)((byte*)p + dest * 4);

                    if (j < vars[i].Occurrences - 1)
                        dest = ((&p->InstrData)[1] & 0xFFFFFF00) >> 8;
                }

                Debug.Assert(j == vars[i].Occurrences);

                varAccs[i] = arr;
            }

            for (int i = 0; i < fns.Length; i++)
            {
                var arr = new uint[fns[i].Occurrences];
                int j = 0;

                var p = (Instruction*)GMFile.PtrFromOffset(content, fns[i].FirstAddress);
                var dest = ((&p->InstrData)[1] & 0xFFFFFF00) >> 8;

                while (dest != 0 && j < fns[i].Occurrences)
                {
                    arr[j++] = dest;

                    p = (Instruction*)((byte*)p + dest * 4);

                    if (j < fns[i].Occurrences - 1)
                        dest = ((&p->InstrData)[1] & 0xFFFFFF00) >> 8;
                }

                Debug.Assert(j == fns[i].Occurrences);

                fnAccs[i] = arr;
            }

            var sb = new StringBuilder();

            for (int i = 0; i < instrs.Length; i++)
            {
                var iptr = instrs[i];

                sb.Append(iptr->OpCode.ToPrettyString()).Append(' ');

                switch (iptr->Kind)
                {
                    case InstructionKind.SingleType:
                        var st = *(SingleTypeInstruction*)iptr;

                        sb.Append(st.Type.ToPrettyString());
                        break;
                    case InstructionKind.DoubleType:
                        var dt = *(DoubleTypeInstruction*)iptr;

                        sb.Append(dt.Types);
                        break;
                    case InstructionKind.Goto:
                        var g = *(GotoInstruction*)iptr;

                        sb.Append("0x").Append(g.Offset.ToString("X6"));
                        break;

                    case InstructionKind.Set:
                        var s = *(SetInstruction*)iptr;

                        sb.Append(s.Types).Append(' ');

                        if (s.Instance <= InstanceType.StackTopOrGlobal)
                            sb.Append(s.Instance.ToPrettyString());
                        else
                        {
                            var o = SectionReader.GetObjectInfo(content, (uint)s.Instance);

                            sb.Append('[').Append(o.Name).Append(']');
                        }

                        sb.Append(':').Append(s.DestVar);
                        break;
                    case InstructionKind.Push:
                        var pp = (PushInstruction*)iptr;
                        var p = *pp;

                        sb.Append(p.Type.ToPrettyString()).Append(' ');

                        var r = p.ValueRest;

                        if (p.Type == DataType.Int16)
                            sb.Append(p.Value);
                        if (p.Type == DataType.Variable)
                        {
                            var rv = *(Reference*)&r;

                            var inst = (InstanceType)p.Value;

                            if (inst <= InstanceType.StackTopOrGlobal)
                                sb.Append(inst.ToPrettyString());
                            else
                            {
                                var o = SectionReader.GetObjectInfo(content, (uint)inst);

                                sb.Append('[').Append(o.Name).Append(']');
                            }
                            sb.Append(' ').Append(rv);
                        }
                        if (p.Type == DataType.Boolean)
                        {
                            var bv = *(DwordBool*)&r;

                            sb.Append(bv.IsTrue() ? "true" : "false");
                        }
                        if (p.Type == DataType.Double)
                        {
                            var dv = *(double*)&r;

                            sb.Append(dv.ToString(CultureInfo.InvariantCulture));
                        }
                        if (p.Type == DataType.Float)
                        {
                            var fv = *(float*)&r;

                            sb.Append(fv.ToString(CultureInfo.InvariantCulture));
                        }
                        if (p.Type == DataType.Int32)
                            sb.Append((int)p.ValueRest);
                        if (p.Type == DataType.Int64)
                            sb.Append((long)p.ValueRest);
                        if (p.Type == DataType.String)
                            sb.Append("S:").Append(p.ValueRest).Append(' ')
                                .Append('"').Append(SectionReader.GetStringInfo(content, p.ValueRest)).Append('"');
                        break;

                    case InstructionKind.Call:
                        var c = *(CallInstruction*)iptr;

                        sb.Append(c.ReturnType.ToPrettyString()).Append(':')
                            .Append(c.Arguments).Append(' ')
                            .Append(c.Function);
                        break;
                    case InstructionKind.Break:
                        var b = *(BreakInstruction*)iptr;

                        sb.Append(b.Type.ToPrettyString()).Append(' ').Append(b.Signal);
                        break;
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
