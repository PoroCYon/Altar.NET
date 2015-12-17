using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Altar.NET
{
    using static SR;

    // http://undertale.rawr.ws/decompilation

    public enum DataType : byte
    {
        Double  ,
        Float   ,
        Int32   ,
        Int64   ,
        Boolean ,
        Variable,
        String  ,
        /// <summary>
        /// Unused
        /// </summary>
        Instance,
        Int16 = 0x0F
    }
    /// <summary>
    /// If it's none of the given values, it represents a GameObjectIndex
    /// </summary>
    public enum InstanceType : short
    {
        StackTopOrGlobal,
        Self   = -1,
        Other  = -2,
        All    = -3,
        Noone  = -4,
        Global = -5
    }
    public enum VariableType : byte
    {
        Array,
        StackTop = 0x80,
        Normal   = 0xA0
    }
    public enum OpCode : byte
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
        /// Unary negation or one's complement.
        /// </summary>
        Neg     = 0x0D,
        /// <summary>
        /// Boolean or number negation, or one's complement.
        /// </summary>
        Not     = 0x0E,
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
        Pushenv = 0xBB,
        Popenv  = 0xBC,
        Push    = 0xC0,
        Call    = 0xDA,
        Break   = 0xFF
    }

    public enum InstructionKind : byte
    {
        SingleType,
        DoubleType,
        Goto      ,

        Set       ,
        Push      ,
        Call      ,
        Break
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TypePair
    {
        byte val;

        public DataType Type1 => unchecked((DataType)( val & 0x0F      ));
        public DataType Type2 => unchecked((DataType)((val & 0xF0) >> 4));

        public override string ToString() => Type1.ToPrettyString() + COLON + Type2.ToPrettyString();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Reference
    {
        uint val;

        public VariableType Type                 => unchecked((VariableType)(val >>         24));
        public uint         NextOccurrenceOffset => unchecked((uint        )(val &  0x00FFFFFF));

        public override string ToString() => Type.ToPrettyString() + HEX_PRE + NextOccurrenceOffset.ToString(HEX_FM6);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct Instruction
    {
        public uint InstrData;

        public OpCode          OpCode => (OpCode)((InstrData & 0xFF000000) >> 24);
        public uint            Rest   => (uint  )( InstrData & 0x00FFFFFF       );
        public InstructionKind Kind   => OpCode.Kind();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SingleTypeInstruction
    {
        ushort _padding;
        public DataType Type;
        public OpCode OpCode;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DoubleTypeInstruction
    {
        ushort _padding;
        public TypePair Types;
        public OpCode OpCode;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GotoInstruction
    {
        uint val;

        public OpCode OpCode => (OpCode)((val & 0xFF000000) >> 24);
        public uint   Offset => (uint  )( val & 0x00FFFFFF       );
        //public Int24 Offset;
        //public OpCode OpCode;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SetInstruction
    {
        public InstanceType Instance;
        public TypePair Types;
        public OpCode OpCode;

        public Reference DestVar;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PushInstruction
    {
        public short Value;
        public DataType Type;
        public OpCode OpCode;

        public uint ValueRest;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CallInstruction
    {
        public ushort Arguments;
        public DataType ReturnType;
        public OpCode OpCode;

        public Reference Function;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BreakInstruction
    {
        public ushort Signal;
        public DataType Type;
        public OpCode OpCode;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct AnyInstruction
    {
        [FieldOffset(0)]
        public uint InstrData;
        [FieldOffset(0)]
        public Instruction Generic;

        [FieldOffset(0)]
        public SingleTypeInstruction SingleType;
        [FieldOffset(0)]
        public DoubleTypeInstruction DoubleType;
        [FieldOffset(0)]
        public GotoInstruction Goto;
        [FieldOffset(0)]
        public SetInstruction Set;
        [FieldOffset(0)]
        public PushInstruction Push;
        [FieldOffset(0)]
        public CallInstruction Call;
        [FieldOffset(0)]
        public BreakInstruction Break;

        public OpCode          OpCode => (OpCode)((InstrData & 0xFF000000) >> 24);
        public uint            Rest   => (uint  )( InstrData & 0x00FFFFFF       );
        public InstructionKind Kind   => OpCode.Kind();
    }

    public static class DecompExt
    {
        public static InstructionKind Kind(this OpCode code)
        {
            switch (code)
            {
                case OpCode.Set:
                    return InstructionKind.Set;
                case OpCode.Push:
                    return InstructionKind.Push;
                case OpCode.Call:
                    return InstructionKind.Call;
                case OpCode.Break:
                    return InstructionKind.Break;

                case OpCode.Conv:
                case OpCode.Mul:
                case OpCode.Div:
                case OpCode.Rem:
                case OpCode.Mod:
                case OpCode.Add:
                case OpCode.Sub:
                case OpCode.And:
                case OpCode.Or:
                case OpCode.Xor:
                case OpCode.Not:
                case OpCode.Clt:
                case OpCode.Cle:
                case OpCode.Ceq:
                case OpCode.Cne:
                case OpCode.Cge:
                case OpCode.Cgt:
                    return InstructionKind.DoubleType;

                case OpCode.Dup:
                case OpCode.Neg:
                case OpCode.Ret:
                case OpCode.Exit:
                case OpCode.Pop:
                    return InstructionKind.SingleType;

                case OpCode.Br:
                case OpCode.Brt:
                case OpCode.Brf:
                case OpCode.Pushenv:
                case OpCode.Popenv:
                    return InstructionKind.Goto;
            }

            throw new ArgumentOutOfRangeException(nameof(code));
        }

        public static uint Size(this DataType type)
        {
            switch (type)
            {
                case DataType.Double:
                case DataType.Int64:
                    return 8;
                case DataType.Float:
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

        public static string ToPrettyString(this DataType     type) => type.ToString().ToLowerInvariant();
        public static string ToPrettyString(this InstanceType type) => type.ToString().ToLowerInvariant();
        public static string ToPrettyString(this VariableType type)
        {
            switch (type)
            {
                case VariableType.Array:
                    return BRACKETS;
                case VariableType.StackTop:
                    return ASTERISK;
                case VariableType.Normal:
                    return String.Empty;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
        public static string ToPrettyString(this OpCode       type) => type.ToString().ToLowerInvariant();
    }
}
