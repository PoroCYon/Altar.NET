using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Altar
{
    public unsafe static class Utils
    {
        public static int PadTo(int v, int p)
        {
            if (p < 1)
                throw new ArgumentOutOfRangeException(nameof(p));
            if (p == 1)
                return v;

            var m = ((p & (p - 1)) == 0) ? (v & (p - 1)) : v % p;

            return m == 0 ? v : v + (p - m);
        }

        public static uint SwapEnd24Lo(uint i) => ((i & 0x0000FF) << 16) | ((i & 0xFF0000) >> 16) | (i & 0x00FF00);
        public static uint SwapEnd24Hi(uint i) => SwapEnd24Lo(i >> 8);

        public static uint SwapEnd32(uint v) => (v & 0xFF000000) >> 24 | (v & 0x00FF0000) >> 8 | (v & 0x0000FF00) << 8 | (v & 0x000000FF) << 24;

        public static int ComparePtrs(IntPtr a, IntPtr b) => a.ToInt64().CompareTo(b.ToInt64());
        public static int IndexOfPtr(AnyInstruction*[] arr, AnyInstruction* elem)
        {
            for (int i = 0; i < arr.Length; i++)
                if (elem == arr[i])
                    return i;

            return -1;
        }
        public static int IndexOfPtr(AnyInstruction*[] arr, IntPtr ptr) => IndexOfPtr(arr, (AnyInstruction*)ptr);
        public static AnyInstruction*[] MPtrListToPtrArr(IList<IntPtr> l)
        {
            var r = new AnyInstruction*[l.Count];

            for (int i = 0; i < r.Length; i++)
                r[i] = (AnyInstruction*)l[i];

            return r;
        }

        public static IEnumerable<uint> UintRange(uint start, uint length)
        {
            var end = start + length;

            for (uint i = start; i < end; i++)
                yield return i;

            yield break;
        }

        public static T Identity<T>(T t) => t;

        public static string ToHexSignString(long v, string fmt)
        {
            if (v >= 0L)
                return v.ToString(fmt);

            v = Math.Abs(v);

            return "-" + v.ToString(fmt);
        }
    }
    public static class Extensions
    {
        public static IEnumerable<T> PopWhile<T>(this Stack<T> stack, Predicate<T> p)
        {
            if (p == null)
                throw new ArgumentNullException(nameof(p));

            while (stack.Count > 0 && p(stack.Peek()))
                yield return stack.Pop();

            yield break;
        }
        public static IEnumerable<T> PopMany <T>(this Stack<T> stack, int amount)
        {
            if (amount > stack.Count || amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            return stack.PopWhile(_ => --amount >= 0);
        }
        public static IEnumerable<T> PopAll  <T>(this Stack<T> stack) => stack.PopWhile(_ => true);

        public static void PushRange<T>(this Stack<T> stack, IEnumerable<T> toPush)
        {
            if (toPush == null)
                throw new ArgumentNullException(nameof(toPush));

            foreach (var e in toPush)
                stack.Push(e);
        }

        public static IEnumerable<T> DequeueWhile<T>(this Queue<T> queue, Predicate<T> p)
        {
            if (p == null)
                throw new ArgumentNullException(nameof(p));

            while (queue.Count > 0 && p(queue.Peek()))
                yield return queue.Dequeue();

            yield break;
        }
        public static IEnumerable<T> DequeueMany <T>(this Queue<T> queue, int amount)
        {
            if (amount > queue.Count || amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            return queue.DequeueWhile(_ => --amount >= 0);
        }
        public static IEnumerable<T> DequeueAll  <T>(this Queue<T> queue) => queue.DequeueWhile(_ => true);

        public static void EnqueueRange<T>(this Queue<T> queue, IEnumerable<T> toEnq)
        {
            if (toEnq == null)
                throw new ArgumentNullException(nameof(toEnq));

            foreach (var e in toEnq)
                queue.Enqueue(e);
        }

        public static IEnumerable<T> ToGeneric<T>(this IEnumerable coll, Func<object, IEnumerable<T>> cast = null)
        {
            if (cast == null)
                cast = o => typeof(T) == typeof(string)
                    ? new[] { (T)(object)o.ToString() }
                    : (o is T ? new[] { (T)o } : new T[0]);

            foreach (var o in coll)
                foreach (var t in cast(o))
                    yield return t;

            yield break;
        }
        public static IEnumerable<T> ToGeneric<T>(this IEnumerable coll, Func<object, T> cast) =>
            coll.ToGeneric(cast == null ? null : (Func<object, IEnumerable<T>>)(o => new[] { cast(o) }));

        static Type RemoveGParams(Type t) => t.IsGenericType && !t.IsGenericTypeDefinition ? t.GetGenericTypeDefinition() : t;
        public static bool Is(this Type child, Type parent, bool stripGeneric = false)
        {
            var RGP = stripGeneric ? (Func<Type, Type>)RemoveGParams : Utils.Identity;

            child  = RGP(child );
            parent = RGP(parent);

            if (parent == typeof(object) || parent == child)
                return true;
            if (child.IsValueType && parent == typeof(ValueType))
                return true;
            if (child.IsEnum && (parent == typeof(Enum) || child.GetEnumUnderlyingType() == parent))
                return true;
            if (child.IsArray && parent == typeof(Array))
                return true;
            if (child.IsPointer)
                return parent == typeof(void*);
            if (child.IsInterface && parent.GetInterfaces().Any(t => RGP(t) == RGP(child)))
                return true;

            var bt = RGP(parent.BaseType);

            do
            {
                if (bt == child)
                    return true;

                bt = RGP(bt.BaseType);
            } while (bt != typeof(object));

            return false;
        }
    }
}
