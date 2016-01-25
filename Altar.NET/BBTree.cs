using System;
using System.Collections.Generic;
using System.Linq;

namespace Altar
{
    public abstract class BBNode
    {
        public bool IsMerged;

        public void Merge(List<BBPrimitive> parent)
        {
            MergeWith(parent);

            IsMerged = true;
        }
        protected abstract void MergeWith(List<BBPrimitive> parent);
    }

    public abstract class BBSimple : BBNode { }
    public abstract class BBPrimitive : BBSimple { }

    public sealed class BBValue : BBPrimitive
    {
        public BinBuffer BinBuffer;

        protected override void MergeWith(List<BBPrimitive> parent)
        {
            if (BinBuffer == null)
                return;

            if (parent.Count == 0 || !(parent[parent.Count - 1] is BBValue))
                parent.Add(new BBValue { BinBuffer = BinBuffer });
            else
                ((BBValue)parent[parent.Count - 1]).BinBuffer.Write(BinBuffer);
        }
    }
    public sealed class BBOffset : BBPrimitive
    {
        /// <summary>
        /// Relative to parent.
        /// </summary>
        public int Offset;

        protected override void MergeWith(List<BBPrimitive> parent)
        {
            parent.Add(new BBOffset { Offset = Offset + parent.Sum(BBTree.BBSSize) });
        }
    }
    public sealed class BBData : BBSimple
    {
        public List<BBPrimitive> Inner;

        protected override void MergeWith(List<BBPrimitive> parent)
        {
            foreach (var bbs in Inner)
                bbs.Merge(parent);
        }
    }

    public sealed class BBChunk : BBNode
    {
        public SectionHeaders Header;

        public BBNode Rest;

        protected override void MergeWith(List<BBPrimitive> parent)
        {
            if (parent.Count == 0)
                parent.Add(new BBValue { BinBuffer = new BinBuffer() });
            else if (parent[parent.Count - 1] is BBOffset)
                parent.Add(new BBValue { BinBuffer = new BinBuffer() });

            var lbb = ((BBValue)parent[parent.Count - 1]).BinBuffer;

            lbb.Write((uint)Header);

            var li = new List<BBPrimitive>();

            Rest.Merge(li);

            var size = li.Sum(BBTree.BBSSize);

            lbb.Write(size);

            new BBData { Inner = li }.Merge(parent);
        }
    }
    public sealed class BBList : BBNode
    {
        public List<BBNode> Elements;

        protected override void MergeWith(List<BBPrimitive> parent)
        {
            if (parent.Count == 0)
                parent.Add(new BBValue { BinBuffer = new BinBuffer() });
            else if (parent[parent.Count - 1] is BBOffset)
                parent.Add(new BBValue { BinBuffer = new BinBuffer() });

            ((BBValue)parent[parent.Count - 1]).BinBuffer.Write(Elements.Count);

            var l = new List<BBPrimitive> { new BBValue { BinBuffer = new BinBuffer() } };

            for (int i = 0; i < Elements.Count; i++)
            {
                Elements[i].Merge(l);

                parent.Add(new BBOffset { Offset = l.Sum(BBTree.BBSSize) });
            }

            var bbd = new BBData { Inner = l };
            bbd.Merge(parent);
        }
    }

    // -----------------------

    public static class BBTree
    {
        internal static int BBSSize(BBSimple bbs)
        {
            if (bbs is BBValue)
                return ((BBValue)bbs).BinBuffer.Size;
            if (bbs is BBOffset)
                return sizeof(int);
            if (bbs is BBData)
                return ((BBData)bbs).Inner.Sum(BBSSize);

            return 0;
        }

        public static BBPrimitive[] Flatten(IEnumerable<BBNode> nodes)
        {
            throw new NotImplementedException(); // TODO
        }
        public static BinBuffer FlushOffsets(BBPrimitive[] primitives)
        {
            throw new NotImplementedException(); // TODO
        }
    }
}
