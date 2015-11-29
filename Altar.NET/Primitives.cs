using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;

namespace Altar.NET
{
    [Flags]
    public enum DwordBool : uint
    {
        False = 0,
        True  = 1,

        TrueMask = ~False
    }

    public static class DwordBoolExtensions
    {
        public static bool IsTrue(this DwordBool dwBool) => (dwBool & DwordBool.TrueMask) != DwordBool.False;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 3)]
    public struct Int24 : IEquatable<Int24>, IComparable<Int24>, IFormattable, IConvertible
    {
        ushort _word;
        byte _byte;

        public uint UValue
        {
            get
            {
                return _byte | ((uint)_word & 0x0000FF00) | (((uint)_word & 0x000000FF) << 16);
            }
            private set
            {
                unchecked
                {
                    _byte = (byte)(value & 0x000000FF);

                    _word = (ushort)((value & 0x0000FF00) | ((value & 0x00FF0000) >> 16));
                }
            }
        }
        public  int  Value
        {
            get
            {
                var v = UValue;

                uint ur = 0;

                ur |= v & 0x007FFFFF;

                if ((v & 0x00800000) != 0)
                    ur |= 0xFFF00000;

                return unchecked((int)ur);
            }
        }

        public Int24(ushort value)
        {
            _word = 0;
            _byte = 0;

            UValue = value;
        }
        public Int24(byte   value)
        {
            _word = 0;
            _byte = 0;

            UValue = value;
        }
        public Int24(uint   value)
        {
            _word = 0;
            _byte = 0;

            UValue = value;
        }
        public Int24( int   value)
        {
            _word = 0;
            _byte = 0;

            unchecked
            {
                var i = (uint)value;

                UValue = (i & 0x007FFFFFF) | (value < 0 ? (uint)0x00800000 : 0);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj is Int24)
                return Equals((Int24)obj);

            return false;
        }
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();

        public bool Equals(Int24 other) => other._word == _word && other._byte == _byte;

        public int CompareTo(Int24 other) => Value.CompareTo(other.Value);

        public string ToString(string format, IFormatProvider provider) => Value.ToString(format, provider);

        public TypeCode GetTypeCode() => TypeCode.Int32;

        public bool    ToBoolean(IFormatProvider _) => Value != 0;
        public char    ToChar   (IFormatProvider _) => (char)Value;
        public sbyte   ToSByte  (IFormatProvider _) => unchecked((sbyte )Value);
        public byte    ToByte   (IFormatProvider _) => unchecked(( byte )Value);
        public short   ToInt16  (IFormatProvider _) => unchecked(( short)Value);
        public ushort  ToUInt16 (IFormatProvider _) => unchecked((ushort)Value);
        public int     ToInt32  (IFormatProvider _) => Value;
        public uint    ToUInt32 (IFormatProvider _) => UValue;
        public long    ToInt64  (IFormatProvider _) => Value;
        public ulong   ToUInt64 (IFormatProvider _) => UValue;
        public float   ToSingle (IFormatProvider _) => Value;
        public double  ToDouble (IFormatProvider _) => Value;
        public decimal ToDecimal(IFormatProvider _) => Value;

        public DateTime ToDateTime(IFormatProvider _)
        {
            throw new InvalidCastException();
        }

        public string ToString(IFormatProvider provider) => Value.ToString(provider);
        public object ToType(Type type, IFormatProvider provider) => ((IConvertible)Value).ToType(type, provider);

        public static implicit operator Int24(ushort value) => new Int24(value);
        public static implicit operator Int24(byte   value) => new Int24(value);

        public string ToString(string format) => ToString(format, CultureInfo.CurrentCulture);

        public static explicit operator Int24(uint   value) => new Int24(value);
        public static explicit operator Int24(char   value) => new Int24(value);
        public static explicit operator Int24( int   value) => new Int24(value);

        public static implicit operator uint  (Int24 value) => value.UValue;
        public static implicit operator  int  (Int24 value) => value. Value;
        public static explicit operator ushort(Int24 value) => value.ToUInt16(null);
        public static explicit operator byte  (Int24 value) => value.ToByte(null);
        public static explicit operator char  (Int24 value) => value.ToChar(null);

        public static implicit operator string(Int24 value) => value.UValue.ToString();
    }

    /// <summary>
    /// ARGB
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 4, Pack = 1)]
    public struct Colour : IEquatable<Colour>
    {
        [FieldOffset(0)]
        public readonly byte A;
        [FieldOffset(1)]
        public readonly byte R;
        [FieldOffset(2)]
        public readonly byte G;
        [FieldOffset(3)]
        public readonly byte B;

        [FieldOffset(0)]
        readonly uint data;

        public Colour(byte a, byte r, byte g, byte b)
        {
            data = 0;

            A = a;
            R = r;
            G = g;
            B = b;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj is Colour)
                return Equals((Colour)obj);

            return false;
        }
        public override int    GetHashCode() => data.GetHashCode();
        public override string ToString   () => "{A=" + A + ", R=" + R + ", G=" + G + ", B=" + B + "}";
        public          string ToHexString() => "#" + data.ToString("X8");

        public bool Equals(Colour other) => data == other.data;

        public static bool operator ==(Colour a, Colour b) => a.data == b.data;
        public static bool operator !=(Colour a, Colour b) => a.data != b.data;
    }

    /// <summary>
    /// Carthesian
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Point : IEquatable<Point>
    {
        public readonly int X, Y;

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }
        public Point(int d)
            : this(d, d)
        {

        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj is Point)
                return Equals((Point)obj);

            return false;
        }
        public override int GetHashCode() => X.GetHashCode() | Y.GetHashCode();
        public override string ToString() => "{X=" + X + ", Y=" + Y + "}";

        public bool Equals(Point other) => X == other.X && Y == other.Y;

        public static bool operator ==(Point a, Point b) =>  a.Equals(b);
        public static bool operator !=(Point a, Point b) => !a.Equals(b);
    }
    /// <summary>
    /// Carthesian
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PointF : IEquatable<PointF>
    {
        public readonly float X, Y;

        public PointF(float x, float y)
        {
            X = x;
            Y = y;
        }
        public PointF(float d)
            : this(d, d)
        {

        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj is PointF)
                return Equals((PointF)obj);

            return false;
        }
        public override int GetHashCode() => X.GetHashCode() | Y.GetHashCode();
        public override string ToString() => "{X=" + X + ", Y=" + Y + "}";

#pragma warning disable RECS0018
        public bool Equals(PointF other) => X == other.X && Y == other.Y;
#pragma warning restore RECS0018

        public static bool operator ==(PointF a, PointF b) =>  a.Equals(b);
        public static bool operator !=(PointF a, PointF b) => !a.Equals(b);

        public static implicit operator PointF(Point  p) => new PointF(     p.X,      p.Y);
        public static explicit operator Point (PointF p) => new Point ((int)p.X, (int)p.Y);
    }

    /// <summary>
    /// AABB
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Rectangle : IEquatable<Rectangle>
    {
        public readonly int X, Y, Width, Height;

        public Rectangle(int x, int y, int w, int h)
        {
            X      = x;
            Y      = y;
            Width  = w;
            Height = h;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj is Rectangle)
                return Equals((Rectangle)obj);

            return false;
        }
        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode() ^ Width.GetHashCode() ^ Height.GetHashCode();
        public override string ToString() => "{X=" + X + ", Y=" + Y + ", " + Width + "x" + Height + "}";

        public bool Equals(Rectangle other) => X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;

        public static bool operator ==(Rectangle a, Rectangle b) =>  a.Equals(b);
        public static bool operator !=(Rectangle a, Rectangle b) => !a.Equals(b);
    }
}
