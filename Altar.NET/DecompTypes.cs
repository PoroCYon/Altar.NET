using System;
using System.Collections.Generic;
using System.Linq;

namespace Altar
{
    public enum BranchType : byte
    {
        Unconditional, // br
        IfTrue,        // brt
        IfFalse        // brf
    }
    public unsafe class GraphVertex
    {
        public uint FirstInstrAddress;
        public AnyInstruction*[] Instructions;
        public GraphBranch[] Branches;
    }
    public class GraphBranch
    {
        public GraphVertex ToVertex;
        public BranchType Type;
        public uint BranchToAddress;
    }

    public static class BranchTypeExt
    {
        public static BranchType Type  (this GotoInstruction instr)
        {
            if (instr.OpCode.Kind() != InstructionKind.Goto)
                return BranchType.Unconditional;

            switch (instr.OpCode)
            {
                case OpCode.Brt:
                    return BranchType.IfTrue;
                case OpCode.Brf:
                    return BranchType.IfFalse;
            }

            return BranchType.Unconditional;
        }
        public static BranchType Invert(this BranchType type)
        {
            switch (type)
            {
                case BranchType.IfFalse:
                    return BranchType.IfTrue;
                case BranchType.IfTrue:
                    return BranchType.IfFalse;
            }

            return BranchType.Unconditional;
        }
    }

    struct CodeBlock
    {
        public List<IntPtr> Instructions;
        public BranchType Type;
        public uint BranchToAddress;
    }
}
