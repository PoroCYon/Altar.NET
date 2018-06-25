using Altar.Decomp;
using Altar.Repack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Altar.Recomp
{
    public static class Assembler
    {
        public static CodeInfo DeserializeCodeFromFile(string filename, uint bcv,
            IDictionary<string, uint> stringIndices, IDictionary<string, uint> objectIndices)
        {
            IEnumerable<Instruction> instructions;
            if (filename.ToLowerInvariant().EndsWith(SR.EXT_GML_ASM))
            {
                instructions = Parser.Parse(Tokenizer.Tokenize(File.ReadAllText(filename)));
            }
            else if (filename.ToLowerInvariant().EndsWith(SR.EXT_GML_LSP))
            {
                // TODO
                throw new NotImplementedException();
            }
            else
            {
                throw new InvalidDataException("Unknown code format for '" + filename + "'");
            }
            return DeserializeAssembly(Path.GetFileNameWithoutExtension(filename), instructions, bcv,
                stringIndices, objectIndices);
        }

        private static InstanceType GetInstanceType(InstanceType instanceType, string objName, IDictionary<string, uint> objectIndices)
        {
            if (instanceType <= 0)
            {
                return instanceType;
            }
            else if (objName != null)
            {
                return (InstanceType)objectIndices[objName];
            }
            else
            {
                throw new InvalidDataException("Bad instance tuple");
            }
        }

        public static InstructionKind OpKind(OpCodePair op, uint bcv)
        {
            if (bcv > 0xE)
                switch (op.VersionF)
                {
                    case FOpCode.Set:
                        return InstructionKind.Set;
                    case FOpCode.PushCst:
                    case FOpCode.PushLoc:
                    case FOpCode.PushGlb:
                    case FOpCode.PushVar:
                    case FOpCode.PushI16:
                        return InstructionKind.Push;
                    case FOpCode.Call:
                        return InstructionKind.Call;
                    case FOpCode.Break:
                        return InstructionKind.Break;

                    case FOpCode.Conv:
                    case FOpCode.Mul:
                    case FOpCode.Div:
                    case FOpCode.Rem:
                    case FOpCode.Mod:
                    case FOpCode.Add:
                    case FOpCode.Sub:
                    case FOpCode.And:
                    case FOpCode.Or:
                    case FOpCode.Xor:
                    case FOpCode.Not:
                    case FOpCode.Shl:
                    case FOpCode.Shr:
                    case FOpCode.Cmp:
                        return InstructionKind.DoubleType;

                    case FOpCode.Dup:
                    case FOpCode.Neg:
                    case FOpCode.Ret:
                    case FOpCode.Exit:
                    case FOpCode.Pop:
                        return InstructionKind.SingleType;

                    case FOpCode.Br:
                    case FOpCode.Brt:
                    case FOpCode.Brf:
                    case FOpCode.PushEnv:
                    case FOpCode.PopEnv:
                        return InstructionKind.Goto;
                }
            else
                switch (op.VersionE)
                {
                    case EOpCode.Set:
                        return InstructionKind.Set;
                    case EOpCode.Push:
                        return InstructionKind.Push;
                    case EOpCode.Call:
                        return InstructionKind.Call;
                    case EOpCode.Break:
                        return InstructionKind.Break;

                    case EOpCode.Conv:
                    case EOpCode.Mul:
                    case EOpCode.Div:
                    case EOpCode.Rem:
                    case EOpCode.Mod:
                    case EOpCode.Add:
                    case EOpCode.Sub:
                    case EOpCode.And:
                    case EOpCode.Or:
                    case EOpCode.Xor:
                    case EOpCode.Not:
                    case EOpCode.Shl:
                    case EOpCode.Shr:
                    case EOpCode.Clt:
                    case EOpCode.Cle:
                    case EOpCode.Ceq:
                    case EOpCode.Cne:
                    case EOpCode.Cge:
                    case EOpCode.Cgt:
                        return InstructionKind.DoubleType;

                    case EOpCode.Dup:
                    case EOpCode.Neg:
                    case EOpCode.Ret:
                    case EOpCode.Exit:
                    case EOpCode.Pop:
                        return InstructionKind.SingleType;

                    case EOpCode.Br:
                    case EOpCode.Brt:
                    case EOpCode.Brf:
                    case EOpCode.PushEnv:
                    case EOpCode.PopEnv:
                        return InstructionKind.Goto;
                }

            throw new ArgumentOutOfRangeException();
        }

        public static uint InstSize(Instruction instr, uint bcv)
        {
            switch (OpKind(instr.OpCode, bcv))
            {
                case InstructionKind.SingleType:
                case InstructionKind.DoubleType:
                case InstructionKind.Goto:
                case InstructionKind.Break:
                case InstructionKind.Environment:
                    return 1;
                case InstructionKind.Call:
                    return 2;

                case InstructionKind.Set:
                    //((Set)instr).IsMagic

                    return 2;
                case InstructionKind.Push: // 0xF?
                    var pui = (Push)instr;

                    switch (pui.Type)
                    {
                        case DataType.Int16:
                            return 1;
                        case DataType.Variable:
                            return 2;
                        default:
                            return pui.Type.Size() / sizeof(uint) + 1;
                    }

                default:
                    return 0;
            }
        }

        private static CodeInfo DeserializeAssembly(string name, IEnumerable<Instruction> instructions, uint bcv,
            IDictionary<string, uint> stringIndices, IDictionary<string, uint> objectIndices)
        {
            uint size = 0;
            var labels = new Dictionary<string, uint>();
            foreach (var inst in instructions)
            {
                if (inst is Label labelInst)
                {
                    if (labelInst.LabelValue is string label)
                    {
                        labels[label] = size * sizeof(int);
                    }
                }
                else
                {
                    size += InstSize(inst, bcv);
                }
            }

            IList<Tuple<ReferenceSignature, uint>> functionReferences = new List<Tuple<ReferenceSignature, uint>>();
            IList<Tuple<ReferenceSignature, uint>> variableReferences = new List<Tuple<ReferenceSignature, uint>>();

            var binaryInstructions = new List<AnyInstruction>();
            size = 0;
            foreach (var inst in instructions)
            {
                if (inst is Label)
                {
                    continue;
                }
                var op = new OpCodes { VersionE = inst.OpCode.VersionE, VersionF = inst.OpCode.VersionF };
                var type = DisasmExt.Kind(op, bcv);
                AnyInstruction bininst = new AnyInstruction();
                switch (type)
                {
                    case InstructionKind.Set:
                        var setinst = (Set)inst;
                        bininst.Set = new SetInstruction
                        {
                            DestVar = new Reference(setinst.VariableType, 0),
                            Instance = GetInstanceType(setinst.InstanceType, setinst.InstanceName, objectIndices),
                            OpCode = op,
                            Types = new TypePair(setinst.Type1, setinst.Type2)
                        };
                        variableReferences.Add(new Tuple<ReferenceSignature, uint>(new ReferenceSignature
                        {
                            Name = setinst.TargetVariable,
                            InstanceType = setinst.InstanceType,
                            Instance = setinst.InstanceType == InstanceType.Local ? name : null,
                            VariableType = setinst.VariableType,
                            VariableIndex = setinst.VariableIndex
                        }, size));
                        break;
                    case InstructionKind.Push:
                        var bp = new PushInstruction
                        {
                            OpCode = op,
                            Type = ((Push)inst).Type
                        };
                        if (bp.Type == DataType.Variable)
                        {
                            var p = (PushVariable)inst;
                            bp.Value = (short)GetInstanceType(p.InstanceType, p.InstanceName, objectIndices);
                            bp.ValueRest = new Reference(p.VariableType, 0).val;
                            variableReferences.Add(new Tuple<ReferenceSignature, uint>(new ReferenceSignature
                            {
                                Name = p.VariableName,
                                InstanceType = p.InstanceType,
                                Instance = p.InstanceType == InstanceType.Local ? name : null,
                                VariableType = p.VariableType,
                                VariableIndex = p.VariableIndex
                            }, size));
                        }
                        else
                        {
                            var p = (PushConst)inst;
                            switch (p.Type)
                            {
                                case DataType.Int16:
                                    bp.Value = (short)(long)p.Value;
                                    break;
                                case DataType.Boolean:
                                    bp.ValueRest = (uint)(long)p.Value;
                                    break;
                                case DataType.Double:
                                case DataType.Single:
                                    bp.ValueRest = BitConverter.ToUInt64(BitConverter.GetBytes(Convert.ToDouble(p.Value)), 0);
                                    break;
                                case DataType.Int32:
                                case DataType.Int64:
                                    bp.ValueRest = BitConverter.ToUInt64(BitConverter.GetBytes(unchecked((long)(p.Value))), 0);
                                    break;
                                case DataType.String:
                                    bp.ValueRest = stringIndices[(string)p.Value];
                                    break;
                            }
                        }
                        bininst.Push = bp;
                        break;
                    case InstructionKind.Call:
                        var callinst = (Call)inst;
                        bininst.Call = new CallInstruction
                        {
                            Arguments = (ushort)callinst.Arguments,
                            Function = new Reference(callinst.FunctionType, 0),
                            OpCode = op,
                            ReturnType = callinst.ReturnType
                        };
                        functionReferences.Add(new Tuple<ReferenceSignature, uint>(new ReferenceSignature
                        {
                            Name = callinst.FunctionName,
                            InstanceType = InstanceType.StackTopOrGlobal,
                            VariableType = callinst.FunctionType,
                            VariableIndex = -1
                        }, size));
                        break;
                    case InstructionKind.Break:
                        var breakinst = (Break)inst;
                        bininst.Break = new BreakInstruction
                        {
                            OpCode = op,
                            Signal = (short)breakinst.Signal,
                            Type = breakinst.Type
                        };
                        break;
                    case InstructionKind.DoubleType:
                        var doubleinst = (DoubleType)inst;
                        bininst.DoubleType = new DoubleTypeInstruction
                        {
                            OpCode = op,
                            Types = new TypePair(doubleinst.Type1, doubleinst.Type2)
                        };
                        if (inst is Compare cmpinst)
                        {
                            bininst.DoubleType.ComparisonType = cmpinst.ComparisonType;
                        }
                        break;
                    case InstructionKind.SingleType:
                        var singleinst = (SingleType)inst;
                        bininst.SingleType = new SingleTypeInstruction
                        {
                            OpCode = op,
                            Type = singleinst.Type
                        };
                        if (inst is Dup dupinst)
                        {
                            bininst.SingleType.DupExtra = dupinst.Extra;
                        }
                        break;
                    case InstructionKind.Goto:
                        var gotoinst = (Branch)inst;
                        uint absTarget = 0;
                        if (gotoinst.Label is long)
                        {
                            absTarget = (uint)(long)(gotoinst.Label);
                        }
                        else if (gotoinst.Label is string)
                        {
                            absTarget = labels[(string)gotoinst.Label];
                        }
                        else if (gotoinst.Label == null)
                        {
                            bininst.Goto = new BranchInstruction
                            {
                                Offset = new Int24(0xF00000),
                                OpCode = op
                            };
                            break;
                        }
                        else
                        {
                            Console.Error.WriteLine($"Error in {name}: Can't use label {gotoinst.Label}");
                            break;
                        }
                        var relTarget = (int)absTarget - (int)size;
                        uint offset = unchecked((uint)relTarget);
                        if (relTarget < 0)
                        {
                            offset &= 0xFFFFFF;
                            offset += 0x1000000;
                        }
                        offset /= 4;
                        bininst.Goto = new BranchInstruction
                        {
                            Offset = new Int24(offset),
                            OpCode = op
                        };
                        break;
                    default:
                        Console.Error.WriteLine($"Error in {name}: Unknown instruction type {type}!");
                        continue;
                }
                binaryInstructions.Add(bininst);
                unsafe
                {
                    size += DisasmExt.Size(&bininst, bcv) * 4;
                }
            }

            return new CodeInfo
            {
                Size = (int)size,
                InstructionsCopy = binaryInstructions.ToArray(),
                functionReferences = functionReferences,
                variableReferences = variableReferences
            };
        }

        public static void WriteCodeBlock(BBData data, AnyInstruction[] instructions, uint bytecodeVersion)
        {
            foreach (var inst in instructions)
            {
                var instdata = new BinBuffer();
                instdata.Write(inst);
                uint size;
                unsafe
                {
                    size = DisasmExt.Size(&inst, bytecodeVersion) * 4;
                }
                data.Buffer.Write(instdata, 0, (int)size, 0);
            }
        }

        private static void WriteCodeInfo(BBData data, CodeInfo ci, StringsChunkBuilder strings, uint bytecodeVersion)
        {
            data.Buffer.Write(strings.GetOffset(ci.Name)); // Name
            data.Buffer.Write(ci.Size); // Length
            if (bytecodeVersion > 0xE)
            {
                data.Buffer.Write(ci.ArgumentCount); // ArgumentCount
                data.Buffer.Write(0); // BytecodeOffset
                data.Buffer.Write(0); // pad
            }
            else
            {
                WriteCodeBlock(data, ci.InstructionsCopy, bytecodeVersion);
            }
        }

        private static void AddReferencesOffset(IList<Tuple<ReferenceSignature, uint>> allOffsets,
            IList<Tuple<ReferenceSignature, uint>> subOffsets, long offset)
        {
            foreach (var kv in subOffsets)
            {
                allOffsets.Add(new Tuple<ReferenceSignature, uint>(kv.Item1, (uint)(kv.Item2 + offset)));
            }
        }

        public static int[] WriteCodes(BBData data, GMFile f, StringsChunkBuilder strings)
        {
            int bytecodeSize = 0;
            foreach (var ci in f.Code)
            {
                bytecodeSize += ci.Size;
            }

            Console.WriteLine($"Assembling...");

            BBData[] datas = new BBData[f.Code.Length];
            for (int i = 0; i < f.Code.Length; i++)
            {
                BBData codedata = new BBData(new BinBuffer(), new int[0]);
                WriteCodeInfo(codedata, f.Code[i], strings, f.General.BytecodeVersion);
                datas[i] = codedata;
            }

            data.Buffer.Write(datas.Length);

            var allOffs = data.OffsetOffsets.ToList();

            var offAcc = data.Buffer.Position + datas.Length * sizeof(int); // after all offsets
            if (f.General.BytecodeVersion > 0xE)
            {
                offAcc += bytecodeSize;
            }
            int[] offsets = new int[datas.Length];
            var stringOffsetOffsets = new int[f.Code.Length];
            for (int i = 0; i < datas.Length; i++)
            {
                allOffs.Add(data.Buffer.Position);
                data.Buffer.Write(offAcc);
                offsets[i] = offAcc;

                stringOffsetOffsets[i] = offAcc + 8;

                offAcc += datas[i].Buffer.Size;
            }

            Console.WriteLine($"Linking...");

            IList<Tuple<ReferenceSignature, uint>> functionReferences = new List<Tuple<ReferenceSignature, uint>>();
            IList<Tuple<ReferenceSignature, uint>> variableReferences = new List<Tuple<ReferenceSignature, uint>>();

            variableReferences.Add(new Tuple<ReferenceSignature, uint>(new ReferenceSignature
            {
                Name = "prototype",
                InstanceType = InstanceType.Self,
                VariableType = VariableType.Normal,
                VariableIndex = 0
            }, 0xFFFFFFFF));
            variableReferences.Add(new Tuple<ReferenceSignature, uint>(new ReferenceSignature
            {
                Name = "@@array@@",
                InstanceType = InstanceType.Self,
                VariableType = VariableType.Normal,
                VariableIndex = 1
            }, 0xFFFFFFFF));

            int[] bytecodeOffsets = null;
            if (f.General.BytecodeVersion > 0xE)
            {
                // In >=F bytecodes, the code comes before the info data, which
                // is why this method can't just be a call to WriteList.
                bytecodeOffsets = new int[f.Code.Length];
                for (int i = 0; i < f.Code.Length; i++)
                {
                    bytecodeOffsets[i] = data.Buffer.Position - 12;
                    AddReferencesOffset(functionReferences, f.Code[i].functionReferences, data.Buffer.Position);
                    if (f.Code[i].variableReferences.Count == 0 || f.Code[i].variableReferences[0].Item1.Name != "arguments")
                    {
                        variableReferences.Add(new Tuple<ReferenceSignature, uint>(new ReferenceSignature
                        {
                            Name = "arguments",
                            InstanceType = InstanceType.Local,
                            Instance = f.Code[i].Name,
                            VariableType = VariableType.Normal,
                            VariableIndex = -1
                        }, 0xFFFFFFFF));
                    }
                    AddReferencesOffset(variableReferences, f.Code[i].variableReferences, data.Buffer.Position);
                    WriteCodeBlock(data, f.Code[i].InstructionsCopy, f.General.BytecodeVersion);
                }
            }

            for (int i = 0; i < datas.Length; i++)
            {
                if (f.General.BytecodeVersion > 0xE)
                {
                    datas[i].Buffer.Position = (int)Marshal.OffsetOf(typeof(CodeEntryF), "BytecodeOffset");
                    datas[i].Buffer.Write(bytecodeOffsets[i] - data.Buffer.Position);
                }
                else
                {
                    AddReferencesOffset(functionReferences, f.Code[i].functionReferences, data.Buffer.Position);
                    if (f.Code[i].variableReferences.Count == 0 || f.Code[i].variableReferences[0].Item1.Name != "arguments")
                    {
                        variableReferences.Add(new Tuple<ReferenceSignature, uint>(new ReferenceSignature
                        {
                            Name = "arguments",
                            InstanceType = InstanceType.Local,
                            Instance = f.Code[i].Name,
                            VariableType = VariableType.Normal,
                            VariableIndex = -1
                        }, 0xFFFFFFFF));
                    }
                    AddReferencesOffset(variableReferences, f.Code[i].variableReferences, data.Buffer.Position);
                }
                SectionWriter.Write(data.Buffer, datas[i]);
                allOffs.AddRange(datas[i].OffsetOffsets); // updated by Write
            }

            IList<ReferenceDef> functionStartOffsetsAndCounts;
            ResolveReferenceOffsets(data, functionReferences, strings, false, out functionStartOffsetsAndCounts);

            IList<ReferenceDef> variableStartOffsetsAndCounts;
            ResolveReferenceOffsets(data, variableReferences, strings, true, out variableStartOffsetsAndCounts);

            if (f.RefData.Variables == null || f.RefData.Variables.Length == 0)
            {
                Console.Error.WriteLine("Warning: Variable definitions not pre-loaded. Linking may be inaccurate or lose information.");
            }
            else
            {
                // I tried my best at guessing what these should be, but it wasn't enough.
                // I suspect it may have to do with variable type, since getting
                // one wrong resulted in "tried to index something that isn't an
                // array" (or something to that effect).
                for (int i = 0; i < variableStartOffsetsAndCounts.Count; i++)
                {
                    var v = variableStartOffsetsAndCounts[i];
                    if (i < f.RefData.Variables.Length &&
                        v.Name == f.RefData.Variables[i].Name)// &&
                                                              //(v.InstanceType == f.RefData.Variables[i].InstanceType || v.InstanceType >= InstanceType.StackTopOrGlobal))
                    {
                        v.unknown2 = f.RefData.Variables[i].unknown2;
                        v.InstanceType = f.RefData.Variables[i].InstanceType;
                        variableStartOffsetsAndCounts[i] = v;
                    }
                }
            }

            f.RefData = new RefData
            {
                Functions = functionStartOffsetsAndCounts.ToArray(),
                Variables = variableStartOffsetsAndCounts.ToArray()
            };

            data.OffsetOffsets = allOffs.ToArray();

            return stringOffsetOffsets;
        }

        public static void ResolveReferenceOffsets(BBData data,
            IList<Tuple<ReferenceSignature, uint>> references, StringsChunkBuilder strings, bool extended,
            out IList<ReferenceDef> startOffsetsAndCounts)
        {
            startOffsetsAndCounts = new List<ReferenceDef>();
            int localCount = 0;
            int nonLocalCount = 0;
            for (int i = 0; i < references.Count; i++)
            {
                Tuple<ReferenceSignature, uint> last = references[i];
                uint diff;
                uint existing;
                uint count = 0;
                var targetRef = references[i].Item1;
                var start = references[i].Item2;
                if (targetRef.InstanceType >= InstanceType.StackTopOrGlobal && extended && targetRef.VariableIndex != -1)
                {
                    for (InstanceType possibleInstanceType = InstanceType.Self; possibleInstanceType >= InstanceType.Local; possibleInstanceType--)
                    {
                        for (int j = i + 1; j < references.Count; j++)
                        {
                            if (references[j].Item1.Name == targetRef.Name &&
                                references[j].Item1.InstanceType == possibleInstanceType)
                            {
                                targetRef.InstanceType = references[j].Item1.InstanceType;
                                targetRef.Instance = references[j].Item1.Instance;
                                break;
                            }
                        }
                        if (targetRef.InstanceType < InstanceType.StackTopOrGlobal)
                        {
                            break;
                        }
                    }
                    if (targetRef.InstanceType >= InstanceType.StackTopOrGlobal)
                    {
                        targetRef.InstanceType = InstanceType.Self; // ??
                    }
                }
                if (targetRef.InstanceType == InstanceType.Local && targetRef.Name == "arguments")
                {
                    //localCount = 0;
                }
                if (start != 0xFFFFFFFF)
                {
                    count = 1;
                    for (int j = i + 1; j < references.Count;)
                    {
                        if (references[j].Item1.Name == targetRef.Name &&
                            (!extended ||
                             (targetRef.VariableIndex != -1) ||
                             (references[j].Item1.InstanceType >= InstanceType.StackTopOrGlobal) ||
                             (references[j].Item1.InstanceType == targetRef.InstanceType &&
                              //references[j].Item1.VariableType == targetRef.VariableType &&
                              (references[j].Item1.InstanceType != InstanceType.Local ||
                               references[j].Item1.Instance == targetRef.Instance))) &&
                            ((targetRef.VariableIndex == -1) ||
                             (targetRef.VariableIndex == references[j].Item1.VariableIndex)))
                        {
                            diff = (references[j].Item2 - last.Item2) & 0xFFFFFF;
                            data.Buffer.Position = (int)last.Item2 + 4;
                            existing = data.Buffer.ReadUInt32();
                            data.Buffer.Write(diff | existing);
                            last = references[j];
                            references.RemoveAt(j);
                            count++;
                        }
                        else
                        {
                            j++;
                        }
                    }
                    diff = strings.GetIndex(last.Item1.Name);
                    data.Buffer.Position = (int)last.Item2 + 4;
                    existing = data.Buffer.ReadUInt32();
                    data.Buffer.Write(diff | existing);
                }
                var def = new ReferenceDef
                {
                    FirstOffset = start,
                    HasExtra = extended,
                    InstanceType = targetRef.InstanceType,
                    Name = targetRef.Name,
                    Occurrences = count,
                    unknown2 = targetRef.InstanceType == InstanceType.Local ?
                        localCount : targetRef.VariableType == VariableType.StackTop ?
                            nonLocalCount : -6,
                    VariableType = targetRef.VariableType
                };
                if (targetRef.VariableIndex == -1)
                {
                    startOffsetsAndCounts.Add(def);
                }
                else
                {
                    while (startOffsetsAndCounts.Count <= targetRef.VariableIndex)
                        startOffsetsAndCounts.Add(new ReferenceDef());
                    startOffsetsAndCounts[targetRef.VariableIndex] = def;
                }
                if (targetRef.InstanceType == InstanceType.Local)
                {
                    localCount++;
                }
                else if (targetRef.VariableType == VariableType.StackTop)
                {
                    nonLocalCount++;
                }
            }
        }
    }
}
