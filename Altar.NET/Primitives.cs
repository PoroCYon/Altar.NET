using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Altar
{
    using static SR;

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
        public static string ToPrettyString(this DwordBool dwBool) => dwBool.IsTrue() ? TRUE : FALSE;
    }

    /// <summary>
    /// ARGB
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 4, Pack = 1)]
    public struct Colour : IEquatable<Colour>
    {
        readonly static string
            As = "{A=",
            Rs = ", R=",
            Gs = ", G=",
            Bs = ", B=";

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
        public override string ToString   () => As + A + Rs + R + Gs + G + Bs + B + C_BRACE;
        public          string ToHexString() => HASH + data.ToString(HEX_FM8);

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
        internal readonly static string
            Xs = "{X=",
            Ys = ", Y=";

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
        public override string ToString() => Xs + X + Ys + Y + C_BRACE;

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
        internal readonly static string
            Xs = "{X=",
            Ys = ", Y=";

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
        public override string ToString() => Xs + X + Ys + Y + C_BRACE;

#pragma warning disable RECS0018
        public bool Equals(PointF other) => X == other.X && Y == other.Y;
#pragma warning restore RECS0018

        public static bool operator ==(PointF a, PointF b) =>  a.Equals(b);
        public static bool operator !=(PointF a, PointF b) => !a.Equals(b);
    }
    /// <summary>
    /// Carthesian
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Point16 : IEquatable<Point16>
    {
        internal readonly static string
            Xs = "{X=",
            Ys = ", Y=";

        public readonly ushort X, Y;

        public Point16(ushort x, ushort y)
        {
            X = x;
            Y = y;
        }
        public Point16(ushort d)
            : this(d, d)
        {

        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj is Point16)
                return Equals((Point16)obj);

            return false;
        }
        public override int GetHashCode() => X.GetHashCode() | Y.GetHashCode();
        public override string ToString() => Xs + X + Ys + Y + C_BRACE;

        public bool Equals(Point16 other) => X == other.X && Y == other.Y;

        public static bool operator ==(Point16 a, Point16 b) =>  a.Equals(b);
        public static bool operator !=(Point16 a, Point16 b) => !a.Equals(b);
    }

    /// <summary>
    /// AABB
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Rectangle : IEquatable<Rectangle>
    {
        readonly static string Xs = "x";

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
        public override string ToString() => Point.Xs + X + Point.Ys + Y + COMMA_S + Width + Xs + Height + C_BRACE;

        public bool Equals(Rectangle other) => X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;

        public static bool operator ==(Rectangle a, Rectangle b) =>  a.Equals(b);
        public static bool operator !=(Rectangle a, Rectangle b) => !a.Equals(b);
    }
}
