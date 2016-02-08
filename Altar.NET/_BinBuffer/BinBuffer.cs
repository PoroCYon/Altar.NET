using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Altar
{
    public class BinBuffer : BinBufferResource
    {
        BinBufferResource res;

        public override int Position
        {
            get
            {
                return res.Position;
            }
            set
            {
                res.Position = value;
            }
        }

        public override bool IsEmpty   => res.IsEmpty;
        public override int Size       => res.Size;
        public override int BufferSize => res.BufferSize;

        public BinBuffer(BinBufferResource resource)
        {
            res = resource;
        }
        public BinBuffer()
            : this(new BinBufferByteResource())
        {

        }
        public BinBuffer(int initialCapacity)
            : this(new BinBufferByteResource(initialCapacity))
        {

        }
        public BinBuffer(byte[] data, bool copy = true, bool startAtEnd = false)
            : this(new BinBufferByteResource(data, copy, startAtEnd))
        {

        }
        public BinBuffer(Stream s, bool copy = false, bool dispose = true)
            : this(new BinBufferStreamResource(s, copy, dispose))
        {

        }

        public override void Clear(bool wipeData = false)
        {
            res.Clear(wipeData);
        }

        public override void Write(Union v, int size)
        {
            res.Write(v, size);
        }

        public override void WriteByte(byte value)
        {
            res.WriteByte(value);
        }
        public override void Write(byte[] data, int startIndex, int count)
        {
            res.Write(data, startIndex, count);
        }
        public override void Write(IntPtr data, int size)
        {
            res.Write(data, size);
        }

        public override Union ReadUnion(int size) => res.ReadUnion(size);

        public override byte ReadByte() => res.ReadByte();
        public override int Read(byte[] data, int startIndex, int count) => res.Read(data, startIndex, count);

        public override Stream AsStream   () => res.AsStream   ();
        public override byte[] AsByteArray() => res.AsByteArray();

        protected override void Dispose(bool disposing)
        {
            res.Dispose();
            res = null;
        }

        #region Write methods
        public void Write(byte v)
        {
            WriteByte(v);
        }

        public void Write(bool v)
        {
            WriteByte((byte)(v ? 1 : 0));
        }

        public void Write(sbyte  v)
        {
            Write(unchecked((byte)v));
        }
        // using the Union struct is much faster than splitting up in bytes (only have to check position/size once) or using the bitconverter (it allocates a new array every call)
        public void Write(ushort v)
        {
            Write(new Union { Short0 = v }, sizeof(ushort));
        }
        public void Write( short v)
        {
            Write(new Union { Short0 = unchecked((ushort)v) }, sizeof(short));
        }
        public void Write(uint   v)
        {
            Write(new Union { Int0 = v }, sizeof(uint));
        }
        public void Write( int   v)
        {
            Write(new Union { Int0 = unchecked((uint)v) }, sizeof(uint));
        }
        public void Write(ulong  v)
        {
            Write(new Union { Long0 = v }, sizeof(ulong));
        }
        public void Write( long  v)
        {
            Write(new Union { Long0 = unchecked((ulong)v) }, sizeof(ulong));
        }

        public void Write(float   v)
        {
            Write(new Union { Float0 = v }, sizeof(float));
        }
        public void Write(double  v)
        {
            Write(new Union { Double0 = v }, sizeof(double));
        }
        public void Write(decimal v)
        {
            Write(new Union { Decimal = v }, sizeof(decimal));
        }

        public void Write(char c)
        {
            Write((ushort)c);
        }
        public void WriteASCII(char c)
        {
            Write((byte)c);
        }

        public void Write(TimeSpan   span)
        {
            Write(new Union { Long0 = unchecked((ulong)span.Ticks) }, sizeof(ulong));
        }
        public void Write(DateTime   date)
        {
            Write(new Union { Long0 = unchecked((ulong)date.Ticks), Byte8 = (byte)date.Kind }, sizeof(ulong) + sizeof(byte));
        }
        public void Write(Complex       c)
        {
            Write(new Union { Double0 = c.Real, Double1 = c.Imaginary }, sizeof(double) * 2);
        }
        public void Write(Point         p)
        {
            Write(unchecked(new Union { Int0 = (uint)p.X, Int1 = (uint)p.Y }), sizeof(int) * 2);
        }
        public void Write(Rectangle     r)
        {
            Write(unchecked(new Union { Int0 = (uint)r.X, Int1 = (uint)r.Y, Int2 = (uint)r.Width, Int3 = (uint)r.Height }), sizeof(int) * 4);
        }
        public void Write(Point16       p)
        {
            Write(unchecked(new Union { Short0 = p.X, Short1 = p.Y }), sizeof(ushort) * 2);
        }
        public void Write(Rectangle16   r)
        {
            Write(unchecked(new Union { Short0 = r.X, Short1 = r.Y, Short2 = r.Width, Short3 = r.Height }), sizeof(ushort) * 4);
        }
        public void Write(PointF        p)
        {
            Write(unchecked(new Union { Float0 = p.X, Float1 = p.Y }), sizeof(float) * 2);
        }
        public void Write(Colour        c)
        {
            Write(unchecked(new Union { Byte0 = c.A, Byte1 = c.R, Byte2 = c.G, Byte3 = c.B }), sizeof(byte) * 4);
        }
        public void Write(BoundingBox  bb)
        {
            Write(unchecked(new Union { Int0 = bb.Top, Int1 = bb.Left, Int2 = bb.Right, Int3 = bb.Bottom }), sizeof(uint) * 4);
        }
        public void Write(BoundingBox2 bb)
        {
            Write(unchecked(new Union { Int0 = bb.Left, Int1 = bb.Right, Int2 = bb.Bottom, Int3 = bb.Top }), sizeof(uint) * 4);
        }

        public unsafe void Write(Int24 i)
        {
            var ip = (byte*)&i;
            Write(new Union { Byte0 = ip[0], Byte1 = ip[1], Byte2 = ip[2] }, sizeof(byte) * 3);
        }

        public void Write(byte[] data)
        {
            Write(data, 0, data.Length);
        }

        public void WriteVlq(uint v)
        {
            do
            {
                unchecked
                {
                    var b = (byte)(v & SByte.MaxValue);

                    v >>= 7;

                    if (v != 0)
                        b |= 128;

                    WriteByte(b);
                }
            } while (v != 0);
        }
        public void WriteVlq(BigInteger v)
        {
            do
            {
                unchecked
                {
                    var b = (byte)(v & SByte.MaxValue);

                    v >>= 7;

                    if (v != 0)
                        b |= 128;

                    WriteByte(b);
                }
            } while (v != 0);
        }

        public void Write(string v, Encoding e)
        {
            var d = e.GetBytes(v);

            Write/*Vlq*/(/*(uint)*/d.Length);
            Write(d);
        }
        public void Write(string v)
        {
            Write(v, Encoding.UTF8);
        }
        public void WriteASCII(string v)
        {
            Write(v, Encoding.ASCII);
        }

        public void Write(BigInteger i)
        {
            var d = i.ToByteArray();
            Write(d.Length);
            Write(d);
        }

        public void Write(BinBuffer bb)
        {
            Write(bb, 0, bb.Size);
        }
        public void Write(BinBuffer bb, int startPos, int count)
        {
            var p = bb.Position;
            bb.Position = 0;
            Write(bb.ReadBytes(count), startPos, count);
            bb.Position = p;
        }

        public unsafe void Write<T>(T value)
            where T : struct
        {
            var s = ILHacks.SizeOf<T>();
            var stuff = Marshal.AllocHGlobal(s);
            try
            {
                ILHacks.Cpblk(ref value, (void*)stuff);
                Write(stuff, s);
            }
            finally
            {
                Marshal.FreeHGlobal(stuff);
            }
        }
        #endregion

        #region Read methods
        public bool ReadBoolean() => ReadByte() != 0;

        public sbyte  ReadSByte () => unchecked((sbyte)ReadByte());
        public ushort ReadUInt16() => ReadUnion(sizeof(ushort)).Short0;
        public  short ReadInt16 () => unchecked((short)ReadUnion(sizeof(ushort)).Short0);
        public uint   ReadUInt32() => ReadUnion(sizeof(uint  )).Int0;
        public  int   ReadInt32 () => unchecked((int  )ReadUnion(sizeof(uint  )).Int0  );
        public ulong  ReadUInt64() => ReadUnion(sizeof(ulong )).Long0;
        public  long  ReadInt64 () => unchecked((long )ReadUnion(sizeof(ulong )).Long0 );

        public float   ReadSingle () => ReadUnion(sizeof(float  )).Float1;
        public double  ReadDouble () => ReadUnion(sizeof(double )).Double1;
        public decimal ReadDecimal() => ReadUnion(sizeof(decimal)).Decimal;

        public char ReadChar     () => (char)ReadUInt16();
        public char ReadASCIIChar() => (char)ReadByte  ();

        public TimeSpan     ReadTimeSpan    () => TimeSpan.FromTicks(unchecked((long)ReadUnion(sizeof(ulong)).Long0));
        public DateTime     ReadDateTime    ()
        {
            var u = ReadUnion(sizeof(ulong) + sizeof(byte));
            return new DateTime(unchecked((long)ReadUnion(sizeof(ulong)).Long0), (DateTimeKind)u.Byte1);
        }
        public Complex      ReadComplex     ()
        {
            var u = ReadUnion(sizeof(double) * 2);
            return new Complex(u.Double0, u.Double1);
        }
        public Point        ReadPoint       ()
        {
            var u = ReadUnion(sizeof(int) * 2);
            return unchecked(new Point((int)u.Int0, (int)u.Int1));
        }
        public Rectangle    ReadRectangle   ()
        {
            var u = ReadUnion(sizeof(int) * 4);
            return unchecked(new Rectangle((int)u.Int0, (int)u.Int1, (int)u.Int2, (int)u.Int3));
        }
        public Point16      ReadPoint16v    ()
        {
            var u = ReadUnion(sizeof(ushort) * 2);
            return unchecked(new Point16(u.Short0, u.Short1));
        }
        public Rectangle16  ReadRectangle16 ()
        {
            var u = ReadUnion(sizeof(ushort) * 4);
            return unchecked(new Rectangle16(u.Short0, u.Short1, u.Short2, u.Short3));
        }
        public PointF       ReadPointF      ()
        {
            var u = ReadUnion(sizeof(float) * 2);
            return unchecked(new PointF(u.Float0, u.Float1));
        }
        public Colour       ReadColour      ()
        {
            var u = ReadUnion(sizeof(byte) * 4);
            return new Colour(u.Byte0, u.Byte1, u.Byte2, u.Byte3);
        }
        public BoundingBox  ReadBoundingBox ()
        {
            var u = ReadUnion(sizeof(uint) * 4);
            return new BoundingBox(u.Int0, u.Int1, u.Int2, u.Int3);
        }
        public BoundingBox2 ReadBoundingBox2()
        {
            var u = ReadUnion(sizeof(uint) * 4);
            return new BoundingBox2(u.Int0, u.Int1, u.Int2, u.Int3);
        }

        public unsafe Int24 ReadInt24()
        {
            var u = ReadUnion(sizeof(byte) * 3);
            var i = new Int24();
            var ip = &i;
            ip[0] = u.Byte0;
            ip[1] = u.Byte1;
            ip[2] = u.Byte2;
            return *ip;
        }

        public byte[] ReadBytes(int amount)
        {
            byte[] data = new byte[amount];
            Read(data, 0, amount);
            return data;
        }
        public byte[] ReadBytesLeft() => ReadBytes(BytesLeft);

        public uint ReadVlqUInt32()
        {
            uint r = 0;

            for (int s = 0; s < 32; s += 7)
            {
                var b = ReadByte();

                r |= (uint)((b & SByte.MaxValue) << s);

                if ((b & 128) == 0)
                    break;
            }

            return r;
        }
        public BigInteger ReadVlqBigInteger()
        {
            BigInteger r = 0;

            for (int s = 0; ; s += 7)
            {
                var b = ReadByte();

                r |= (b & SByte.MaxValue) << s;

                if ((b & 128) == 0)
                    break;
            }

            return r;
        }

        public string ReadString     (Encoding e) => e.GetString(ReadBytes(/*(int)*/ReadInt32/*VlqUInt32*/()));
        public string ReadString     (          ) => ReadString(Encoding.UTF8);
        public string ReadASCIIString(          ) => ReadString(Encoding.ASCII);

        public BigInteger ReadBigInteger() => new BigInteger(ReadBytes(unchecked((int)ReadUnion(sizeof(uint)).Int0)));

        public unsafe T ReadStruct<T>()
            where T : struct
        {
            var s = ILHacks.SizeOf<T>();

            fixed (byte* ptr = ReadBytes(s))
            {
                var r = default(T);
                ILHacks.Cpblk((void*)ptr, ref r);
                return r;
            }
        }
        #endregion
    }
}
