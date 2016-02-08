using System;
using System.Collections.Generic;
using System.Linq;

namespace Altar.Repack
{
    public class BBData
    {
        public BinBuffer Buffer;
        public int[] OffsetOffsets;

        public BBData(BinBuffer bb, int[] offs)
        {
            Buffer        = bb;
            OffsetOffsets = offs;
        }
    }

    public static class SectionWriter
    {
        static void UpdateOffsets(BBData data, int parentOffset)
        {
            var offs = data.OffsetOffsets;
            var bb = data.Buffer;

            for (int i = 0; i < offs.Length; i++)
            {
                bb.Position = offs[i];
                var o = bb.ReadInt32();
                bb.Position -= sizeof(int);
                bb.Write(o + parentOffset);
                offs[i] += parentOffset;
            }

            data.Buffer        = bb;
            data.OffsetOffsets = offs;
        }

        public static void Write(BinBuffer self, BBData data)
        {
            UpdateOffsets(data, self.Position);

            self.Write(data.Buffer);
        }
        public static int WriteOffset(BinBuffer self, int offset)
        {
            var r = self.Position;

            self.Write(offset);

            return r;
        }

        public static void WriteList(BBData data, BBData[] datas)
        {
            var bb = data.Buffer;

            bb.Write(datas.Length);

            var allOffs = data.OffsetOffsets.ToList();

            var offAcc = bb.Position + datas.Length * sizeof(int); // after all offsets
            for (int i = 0; i < datas.Length; i++)
            {
                allOffs.Add(bb.Position);
                bb.Write(offAcc);

                offAcc += datas[i].Buffer.Size;
            }

            for (int i = 0; i < datas.Length; i++)
            {
                Write(bb, datas[i]);
                allOffs.AddRange(datas[i].OffsetOffsets); // updated by Write
            }

            data.OffsetOffsets = allOffs.ToArray();
        }
        public static void WriteChunk(BBData data, SectionHeaders chunk, BBData inner)
        {
            var bb = data.Buffer;

            bb.Write((uint)chunk);

            Write(bb, inner);
        }

        public static void WriteIffWad(BBData data, IDictionary<SectionHeaders, BBData> chunks)
        {
            foreach (var kvp in chunks) WriteChunk(data, kvp.Key, kvp.Value);
        }
    }
}
