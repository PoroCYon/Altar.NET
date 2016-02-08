using System;
using System.Collections.Generic;
using System.Linq;
using Altar.Decomp;

namespace Altar.Recomp
{
    public struct OpCodePair
    {
        public EOpCode VersionE;
        public FOpCode VersionF;
    }

    public abstract class Instruction
    {
        public OpCodePair OpCode;
    }

    public class SingleType : Instruction
    {
        public DataType Type;
    }
    public class DoubleType : Instruction
    {
        public DataType Type1;
        public DataType Type2;
    }
    public class Compare : DoubleType
    {
        public ComparisonType ComparisonType;
    }
    public class Branch : Instruction
    {
        public object Label;
    }
    public class Set : DoubleType
    {
        public InstanceType InstanceType;
        public string InstanceName;

        public string TargetVariable;

        public VariableType VariableType;
    }
    public class Call : Instruction
    {
        public DataType ReturnType;

        public long Arguments;
        public string FunctionName;
        public VariableType FunctionType;
    }
    public class Break : SingleType
    {
        public long Signal;
    }

    public abstract class Push : SingleType { }

    public class PushConst : Push
    {
        public object Value;
    }
    public class PushVariable : Push
    {
        public InstanceType InstanceType;
        public string InstanceName;

        public string VariableName;

        public VariableType VariableType;
    }
}
