using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Altar
{
    using static SR;

    public enum BranchType : byte
    {
        Unconditional, // br
        IfTrue,        // brt
        IfFalse        // brf
    }
    public unsafe class GraphVertex
    {
        public AnyInstruction*[] Instructions;
        public GraphBranch[] Branches;
    }
    public unsafe class GraphBranch
    {
        public GraphVertex ToVertex;
        public BranchType Type;
        public AnyInstruction* BranchTo;
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

        public override string ToString()
        {
            switch (ReturnType)
            {
                case DataType.Boolean:
                    if (Value is bool)
                        return (bool)Value ? TRUE : FALSE;
                    break;
                case DataType.Double:
                    if (Value is double)
                        return ((double)Value).ToString(CultureInfo.InvariantCulture) + DOUBLE_L;
                    break;
                case DataType.Int16:
                    if (Value is short)
                        return ((short)Value).ToString(CultureInfo.InvariantCulture) + SHORT_L;
                    break;
                case DataType.Int32:
                    if (Value is int)
                        return ((int)Value).ToString(CultureInfo.InvariantCulture);
                    break;
                case DataType.Int64:
                    if (Value is long)
                        return ((long)Value).ToString(CultureInfo.InvariantCulture);
                    break;
                case DataType.Single:
                    if (Value is float)
                        return ((float)Value).ToString(CultureInfo.InvariantCulture) + SINGLE_L;
                    break;
                case DataType.String:
                    if (Value is string)
                        return (string)Value;
                    break;
                default:
                    return O_PAREN + ReturnType.ToPrettyString() + SPACE_S + Value + C_PAREN;
            }

            throw new ArgumentException("Invalid object value " + Value.GetType() + " for data type " + ReturnType.ToPrettyString(), nameof(Value));
        }
    }
    public class VariableExpression : Expression
    {
        public ReferenceDef Variable;
        public VariableType Type;
        public InstanceType Owner;

        public override string ToString() => Owner.ToPrettyString() + DOT + Variable.Name + Type.ToPrettyString() /*+ COLON + ReturnType.ToPrettyString()*/ /* it's always variable */;
    }
    public class UnaryOperatorExpression : Expression
    {
        public Expression Input;
        public UnaryOperator Operator;
        public DataType OriginalType;

        public override string ToString() => O_PAREN + (Operator == UnaryOperator.Convert ? ReturnType.ToPrettyString() : Operator.ToPrettyString()) + SPACE_S + Input + C_PAREN;
    }
    public class BinaryOperatorExpression : Expression
    {
        public Expression Arg1;
        public Expression Arg2;
        public BinaryOperator Operator;
        public DataType OriginalType;

        public override string ToString() => O_PAREN + Operator.ToPrettyString() + SPACE_S + Arg1 + SPACE_S + Arg2 + C_PAREN;
    }
    public class CallExpression : Expression
    {
        public Expression[] Arguments;
        public VariableType Type;
        public ReferenceDef Function;

        public override string ToString() => O_PAREN + Function.Name + Type.ToPrettyString() + COLON + ReturnType.ToPrettyString() + SPACE_S + String.Join(SPACE_S, Arguments.Select(o => o.ToString())) + C_PAREN;
    }

    // ---

    public abstract class Statement { }

    public class SetStatement : Statement
    {
        public Expression Value;
        public VariableType Type;
        public InstanceType Owner;
        public ReferenceDef Target;
        public DataType OriginalType;
        public DataType ReturnType;

        public override string ToString() => O_PAREN + Owner.ToPrettyString() + DOT + Target + Type.ToPrettyString() + EQ_S + Value + C_PAREN;
    }
    public class CallStatement : Statement
    {
        public CallExpression Call;

        public override string ToString() => CALL + SPACE_S + Call.ToString();
    }
    public unsafe class BranchStatement : Statement
    {
        public BranchType Type;
        /// <summary>
        /// Null if <see cref="Type" /> == <see cref="BranchType.Unconditional" />.
        /// </summary>
        public Expression Conditional;
        public AnyInstruction* Target;
        public long TargetOffset;

        public override string ToString()
        {
            var s = String.Empty;

            switch (Type)
            {
                case BranchType.IfFalse:
                    s = IFF + Conditional + SPACE_S;
                    break;
                case BranchType.IfTrue:
                    s = IFT + Conditional + SPACE_S;
                    break;
            }

            s += GOTO;

            s += HEX_PRE + TargetOffset.ToString(HEX_FM8);

            return s;
        }
    }
    public class BreakStatement : Statement
    {
        public DataType Type;
        public uint Signal;

        public override string ToString() => BREAK + Type.ToPrettyString() + SPACE_S + Signal;
    }
    public class ReturnStatement : Statement
    {
        public DataType ReturnType;
        public Expression RetValue;

        public override string ToString() => RET + ReturnType.ToPrettyString() + SPACE_S + RetValue;
    }
    public class PushEnvStatement : Statement
    {
        // ?

        public override string ToString() => PUSHE;
    }
    public class PopEnvStatement : Statement
    {
        public override string ToString() => POPE;
    }

    // temp..?
    public class PushStatement : Statement
    {
        public Expression Expr;

        public override string ToString() => PUSH + Expr;
    }
    public class PopStatement : Statement
    {
        public override string ToString() => POP;
    }
    public class DupStatement : Statement
    {
        public override string ToString() => DUP;
    }

    // ---

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

        public static string ToPrettyString(this UnaryOperator  op)
        {
            switch (op)
            {
                case UnaryOperator.Complement:
                    return TILDE;
                case UnaryOperator.Negation:
                    return DASH;
            }

            return op.ToString().ToLowerInvariant();
        }
        public static string ToPrettyString(this BinaryOperator op)
        {
            switch (op)
            {
                case BinaryOperator.Addition:
                    return PLUS;
                case BinaryOperator.And:
                    return AMP;
                case BinaryOperator.Division:
                    return SLASH;
                case BinaryOperator.Equality:
                    return EQUAL;
                case BinaryOperator.GreaterThan:
                    return GT;
                case BinaryOperator.GTOrEqual:
                    return GTE;
                case BinaryOperator.Inequality:
                    return NEQUAL;
                case BinaryOperator.LeftShift:
                    return LEFTSH;
                case BinaryOperator.LowerThan:
                    return LT;
                case BinaryOperator.LTOrEqual:
                    return LTE;
                case BinaryOperator.Modulo:
                    return MOD;
                case BinaryOperator.Multiplication:
                    return ASTERISK;
                case BinaryOperator.Or:
                    return VBAR;
                case BinaryOperator.RightShift:
                    return RIGHTSH;
                case BinaryOperator.Subtraction:
                    return DASH;
                case BinaryOperator.Xor:
                    return XOR;
            }

            return op.ToString().ToLowerInvariant();
        }
    }
}
