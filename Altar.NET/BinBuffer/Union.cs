using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Altar
{
    // ugly, but better than allocating new byte arrays (System.BitConverter)

    [StructLayout(LayoutKind.Explicit, Size = sizeof(decimal))]
    public struct Union
    {
        [FieldOffset(0x0)]
        public byte Byte0;
        [FieldOffset(0x1)]
        public byte Byte1;
        [FieldOffset(0x2)]
        public byte Byte2;
        [FieldOffset(0x3)]
        public byte Byte3;
        [FieldOffset(0x4)]
        public byte Byte4;
        [FieldOffset(0x5)]
        public byte Byte5;
        [FieldOffset(0x6)]
        public byte Byte6;
        [FieldOffset(0x7)]
        public byte Byte7;
        [FieldOffset(0x8)]
        public byte Byte8;
        [FieldOffset(0x9)]
        public byte Byte9;
        [FieldOffset(0xA)]
        public byte ByteA;
        [FieldOffset(0xB)]
        public byte ByteB;
        [FieldOffset(0xC)]
        public byte ByteC;
        [FieldOffset(0xD)]
        public byte ByteD;
        [FieldOffset(0xE)]
        public byte ByteE;
        [FieldOffset(0xF)]
        public byte ByteF;

        [FieldOffset(0x0)]
        public ushort Short0;
        [FieldOffset(0x2)]
        public ushort Short1;
        [FieldOffset(0x4)]
        public ushort Short2;
        [FieldOffset(0x6)]
        public ushort Short3;
        [FieldOffset(0x8)]
        public ushort Short4;
        [FieldOffset(0xA)]
        public ushort Short5;
        [FieldOffset(0xC)]
        public ushort Short6;
        [FieldOffset(0xE)]
        public ushort Short7;

        [FieldOffset(0x0)]
        public uint Int0;
        [FieldOffset(0x4)]
        public uint Int1;
        [FieldOffset(0x8)]
        public uint Int2;
        [FieldOffset(0xC)]
        public uint Int3;

        [FieldOffset(0)]
        public ulong Long0;
        [FieldOffset(8)]
        public ulong Long1;

        [FieldOffset(0x0)]
        public float Float0;
        [FieldOffset(0x4)]
        public float Float1;
        [FieldOffset(0x8)]
        public float Float2;
        [FieldOffset(0xC)]
        public float Float3;

        [FieldOffset(0)]
        public double Double0;
        [FieldOffset(8)]
        public double Double1;

        [FieldOffset(0)]
        public decimal Decimal;

        public unsafe static byte ByteAt (Union* ptr, uint index        ) => ((byte*)ptr)[index];
        public unsafe static void SetByte(Union* ptr, uint index, byte v) => ((byte*)ptr)[index] = v;
    }
}
