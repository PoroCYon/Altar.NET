using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;

namespace Altar
{
    using static SR;

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 3), DebuggerDisplay("{DebugDisplay()}")]
    public struct Int24 : IEquatable<Int24>, IComparable<Int24>, IFormattable, IConvertible
    {
        byte _byte0, _byte1, _byte2;

        public uint UValue
        {
            get
            {
                return _byte0 | (uint)_byte1 << 8 | (uint)_byte2 << 16;
            }
            private set
            {
                unchecked
                {
                    _byte0 = (byte)( value & 0x000000FF       );
                    _byte1 = (byte)((value & 0x0000FF00) >>  8);
                    _byte2 = (byte)((value & 0x00FF0000) >> 16);
                }
            }
        }
        public  int  Value
        {
            get
            {
                var v = UValue;

                int r = 0;

                r = (int)(v & 0x007FFFFF);

                if ((v & 0x00800000) != 0)
                    r *= -1;

                return r;
            }
            set
            {
                var i = (uint)Math.Abs(value);

                UValue = (i & 0x007FFFFFF) | (value < 0 ? (uint)0x00800000 : 0);
            }
        }

        [DebuggerStepThrough]
        public Int24(byte value)
        {
            _byte0 = 0;
            _byte1 = 0;
            _byte2 = 0;

            UValue = value;
        }
        [DebuggerStepThrough]
        public Int24(ushort value)
        {
            _byte0 = 0;
            _byte1 = 0;
            _byte2 = 0;

            UValue = value;
        }
        [DebuggerStepThrough]
        public Int24(uint   value)
        {
            _byte0 = 0;
            _byte1 = 0;
            _byte2 = 0;

            UValue = value;
        }
        public Int24( int   value)
        {
            _byte0 = 0;
            _byte1 = 0;
            _byte2 = 0;

            Value = value;
        }

        [DebuggerStepThrough]
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj is Int24)
                return Equals((Int24)obj);

            return false;
        }
        [DebuggerStepThrough]
        public override int GetHashCode() => Value.GetHashCode();
        [DebuggerStepThrough]
        public override string ToString() => Value.ToString();

        [DebuggerStepThrough]
        public bool Equals(Int24 other) => _byte0 == other._byte0 && _byte1 == other._byte1 && _byte2 == other._byte2;

        [DebuggerStepThrough]
        public int CompareTo(Int24 other) => Value.CompareTo(other.Value);

        [DebuggerStepThrough]
        public string ToString(string format, IFormatProvider provider) => Value.ToString(format, provider);
        [DebuggerStepThrough]
        public string ToString(string format) => ToString(format, CultureInfo.CurrentCulture);

        [DebuggerStepThrough]
        string DebugDisplay() => HEX_PRE + UValue.ToString(HEX_FM6);

        #region IConvertible
        [DebuggerStepThrough]
        public TypeCode GetTypeCode() => TypeCode.Int32;

        [DebuggerStepThrough]
        public bool    ToBoolean(IFormatProvider _) => Value != 0;
        [DebuggerStepThrough]
        public char    ToChar   (IFormatProvider _) => unchecked((char)(UValue & 0xFFFF));
        [DebuggerStepThrough]
        public sbyte   ToSByte  (IFormatProvider _) => unchecked((sbyte ) Value);
        [DebuggerStepThrough]
        public byte    ToByte   (IFormatProvider _) => unchecked(( byte )(UValue & 0xFF));
        [DebuggerStepThrough]
        public short   ToInt16  (IFormatProvider _) => unchecked(( short) Value);
        [DebuggerStepThrough]
        public ushort  ToUInt16 (IFormatProvider _) => unchecked((ushort)(UValue & 0xFFFF));
        [DebuggerStepThrough]
        public int     ToInt32  (IFormatProvider _) => Value;
        [DebuggerStepThrough]
        public uint    ToUInt32 (IFormatProvider _) => UValue;
        [DebuggerStepThrough]
        public long    ToInt64  (IFormatProvider _) => Value;
        [DebuggerStepThrough]
        public ulong   ToUInt64 (IFormatProvider _) => UValue;
        [DebuggerStepThrough]
        public float   ToSingle (IFormatProvider _) => Value;
        [DebuggerStepThrough]
        public double  ToDouble (IFormatProvider _) => Value;
        [DebuggerStepThrough]
        public decimal ToDecimal(IFormatProvider _) => Value;

        [DebuggerStepThrough]
        public DateTime ToDateTime(IFormatProvider _) => ((IConvertible)Value).ToDateTime(_);

        [DebuggerStepThrough]
        public string ToString(IFormatProvider provider) => Value.ToString(provider);
        [DebuggerStepThrough]
        public object ToType(Type type, IFormatProvider provider) => ((IConvertible)Value).ToType(type, provider);
        #endregion

        #region operators
        [DebuggerStepThrough]
        public static implicit operator Int24(ushort value) => new Int24(value);
        [DebuggerStepThrough]
        public static implicit operator Int24(byte   value) => new Int24(value);

        [DebuggerStepThrough]
        public static explicit operator Int24(uint   value) => new Int24(value);
        [DebuggerStepThrough]
        public static explicit operator Int24(char   value) => new Int24(value);
        [DebuggerStepThrough]
        public static explicit operator Int24( int   value) => new Int24(value);
        [DebuggerStepThrough]
        public static explicit operator Int24(ulong  value) => unchecked(new Int24((uint)value));
        [DebuggerStepThrough]
        public static explicit operator Int24( long  value) => unchecked(new Int24(( int)value));

        [DebuggerStepThrough]
        public static implicit operator uint  (Int24 value) => value.UValue;
        [DebuggerStepThrough]
        public static implicit operator  int  (Int24 value) => value. Value;
        [DebuggerStepThrough]
        public static implicit operator ulong (Int24 value) => value.UValue;
        [DebuggerStepThrough]
        public static implicit operator  long (Int24 value) => value. Value;
        [DebuggerStepThrough]
        public static explicit operator ushort(Int24 value) => value.ToUInt16(null);
        [DebuggerStepThrough]
        public static explicit operator byte  (Int24 value) => value.ToByte(null);
        [DebuggerStepThrough]
        public static explicit operator char  (Int24 value) => value.ToChar(null);

        [DebuggerStepThrough]
        public static Int24 operator +(Int24 a, Int24 b) => new Int24(a.Value + b.Value);
        [DebuggerStepThrough]
        public static Int24 operator -(Int24 a, Int24 b) => new Int24(a.Value - b.Value);
        [DebuggerStepThrough]
        public static Int24 operator *(Int24 a, Int24 b) => new Int24(a.Value * b.Value);
        [DebuggerStepThrough]
        public static Int24 operator /(Int24 a, Int24 b) => new Int24(a.Value / b.Value);

        [DebuggerStepThrough]
        public static Int24 operator ++(Int24 v) => new Int24(v.Value + 1);
        [DebuggerStepThrough]
        public static Int24 operator --(Int24 v) => new Int24(v.Value - 1);

        [DebuggerStepThrough]
        public static Int24 operator +(Int24 v) => v;
        [DebuggerStepThrough]
        public static Int24 operator -(Int24 v) => new Int24(-v.Value);

        [DebuggerStepThrough]
        public static Int24 operator &(Int24 a, Int24 b) => new Int24(a.UValue & b.UValue);
        [DebuggerStepThrough]
        public static Int24 operator |(Int24 a, Int24 b) => new Int24(a.UValue | b.UValue);
        [DebuggerStepThrough]
        public static Int24 operator ^(Int24 a, Int24 b) => new Int24(a.UValue ^ b.UValue);

        [DebuggerStepThrough]
        public static Int24 operator <<(Int24 a, int b) => new Int24(a.UValue << b);
        [DebuggerStepThrough]
        public static Int24 operator >>(Int24 a, int b) => new Int24(a.UValue >> b);

        [DebuggerStepThrough]
        public static bool operator > (Int24 a, Int24 b) => a.Value >  b.Value;
        [DebuggerStepThrough]
        public static bool operator < (Int24 a, Int24 b) => a.Value <  b.Value;
        [DebuggerStepThrough]
        public static bool operator >=(Int24 a, Int24 b) => a.Value >= b.Value;
        [DebuggerStepThrough]
        public static bool operator <=(Int24 a, Int24 b) => a.Value <= b.Value;

        [DebuggerStepThrough]
        public static bool operator ==(Int24 a, Int24 b) =>  a.Equals(b);
        [DebuggerStepThrough]
        public static bool operator !=(Int24 a, Int24 b) => !a.Equals(b);
        #endregion
    }
}
