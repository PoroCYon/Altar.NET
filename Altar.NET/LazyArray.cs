using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Altar
{
    public class LazyArray<T> : IList<T>
    {
        public struct Etor : IEnumerator<T>
        {
            bool firstest;
            uint curind;
            LazyArray<T> arr;

            internal Etor(LazyArray<T> arr)
            {
                firstest = true;
                curind = 0;
                this.arr = arr;
            }

            public T Current => arr[curind];
            object IEnumerator.Current => arr[curind];

            public bool MoveNext()
            {
                if (firstest)
                {
                    firstest = false;
                    return arr.max > 0;
                }
                ++curind;
                return curind < arr.Length;
            }
            public void Reset()
            {
                firstest = true;
                curind = 0;
            }

            public void Dispose() { curind = 0; arr = null; }
        }

        Dictionary<uint, KeyValuePair<bool, T>> cache =
            new Dictionary<uint, KeyValuePair<bool, T>>();
        Func<uint, KeyValuePair<bool, T>> getter;
        uint max;

        public LazyArray(Func<uint, KeyValuePair<bool, T>> get, uint max)
        {
            if (get == null && max != 0)
                throw new ArgumentNullException(nameof(get));

            getter = get;
            this.max = max;
        }

        public int Count   => (int)max;
        public int Length  => (int)max;
        public int InCache => cache.Count;
        public bool IsReadOnly => true;

        public T this[uint ind]
        {
            get
            {
                if (ind >= max)
                    throw new IndexOutOfRangeException("ind="+ind+"/"+max);

                KeyValuePair<bool, T> v;
                if (cache.TryGetValue(ind, out v))
                    if (v.Key) return v.Value;
                    else throw new IndexOutOfRangeException();

                var res = cache[ind] = getter(ind);

                if (res.Key) return res.Value;
                throw new IndexOutOfRangeException();
            }
        }
        public T this[int ind]
        {
            get { return this[(uint)ind]; }
            set { throw new NotImplementedException(); }
        }

        public void Add(T _)
        {
            throw new NotImplementedException();
        }
        public void Clear()
        {
            cache.Clear();
        }
        public bool Contains(T t)
        {
            var v = new KeyValuePair<bool, T>(true, t);
            return cache.ContainsValue(v);
        }
        public bool Remove(T v)
        {
            uint kkk;
            foreach (var kvp in cache)
                if (kvp.Value.Key && (ReferenceEquals(kvp.Value, v) ||
                        (!ReferenceEquals(kvp.Value.Value, null)
                         && kvp.Value.Value.Equals(v))))
                {
                    kkk = kvp.Key;
                    goto FoundOne;
                }

            return false;

        FoundOne:
            cache.Remove(kkk);
            return true;
        }
        public void CopyTo(T[] dest, int off)
        {
            for (int i = 0; i < Math.Min(max, dest.Length - off); ++i)
                dest[off+i] = this[i];
        }

        public int IndexOf(T v)
        {
            foreach (var kvp in cache)
                if (kvp.Value.Key && (ReferenceEquals(kvp.Value, v) ||
                        (!ReferenceEquals(kvp.Value.Value, null)
                         && kvp.Value.Value.Equals(v))))
                    return (int)kvp.Key;

            return -1;
        }
        public void Insert(int ind, T v)
        {
            throw new NotImplementedException();
        }
        public void RemoveAt(int ind)
        {
            KeyValuePair<bool, T> kvp;
            if (cache.TryGetValue((uint)ind, out kvp) && kvp.Key)
                cache.Remove((uint)ind);
        }

        public IEnumerator<T> GetEnumerator  () => new Etor(this);
        IEnumerator IEnumerable.GetEnumerator() => new Etor(this);

        public static implicit operator T[](LazyArray<T> la) => la.ToArray();
        public static implicit operator LazyArray<T>(T[] ar) =>
            new LazyArray<T>(i => new KeyValuePair<bool, T>(true, ar[i]), unchecked((uint)ar.Length));
    }
}

