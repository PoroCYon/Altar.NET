using System;
using System.Collections.Generic;
using System.Linq;

namespace Altar.NET
{
    static class Utils
    {
        internal static int PadTo(int v, int p)
        {
            var m = v % p;

            if (m == 0)
                return v;

            return v + (p - m);
        }

        internal static uint SwapEnd24(uint i)
        {
            i >>= 8;

            return ((i & 0x0000FF) << 16) | ((i & 0xFF0000) >> 16) | (i & 0x00FF00);
        }

        internal static uint SwapEnd32(uint v) => (v & 0xFF000000) >> 24 | (v & 0x00FF0000) >> 8 | (v & 0x0000FF00) << 8 | (v & 0x000000FF) << 24;
    }
}
