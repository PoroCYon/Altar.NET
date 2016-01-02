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

    // ---

    public enum UnaryOperator
    {
        Negation   = 1,
        Complement    ,
        Return        ,
        Exit          ,
        Convert
    }
    public enum BinaryOperator
    {
        Addition       = 1,
        Subtraction       ,
        Multiplication    ,
        Division          ,
        Modulo            ,
        And               ,
        Or                ,
        Xor               ,
        LeftShift         ,
        RightShift        ,
        Equality          ,
        Inequality        ,
        GreaterThan       ,
        LowerThan         ,
        GTOrEqual         ,
        LTOrEqual
    }
    public enum ExpressionType
    {
        Literal  = 1,
        Variable    ,
        UnaryOp     ,
        BinaryOp    ,
        Call        ,
        Set
    }

    public abstract class Expression
    {
        public DataType ReturnType
        {
            get;
            set;
        }
    }

    public class LiteralExpression : Expression
    {
        public object Value;
        public DataType OriginalType;

        public override string ToString() => (Value ?? SR.NULL).ToString();
    }
    public class VariableExpression : Expression
    {
        public ReferenceDef Variable;
        public VariableType Type;
        public InstanceType Owner;
        public DataType OriginalType;

        public override string ToString() => Owner.ToPrettyString() + SR.DOT + Variable.Name + Type.ToPrettyString();
    }
    public class UnaryOperatorExpression : Expression
    {
        public Expression Input;
        public UnaryOperator Operator;
        public DataType OriginalType;

        public override string ToString() => SR.O_PAREN + (Operator == UnaryOperator.Convert ? ReturnType.ToString() : Operator.ToString()) + SR.SPACE_S + Input + SR.C_PAREN;
    }
    public class BinaryOperatorExpression : Expression
    {
        public Expression Arg1;
        public Expression Arg2;
        public BinaryOperator Operator;
        public DataType OriginalType;

        public override string ToString() => SR.O_PAREN + Operator + SR.SPACE_S + Arg1 + SR.SPACE_S + Arg2 + SR.C_PAREN;
    }
    public class CallExpression : Expression
    {
        public Expression[] Arguments;
        public VariableType Type;
        public ReferenceDef Function;

        public override string ToString() => SR.O_PAREN + Function.Name + Type.ToPrettyString() + SR.SPACE_S + String.Join(SR.SPACE_S, Arguments.Select(o => o.ToString())) + SR.C_PAREN;
    }
    public class SetExpression : Expression
    {
        public Expression Value;
        public VariableType Type;
        public InstanceType Owner;
        public ReferenceDef Target;
        public DataType OriginalType;

        public override string ToString() => SR.O_PAREN + Owner.ToPrettyString() + SR.DOT + Target + Type.ToPrettyString() + " = " + Value + SR.C_PAREN;
    }

    public static class DecompExt
    {
        public static UnaryOperator  UnaryOp (this OpCode code)
        {
            switch (code)
            {
                case OpCode.Neg:
                    return UnaryOperator.Negation;
                case OpCode.Not:
                    return UnaryOperator.Complement;
                case OpCode.Ret:
                    return UnaryOperator.Return;
                case OpCode.Exit:
                    return UnaryOperator.Exit;
                case OpCode.Conv:
                    return UnaryOperator.Convert;
            }

            return 0;
        }
        public static BinaryOperator BinaryOp(this OpCode code)
        {
            switch (code)
            {
                case OpCode.Add:
                    return BinaryOperator.Addition;
                case OpCode.Sub:
                    return BinaryOperator.Subtraction;
                case OpCode.Mul:
                    return BinaryOperator.Multiplication;
                case OpCode.Div:
                    return BinaryOperator.Division;
                case OpCode.Mod:
                    return BinaryOperator.Modulo;
                case OpCode.And:
                    return BinaryOperator.And;
                case OpCode.Or:
                    return BinaryOperator.Or;
                case OpCode.Xor:
                    return BinaryOperator.Xor;
                case OpCode.Shl:
                    return BinaryOperator.LeftShift;
                case OpCode.Shr:
                    return BinaryOperator.RightShift;
                case OpCode.Ceq:
                    return BinaryOperator.Equality;
                case OpCode.Cne:
                    return BinaryOperator.Inequality;
                case OpCode.Cgt:
                    return BinaryOperator.GreaterThan;
                case OpCode.Clt:
                    return BinaryOperator.LowerThan;
                case OpCode.Cge:
                    return BinaryOperator.GTOrEqual;
                case OpCode.Cle:
                    return BinaryOperator.LTOrEqual;
            }

            return 0;
        }

        public static UnaryOperator  UnaryOp (this AnyInstruction instr) => instr.Code().UnaryOp ();
        public static BinaryOperator BinaryOp(this AnyInstruction instr) => instr.Code().BinaryOp();

        public static ExpressionType ExprType(this AnyInstruction instr)
        {
            var c = instr.Code();
            switch (c)
            {
                case OpCode.Push:
                    return instr.Push.Type == DataType.Variable ? ExpressionType.Variable : ExpressionType.Literal;
                case OpCode.Set:
                    return ExpressionType.Set;
                case OpCode.Call:
                    return ExpressionType.Call;
            }

            if (c.UnaryOp () != 0)
                return ExpressionType.UnaryOp ;
            if (c.BinaryOp() != 0)
                return ExpressionType.BinaryOp;

            return 0;
        }
    }
}
