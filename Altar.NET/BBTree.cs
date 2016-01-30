using System;
using System.Collections.Generic;
using System.Linq;

namespace Altar
{
    public abstract class BBNode
    {
        public abstract void AddToPrimitives(List<BBPrimitive> prev);
    }

    // just for type restrictions in BBTree methods
    public abstract class BBSimple : BBNode { }
    public abstract class BBPrimitive : BBSimple { }

    public sealed class BBValue : BBPrimitive
    {
        public BinBuffer BinBuffer;

        public override void AddToPrimitives(List<BBPrimitive> prev)
        {
            if (prev.Count == 0 || !(prev[prev.Count - 1] is BBValue))
                prev.Add(this);
            else
                ((BBValue)prev[prev.Count - 1]).BinBuffer.Write(BinBuffer);
        }
    }
    public sealed class BBOffset : BBPrimitive
    {
        /// <summary>
        /// Relative to parent.
        /// </summary>
        public int Offset;

        public override void AddToPrimitives(List<BBPrimitive> prev)
        {
            prev.Add(new BBOffset { Offset = Offset + prev.Sum(BBTree.BBNSize) });
        }
    }
    //TODO: lazy offset?

    public sealed class BBData : BBSimple
    {
        public List<BBSimple> Inner;

        public override void AddToPrimitives(List<BBPrimitive> prev)
        {
            foreach (var s in Inner)
                s.AddToPrimitives(prev);
        }
    }

    public sealed class BBChunk : BBNode
    {
        public SectionHeaders Header;

        public BBNode Rest;

        public override void AddToPrimitives(List<BBPrimitive> prev)
        {
            if (!(Rest is BBSimple))
                throw new NotSupportedException();

            BBTree.LastOrAdd(prev).Write((uint)Header);

            Rest.AddToPrimitives(prev);
        }
    }
    public sealed class BBList : BBNode
    {
        public List<BBNode> Elements;

        public override void AddToPrimitives(List<BBPrimitive> prev)
        {
            var count = Elements.Count;

            BBTree.LastOrAdd(prev).Write((uint)count);

            var offAccum = 0;
            var offsets = new int[count];
            for (int i = 0; i < count; i++)
            {
                if (!(Elements[i] is BBSimple))
                    throw new NotSupportedException();

                offsets[i] = offAccum;

                offAccum += BBTree.BBNSize(Elements[i]);
            }

            prev.AddRange(offsets.Select(o => new BBOffset { Offset = o }));

            foreach (var e in Elements)
                e.AddToPrimitives(prev);
        }
    }

    // -----------------------

    public static class BBTree
    {
        static BBNode[] EmptyNodeArr = { };

        internal static BinBuffer LastOrAdd(List<BBPrimitive> prev, BinBuffer def = null)
        {
            if (prev.Count == 0 || !(prev[prev.Count - 1] is BBValue))
                prev.Add(new BBValue { BinBuffer = def ?? new BinBuffer() });

            return ((BBValue)prev[prev.Count - 1]).BinBuffer;
        }

        internal static int BBNSize(BBNode bbn)
        {
            if (bbn is BBValue)
                return ((BBValue)bbn).BinBuffer.Size;
            if (bbn is BBOffset)
                return sizeof(int);
            if (bbn is BBData)
                return ((BBData)bbn).Inner.Sum(BBNSize);
            if (bbn is BBChunk)
                return sizeof(SectionHeaders) + sizeof(uint) + BBNSize(((BBChunk)bbn).Rest);
            if (bbn is BBList)
                return sizeof(uint) + sizeof(uint) * ((BBList)bbn).Elements.Count + ((BBList)bbn).Elements.Sum(BBNSize);

            return 0;
        }

        static IEnumerable<Accessor<BBNode>> SubnodesOf(BBNode node)
        {
            if (node is BBSimple)
                return null;
            if (node is BBChunk)
            {
                var n = (BBChunk)node;

                return new[] { new Accessor<BBNode>(() => n.Rest, v => n.Rest = v) };
            }
            if (node is BBList)
            {
                var l = (BBList)node;
                var ll = l.Elements;

                var accs = new Accessor<BBNode>[ll.Count];
                for (int i = 0; i < ll.Count; i++)
                {
                    var ii = i;

                    accs[i] = new Accessor<BBNode>(() => ll[ii], v => ll[ii] = v);
                }

                return accs;
            }

            return null;
        }
        static IEnumerable<Tuple<BBNode, Accessor<BBNode>>> LeavesOrCurrent(BBNode parent, Accessor<BBNode> node)
        {
            var nodes = SubnodesOf(node.Get());

            if (nodes == null)
                return new[] { Tuple.Create(parent, node) };

            return nodes.SelectMany(n => LeavesOrCurrent(node.Get(), n));
        }
        static BBNode[] FlattenLeaves(BBNode[] nodes)
        {
            var leaves = new List<Tuple<BBNode, Accessor<BBNode>>>();

            for (int i = 0; i < nodes.Length; i++)
            {
                var ii = i;
                var acc = new Accessor<BBNode>(() => nodes[ii], v => nodes[ii] = v);
                leaves.AddRange(LeavesOrCurrent(null, acc));
            }

            foreach (var t in leaves)
            {
                var p = t.Item1;
                var c = t.Item2;

                if (p is BBSimple)
                    throw new NotSupportedException();

                //TODO: merge (get parent data -> AddToPrimitives)
            }

            throw new NotImplementedException(); //TODO: something
        }

        public static BBPrimitive[] Flatten(IEnumerable<BBNode> nodes)
        {
            var e = nodes.ToArray();

            while (!e.All(n => n is BBPrimitive))
                e = FlattenLeaves(e);

            return e.Select(p => (BBPrimitive)p).ToArray();
        }
        public static BinBuffer FlushOffsets(BBPrimitive[] primitives)
        {
            var bb = new BinBuffer(primitives.Sum(BBNSize));

            foreach (var p in primitives)
            {
                if (p is BBValue)
                    bb.Write(((BBValue )p).BinBuffer);
                if (p is BBOffset)
                    bb.Write(((BBOffset)p).Offset);
            }

            bb.Position = 0;
            return bb;
        }
    }
}
