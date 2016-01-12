using System;
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
    }
    public static class Extensions
    {
        public static IEnumerable<T> PopMany <T>(this Stack<T> stack, int amount)
        {
            if (amount > stack.Count || amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            for (int i = 0; i < amount; i++)
                yield return stack.Pop();

            yield break;
        }
        public static IEnumerable<T> PopWhile<T>(this Stack<T> stack, Predicate<T> p)
        {
            if (p == null)
                throw new ArgumentNullException(nameof(p));

            while (stack.Count > 0 && p(stack.Peek()))
                yield return stack.Pop();

            yield break;
        }

        public static void PushRange<T>(this Stack<T> stack, IEnumerable<T> toPush)
        {
            if (toPush == null)
                throw new ArgumentNullException(nameof(toPush));

            foreach (var e in toPush)
                stack.Push(e);
        }

        public static IEnumerable<T> DequeueMany <T>(this Queue<T> queue, int amount)
        {
            if (amount > queue.Count || amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            for (int i = 0; i < amount; i++)
                yield return queue.Dequeue();

            yield break;
        }
        public static IEnumerable<T> DequeueWhile<T>(this Queue<T> queue, Predicate<T> p)
        {
            if (p == null)
                throw new ArgumentNullException(nameof(p));

            while (queue.Count > 0 && p(queue.Peek()))
                yield return queue.Dequeue();

            yield break;
        }

        public static void EnqueueRange<T>(this Queue<T> queue, IEnumerable<T> toEnq)
        {
            if (toEnq == null)
                throw new ArgumentNullException(nameof(toEnq));

            foreach (var e in toEnq)
                queue.Enqueue(e);
        }
    }
}
