using System;
using System.Collections.Generic;
using System.Linq;

namespace Altar
{
    public unsafe static class Decompiler
    {
        static GraphBranch[] EmptyGBArray = { };
        static GraphVertex[] EmptyGVArray = { };

        static List<CodeBlock> SplitBlocks(GMFileContent content, CodeInfo code)
        {
            var blocks = new List<CodeBlock>();
            var curBl  = new List<IntPtr>();
            var instr  = code.Instructions;
            var firstI = (long)instr[0];

            // find all branch-to instructions
            var targets = new List<long>();
            for (int i = 0; i < instr.Length; i++)
                if (instr[i]->Kind() == InstructionKind.Goto)
                    targets.Add((long)instr[i] - firstI + instr[i]->Goto.Offset);

            // split code into blocks separated by br[tf]? instructions
            for (int i = 0; i < instr.Length; i++)
            {
                curBl.Add((IntPtr)instr[i]);

                if (instr[i]->Kind() == InstructionKind.Goto)
                {
                    blocks.Add(new CodeBlock
                    {
                        Instructions = curBl,
                        Type = instr[i]->Goto.Type(),
                        BranchToAddress = (uint)((long)instr[i] - firstI) + instr[i]->Goto.Offset
                    });

                    curBl = new List<IntPtr>();
                }
                else if (targets.Contains((long)instr[i] - firstI))
                {
                    blocks.Add(new CodeBlock
                    {
                        Instructions = curBl,
                        Type = BranchType.Unconditional,
                        BranchToAddress = i == instr.Length - 1 ? 0xFFFFFFFF : (uint)((long)instr[i + 1] - firstI)
                    });
                }
            }

            if (curBl.Count != 0)
                blocks.Add(new CodeBlock
                {
                    Type = BranchType.Unconditional,
                    BranchToAddress = 0xFFFFFFFF,
                    Instructions = curBl
                });

            return blocks;
        }
        static GraphVertex[] CreateVertices(GMFileContent content, CodeInfo code)
        {
            var blocks = SplitBlocks(content, code);
            var instr = code.Instructions;
            var firstI = (long)instr[0];

            // only one block -> just return it as a single vertex
            if (blocks.Count == 1)
            {
                var bis = blocks[0].Instructions;
                var r = new GraphVertex
                {
                    Branches = EmptyGBArray,
                    FirstInstrAddress = 0,
                    Instructions = new AnyInstruction*[bis.Count]
                };

                for (int i = 0; i < bis.Count; i++)
                    r.Instructions[i] = (AnyInstruction*)bis[i];

                return new[] { r };
            }

            // find all branch-to instructions
            var targets = new List<long>();
            for (int i = 0; i < instr.Length; i++)
                if (instr[i]->Kind() == InstructionKind.Goto)
                    targets.Add((long)instr[i] - firstI + instr[i]->Goto.Offset);

            var vertices = new GraphVertex[blocks.Count];

            // create list of vertices
            for (int i = 0; i < blocks.Count; i++)
            {
                var blk = blocks[i];
                var ins = blk.Instructions;
                var hasNext = i < blocks.Count - 1 && blk.Type != BranchType.Unconditional /* no need to check if uncond */;

                vertices[i] = new GraphVertex
                {
                    FirstInstrAddress = (uint)((long)ins[0] - firstI),
                    Instructions = new AnyInstruction*[ins.Count],
                    Branches = new GraphBranch[hasNext ? 2 : 1]
                };

                vertices[i].Branches[0] = new GraphBranch
                {
                    BranchToAddress = blk.BranchToAddress,
                    Type = blk.Type
                };

                if (hasNext)
                    vertices[i].Branches[1] = new GraphBranch
                    {
                        BranchToAddress = (uint)((long)blocks[i + 1].Instructions[0] - firstI),
                        Type = blk.Type.Invert()
                    };

                for (int j = 0; j < ins.Count; j++)
                    vertices[i].Instructions[j] = (AnyInstruction*)ins[j];
            }

            // connect vertex branches to target vertices
            for (int i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i];

                for (int j = 0; j < v.Branches.Length; j++)
                    v.Branches[j].ToVertex =
                        vertices.FirstOrDefault(ve => ve.FirstInstrAddress == v.Branches[j].BranchToAddress)
                            ?? (i == vertices.Length - 1 ? null : vertices[i + 1]);
            }

            return vertices;
        }

        /// <summary>
        /// Returns the entry vertex of the code.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public static GraphVertex[] BuildGraph(GMFileContent content, CodeInfo code)
        {
            if (code.Instructions.Length == 0)
                return EmptyGVArray;

            return CreateVertices(content, code);
        }
    }
}
