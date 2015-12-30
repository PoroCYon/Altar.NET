using System;
using System.Collections.Generic;
using System.Linq;

namespace Altar
{
    static class Utils
    {
        internal static int PadTo(int v, int p)
        {
            if (p < 1)
                throw new ArgumentOutOfRangeException(nameof(p));
            if (p == 1)
                return v;

            var m = ((p & (p - 1)) == 0) ? (v & (p - 1)) : v % p;

            return m == 0 ? v : v + (p - m);
        }

        internal static uint SwapEnd24Lo(uint i) => ((i & 0x0000FF) << 16) | ((i & 0xFF0000) >> 16) | (i & 0x00FF00);
        internal static uint SwapEnd24Hi(uint i) => SwapEnd24Lo(i >> 8);

        internal static uint SwapEnd32(uint v) => (v & 0xFF000000) >> 24 | (v & 0x00FF0000) >> 8 | (v & 0x0000FF00) << 8 | (v & 0x000000FF) << 24;
    }
}
