using System;
using System.Collections.Generic;
using System.Linq;

namespace Altar
{
    public sealed class Ref<T>
        where T : class
    {
        public T Value = null;

        public Ref()
        {

        }
        public Ref(T val)
        {
            Value = val;
        }

        public override bool Equals(object obj) => Object.ReferenceEquals(obj, this) || Object.ReferenceEquals(obj, Value) || Value.Equals(obj);
#pragma warning disable RECS0025
        public override int GetHashCode() => Value == null ? 0 : Value.GetHashCode();
#pragma warning restore RECS0025
        public override string ToString() => Value == null ? SR.NULL : Value.ToString() + SR.ASTERISK;

        public static implicit operator Ref<T>(T val ) => new Ref<T>(val);
        public static explicit operator T(Ref<T> @ref) => @ref?.Value;
    }

    public struct Accessor<T>
    {
        public Func  <T> Get;
        public Action<T> Set;

        public Accessor(Func<T> get, Action<T> set)
        {
            Get = get;
            Set = set;
        }
        public Accessor(Func<T> get)
            : this(get, null)
        {

        }
        public Accessor(Action<T> set)
            : this(null, set)
        {

        }

        public static explicit operator T(Accessor<T> acc)
        {
            if (acc.Get == null)
                throw new NotSupportedException();

            return acc.Get();
        }
    }
}
