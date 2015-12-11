using System;
using System.Collections.Generic;
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
