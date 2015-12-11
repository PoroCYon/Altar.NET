using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;

namespace Altar.NET
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 3), DebuggerDisplay("{DebugDisplay()}")]
    public struct Int24 : IEquatable<Int24>, IComparable<Int24>, IFormattable, IConvertible
    {
        readonly static string HEXP = "0x", HEXF = "x6";

        ushort _word;
        byte   _byte;

        public uint UValue
        {
            get
            {
                return _byte | ((uint)_word & 0x0000FF00) | (((uint)_word & 0x000000FF) << 16);
                      //(uint)_byte << 16 | _word;
            }
            private set
            {
                unchecked
                {
                    _byte = (byte)(value & 0x000000FF);
                    _word = (ushort)((value & 0x0000FF00) | ((value & 0x00FF0000) >> 16));

                    //_word = (ushort)(value & 0x0000FFFF);
                    //_byte = (byte)((value & 0x00FF0000) >> 16);
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

        [DebuggerStepThrough]
        public Int24(ushort value)
        {
            _word = 0;
            _byte = 0;

            UValue = value;
        }
        [DebuggerStepThrough]
        public Int24(byte   value)
        {
            _word = 0;
            _byte = 0;

            UValue = value;
        }
        [DebuggerStepThrough]
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
        public bool Equals(Int24 other) => other._word == _word && other._byte == _byte;

        [DebuggerStepThrough]
        public int CompareTo(Int24 other) => Value.CompareTo(other.Value);

        [DebuggerStepThrough]
        public string ToString(string format, IFormatProvider provider) => Value.ToString(format, provider);
        [DebuggerStepThrough]
        public string ToString(string format) => ToString(format, CultureInfo.CurrentCulture);

        string DebugDisplay() => HEXP + UValue.ToString(HEXF);

        #region IConvertible
        [DebuggerStepThrough]
        public TypeCode GetTypeCode() => TypeCode.Int32;

        [DebuggerStepThrough]
        public bool    ToBoolean(IFormatProvider _) => Value != 0;
        [DebuggerStepThrough]
        public char    ToChar   (IFormatProvider _) => (char)Value;
        [DebuggerStepThrough]
        public sbyte   ToSByte  (IFormatProvider _) => unchecked((sbyte )Value);
        [DebuggerStepThrough]
        public byte    ToByte   (IFormatProvider _) => unchecked(( byte )Value);
        [DebuggerStepThrough]
        public short   ToInt16  (IFormatProvider _) => unchecked(( short)Value);
        [DebuggerStepThrough]
        public ushort  ToUInt16 (IFormatProvider _) => unchecked((ushort)Value);
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
        public DateTime ToDateTime(IFormatProvider _)
        {
            throw new InvalidCastException();
        }

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
        public static implicit operator uint  (Int24 value) => value.UValue;
        [DebuggerStepThrough]
        public static implicit operator  int  (Int24 value) => value. Value;
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
