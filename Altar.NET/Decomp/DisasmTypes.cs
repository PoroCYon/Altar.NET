using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace Altar.Decomp
{
    using static SR;

    // http://undertale.rawr.ws/decompilation
    // https://gitlab.com/snippets/14943

    public enum DataType : byte
    {
        Double  ,
        Single  ,
        Int32   ,
        Int64   ,
        Boolean ,
        Variable,
        String  ,
        /// <summary>
        /// Unused
        /// </summary>
        [Obsolete("Unused")]
        Instance,
        Int16 = 0x0F
    }
    /// <summary>
    /// If it's none of the given values, it represents an <see cref="ObjectInfo" /> index.
    /// </summary>
    public enum InstanceType : short
    {
        StackTopOrGlobal = 0,

        Self   = -1,
        Other  = -2,
        All    = -3,
        Noone  = -4,
        Global = -5,

        //TODO: unknown
        Unknown = -6,
        // script-scope local var
        Local   = -7
    }
    public enum VariableType : byte
    {
        Array,
        StackTop = 0x80,
        Normal   = 0xA0,
        //? room-scope local vars?
        Unknown  = 0xE0  //TODO: FIND OUT WHAT THIS IS
    }
    public enum ComparisonType : byte
    {
        LowerThan   = 1,
        LTOrEqual   = 2,
        Equality    = 3,
        Inequality  = 4,
        GTOrEqual   = 5,
        GreaterThan = 6
    }

    public enum EOpCode : byte
    {
        Conv    = 0x03,
        Mul     = 0x04,
        Div     = 0x05,
        Rem     = 0x06,
        Mod     = 0x07,
        Add     = 0x08,
        Sub     = 0x09,
        And     = 0x0A,
        Or      = 0x0B,
        Xor     = 0x0C,
        /// <summary>
        /// Unary negation
        /// </summary>
        Neg     = 0x0D,
        /// <summary>
        /// Bitwise NOT (! for bools, ~ otherwise)
        /// </summary>
        Not     = 0x0E,
        Shl     = 0x0F,
        Shr     = 0x10,
        Clt     = 0x11,
        Cle     = 0x12,
        Ceq     = 0x13,
        Cne     = 0x14,
        Cge     = 0x15,
        Cgt     = 0x16,
        Set     = 0x41,
        Dup     = 0x82,
        Ret     = 0x9D,
        Exit    = 0x9E,
        Pop     = 0x9F,
        Br      = 0xB7,
        Brt     = 0xB8,
        Brf     = 0xB9,
        PushEnv = 0xBB,
        PopEnv  = 0xBC,
        Push    = 0xC0,
        Call    = 0xDA,
        Break   = 0xFF
    }
    public enum FOpCode : byte
    {
        Conv    = 0x07,
        Mul     = 0x08,
        Div     = 0x09,
        Rem     = 0x0A,
        Mod     = 0x0B,
        Add     = 0x0C,
        Sub     = 0x0D,
        And     = 0x0E,
        Or      = 0x0F,
        Xor     = 0x10,
        /// <summary>
        /// Unary negation
        /// </summary>
        Neg     = 0x11,
        /// <summary>
        /// Bitwise NOT (! for bools, ~ otherwise)
        /// </summary>
        Not     = 0x12,
        Shl     = 0x13,
        Shr     = 0x14,
        Cmp     = 0x15,
        Set     = 0x45,
        Dup     = 0x86,
        Ret     = 0x9C,
        Exit    = 0x9D,
        Pop     = 0x9E,
        Br      = 0xB6,
        Brt     = 0xB7,
        Brf     = 0xB8,
        PushEnv = 0xBA,
        PopEnv  = 0xBB,
        PushCst = 0xC0,
        PushLoc = 0xC1,
        PushGlb = 0xC2,
        PushVar = 0xC3,
        PushI16 = 0x84,
        Call    = 0xD9,
        Break   = 0xFF
    }
    public enum GeneralOpCode
    {
        Conv    = 0x03,
        Mul     = 0x04,
        Div     = 0x05,
        Rem     = 0x06,
        Mod     = 0x07,
        Add     = 0x08,
        Sub     = 0x09,
        And     = 0x0A,
        Or      = 0x0B,
        Xor     = 0x0C,
        /// <summary>
        /// Unary negation
        /// </summary>
        Neg     = 0x0D,
        /// <summary>
        /// Bitwise NOT (! for bools, ~ otherwise)
        /// </summary>
        Not     = 0x0E,
        Shl     = 0x0F,
        Shr     = 0x10,
        Cmp     = 0x15,
        Set     = 0x41,
        Dup     = 0x82,
        Ret     = 0x9D,
        Exit    = 0x9E,
        Pop     = 0x9F,
        Br      = 0xB7,
        Brt     = 0xB8,
        Brf     = 0xB9,
        PushEnv = 0xBB,
        PopEnv  = 0xBC,
        Push    = 0xC0,
        Call    = 0xDA,
        Break   = 0xFF
    }

    public enum InstructionKind : byte
    {
        SingleType = 1,
        DoubleType    ,
        Goto          ,
        Set           ,
        Push          ,
        Call          ,
        Break         ,
        Environment
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TypePair
    {
        byte val;

        public DataType Type1 => unchecked((DataType)( val & 0x0F      ));
        public DataType Type2 => unchecked((DataType)((val & 0xF0) >> 4));

        public TypePair(DataType t1, DataType t2)
        {
            val = (byte)((int)t1 | ((int)t2 << 4));
        }

        public override string ToString() => Type1.ToPrettyString() + COLON + Type2.ToPrettyString();
    }
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 1)]
    public struct OpCodes
    {
        [FieldOffset(0)]
        public EOpCode VersionE;
        [FieldOffset(0)]
        public FOpCode VersionF;

        public override string ToString() => VersionE + SLASH + VersionF;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Reference
    {
        uint val;

        public VariableType Type                 => (VariableType)(val >> 24);
        public uint         NextOccurrenceOffset => (val & 0x00FFFFFF);

        public override string ToString() => Type.ToPrettyString() + HEX_PRE + NextOccurrenceOffset.ToString(HEX_FM6);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SingleTypeInstruction
    {
        public ushort DupExtra;
        public DataType Type;
        public OpCodes OpCode;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DoubleTypeInstruction
    {
        byte _pad;
        public ComparisonType ComparisonType;
        public TypePair Types;
        public OpCodes OpCode;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BranchInstruction
    {
        public Int24 Offset ;
        public OpCodes OpCode;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SetInstruction
    {
        public InstanceType Instance;
        public TypePair Types;
        public OpCodes OpCode;

        public Reference DestVar;

        public unsafe bool IsMagic
        {
            get
            {
                fixed (SetInstruction* self = &this)
                {
                    return *(uint*)self == DisasmExt.SetArrayMagic;
                }
            }
        }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PushInstruction
    {
        public short Value;
        public DataType Type;
        public OpCodes OpCode;

        public uint ValueRest;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CallInstruction
    {
        public ushort Arguments;
        public DataType ReturnType;
        public OpCodes OpCode;

        public Reference Function;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BreakInstruction
    {
        public short Signal;
        public DataType Type;
        public OpCodes OpCode;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct AnyInstruction
    {
        [FieldOffset(0)]
        public uint InstrData;

        [FieldOffset(0)]
        public Int24 Rest;
        [FieldOffset(3)]
        public OpCodes OpCode;

        [FieldOffset(0)]
        public SingleTypeInstruction SingleType;
        [FieldOffset(0)]
        public DoubleTypeInstruction DoubleType;
        [FieldOffset(0)]
        public BranchInstruction Goto;
        [FieldOffset(0)]
        public SetInstruction Set;
        [FieldOffset(0)]
        public PushInstruction Push;
        [FieldOffset(0)]
        public CallInstruction Call;
        [FieldOffset(0)]
        public BreakInstruction Break;
    }

    public struct RefData
    {
        public ReferenceDef[] Variables, Functions;
        public IDictionary<IntPtr, int> VarAccessors, FuncAccessors;
    }

    public unsafe static class DisasmExt
    {
        internal const uint SetArrayMagic = 0x455F0006;
        internal readonly static SetInstruction MagicSetInstr = new SetInstruction
        {
            Instance = (InstanceType)6,
            OpCode   = new OpCodes { VersionF = FOpCode.Set },
            Types    = new TypePair(DataType.Int16, DataType.Variable)
        };

        [DebuggerHidden]
        public static InstructionKind Kind(this OpCodes code, uint bcv)
        {
            if (bcv > 0xE)
                switch (code.VersionF)
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
                switch (code.VersionE)
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

            throw new ArgumentOutOfRangeException(nameof(code));
        }

        [DebuggerHidden]
        public static uint Size(this DataType type)
        {
            switch (type)
            {
                case DataType.Double:
                case DataType.Int64:
                    return 8;
                case DataType.Single:
                case DataType.Int32:
                case DataType.Boolean:
                    return 4;
                case DataType.Variable:
                    return 8;
                case DataType.String:
                    return 4;
                case DataType.Int16:
                    return 2;
                //case DataType.Instance:
                //    return 0;
            }

            throw new ArgumentOutOfRangeException(nameof(type));
        }

        [DebuggerHidden]
        public static uint            Rest(this AnyInstruction instr) => instr.InstrData & 0x00FFFFFF;
        [DebuggerHidden]
        public static InstructionKind Kind(this AnyInstruction instr, uint bcv) => instr.OpCode.Kind(bcv);

#pragma warning disable 618
        [DebuggerHidden]
        public static string ToPrettyString(this DataType     type) => type == DataType.Variable ? VAR : type == DataType.Instance ? INST : type == DataType.Boolean ? BOOL : type.ToString().ToLowerInvariant();
#pragma warning restore 618
        [DebuggerHidden]
        public static string ToPrettyString(this InstanceType type) => type == InstanceType.StackTopOrGlobal ? STOG : type.ToString().ToLowerInvariant();
        [DebuggerHidden]
        public static string ToPrettyString(this VariableType type)
        {
            switch (type)
            {
                case VariableType.Array:
                    return BRACKETS;
                case VariableType.StackTop:
                    return ASTERISK;
                case VariableType.Unknown:
                    return QMARK;
                case VariableType.Normal:
                    return String.Empty;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
        [DebuggerHidden]
        public static string ToPrettyString(this FOpCode type)
        {
            switch (type)
            {
                case FOpCode.PushCst:
                    return "push.cst";
                case FOpCode.PushLoc:
                    return "push.loc";
                case FOpCode.PushGlb:
                    return "push.glb";
                case FOpCode.PushI16:
                    return "push.i16";
                case FOpCode.PushVar:
                    return "push.var";
            }

            return type.ToString().ToLowerInvariant();
        }
        [DebuggerHidden]
        public static string ToPrettyString(this OpCodes     type, int bcv) => bcv > 0xE ? type.VersionF.ToPrettyString() : type.VersionE.ToString().ToLowerInvariant();
        [DebuggerHidden]
        public static string ToPrettyString(this ComparisonType type)
        {
            switch (type)
            {
                case ComparisonType.Equality:
                    return EQUAL;
                case ComparisonType.Inequality:
                    return NEQUAL;
                case ComparisonType.GreaterThan:
                    return GT;
                case ComparisonType.LowerThan:
                    return LT;
                case ComparisonType.GTOrEqual:
                    return GTE;
                case ComparisonType.LTOrEqual:
                    return LTE;
            }

            throw new ArgumentOutOfRangeException(nameof(type));
        }

        public unsafe static uint Size(AnyInstruction* pInstr, uint bcv)
        {
            switch (pInstr->Kind(bcv))
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
                    if (*(uint*)pInstr == SetArrayMagic) // some magic number that has something to do with arrays
                        return 1;

                    return 2;
                case InstructionKind.Push: // 0xF?
                    var pui = (PushInstruction*)pInstr;

                    switch (pui->Type)
                    {
                        case DataType.Int16:
                            return 1;
                        case DataType.Variable:
                            return 2;
                        default:
                            return pui->Type.Size() / sizeof(uint) + 1;
                    }

                default:
                    return 0;
            }
        }

        [DebuggerHidden]
        public static GeneralOpCode General(this OpCodes code, uint bcv)
        {
            if (bcv > 0xE)
                switch (code.VersionF)
                {
                    case FOpCode.Conv:
                        return GeneralOpCode.Conv;
                    case FOpCode.Mul:
                        return GeneralOpCode.Mul;
                    case FOpCode.Div:
                        return GeneralOpCode.Div;
                    case FOpCode.Rem:
                        return GeneralOpCode.Rem;
                    case FOpCode.Mod:
                        return GeneralOpCode.Mod;
                    case FOpCode.Add:
                        return GeneralOpCode.Add;
                    case FOpCode.Sub:
                        return GeneralOpCode.Sub;
                    case FOpCode.And:
                        return GeneralOpCode.And;
                    case FOpCode.Or:
                        return GeneralOpCode.Or;
                    case FOpCode.Xor:
                        return GeneralOpCode.Xor;
                    case FOpCode.Neg:
                        return GeneralOpCode.Neg;
                    case FOpCode.Not:
                        return GeneralOpCode.Not;
                    case FOpCode.Shl:
                        return GeneralOpCode.Shl;
                    case FOpCode.Shr:
                        return GeneralOpCode.Shr;
                    case FOpCode.Cmp:
                        return GeneralOpCode.Cmp;
                    case FOpCode.Set:
                        return GeneralOpCode.Set;
                    case FOpCode.Dup:
                        return GeneralOpCode.Dup;
                    case FOpCode.Ret:
                        return GeneralOpCode.Ret;
                    case FOpCode.Exit:
                        return GeneralOpCode.Exit;
                    case FOpCode.Pop:
                        return GeneralOpCode.Pop;
                    case FOpCode.Br:
                        return GeneralOpCode.Br;
                    case FOpCode.Brt:
                        return GeneralOpCode.Brt;
                    case FOpCode.Brf:
                        return GeneralOpCode.Brf;
                    case FOpCode.PushEnv:
                        return GeneralOpCode.PushEnv;
                    case FOpCode.PopEnv:
                        return GeneralOpCode.PopEnv;
                    case FOpCode.PushCst:
                    case FOpCode.PushLoc:
                    case FOpCode.PushGlb:
                    case FOpCode.PushVar:
                    case FOpCode.PushI16:
                        return GeneralOpCode.Push;
                    case FOpCode.Call:
                        return GeneralOpCode.Call;
                    case FOpCode.Break:
                        return GeneralOpCode.Break;
                    default:
                        return 0;
                }

            switch (code.VersionE)
            {
                case EOpCode.Ceq:
                case EOpCode.Cge:
                case EOpCode.Cgt:
                case EOpCode.Cle:
                case EOpCode.Clt:
                case EOpCode.Cne:
                    return GeneralOpCode.Cmp;
                default:
                    return (GeneralOpCode)code.VersionE;
            }
        }
    }
}
