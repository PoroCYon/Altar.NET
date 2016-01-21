using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public static BranchType Type  (this GotoInstruction instr, uint bcv)
        {
            if (instr.OpCode.Kind(bcv) != InstructionKind.Goto)
                return BranchType.Unconditional;

            if (bcv > 0xE)
                switch (instr.OpCode.VersionF)
                {
                    case FOpCode.Brt:
                        return BranchType.IfTrue;
                    case FOpCode.Brf:
                        return BranchType.IfFalse;
                }
            else
                switch (instr.OpCode.VersionE)
                {
                    case EOpCode.Brt:
                        return BranchType.IfTrue;
                    case EOpCode.Brf:
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

    public enum UnaryOperator : byte
    {
        Negation   = 1,
        Complement    ,
        Return        ,
        Exit          ,
        Convert       ,
        Duplicate
    }
    public enum BinaryOperator : byte
    {
        Addition       = 1,
        Subtraction       ,
        Multiplication    ,
        Division          ,
        Remainder         ,
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
    public enum ExpressionType : byte
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
                        return ((short )Value).ToString(CultureInfo.InvariantCulture) + SHORT_L;
                    break;
                case DataType.Int32:
                    if (Value is int)
                        return ((int   )Value).ToString(CultureInfo.InvariantCulture);
                    break;
                case DataType.Int64:
                    if (Value is long)
                        return ((long  )Value).ToString(CultureInfo.InvariantCulture) + LONG_L;
                    break;
                case DataType.Single:
                    if (Value is float)
                        return ((float )Value).ToString(CultureInfo.InvariantCulture) + SINGLE_L;
                    break;
                case DataType.String:
                    if (Value is string)
                        return ((string)Value).Escape();
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
        public InstanceType OwnerType;
        public string OwnerName;
        /// <summary>
        /// Null if <see cref="Type" /> != <see cref="VariableType.Array" />.
        /// </summary>
        public Expression[] ArrayIndices;

        public override string ToString()
        {
            var a = (OwnerName == null ? OwnerType.ToPrettyString() : O_BRACKET + OwnerName + C_BRACKET)
                + DOT + Variable.Name;

            if (ArrayIndices != null && Type == VariableType.Array)
                return a + O_BRACKET + String.Join(COMMA_S, ArrayIndices.Select(e => e.ToString())) + C_BRACKET;

            return a + Type.ToPrettyString();
        }
    }
    public class MemberExpression : VariableExpression
    {
        public Expression Owner;

        public override string ToString()
        {
            var a = Owner + COLON + Variable.Name;

            if (ArrayIndices != null && Type == VariableType.Array)
                return a + O_BRACKET + ArrayIndices + C_BRACKET;

            return a;
        }
    }
    public class UnaryOperatorExpression : Expression
    {
        public Expression Input;
        public UnaryOperator Operator;
        public DataType OriginalType;

        public override string ToString()
        {
            if (Operator == UnaryOperator.Duplicate)
                return O_PAREN + DUP + SPACE_S + ReturnType.ToPrettyString() + C_PAREN;

            return O_PAREN + (Operator == UnaryOperator.Convert ? ReturnType.ToPrettyString() : Operator.ToPrettyString()) + SPACE_S + Input + C_PAREN;
        }
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
    public class PopExpression : Expression
    {
        public PopExpression()
        {
            ReturnType = (DataType)0xFF;
        }

        public override string ToString() => POP;
    }
    public class AssertExpression : Expression
    {
        public short ControlValue;
        public Expression Expr;

        public override string ToString() => O_PAREN + ASSERT + ReturnType.ToPrettyString() + SPACE_S + ControlValue + SPACE_S + Expr + C_PAREN;
    }

    // ---

    public enum EnvStackOperator : byte
    {
        PushEnv,
        PopEnv
    }

    public abstract class Statement { }

    public class SetStatement : Statement
    {
        public Expression Value;
        public VariableType Type;
        public InstanceType OwnerType;
        public string OwnerName;
        public ReferenceDef Target;
        public DataType OriginalType;
        public DataType ReturnType;
        /// <summary>
        /// Null if <see cref="Type" /> != <see cref="VariableType.Array" />.
        /// </summary>
        public Expression[] ArrayIndices;

        public override string ToString() =>
            (OwnerName == null ? OwnerType.ToPrettyString() : O_BRACKET + OwnerName + C_BRACKET)
                + DOT + Target.Name +
                (Type == VariableType.Array && ArrayIndices != null
                    ? O_BRACKET + String.Join(COMMA_S, ArrayIndices.Select(e => e.ToString())) + C_BRACKET
                    : Type.ToPrettyString())
                + EQ_S + Value;
    }
    public class CallStatement : Statement
    {
        public CallExpression Call;

        public override string ToString() => CALL + Call.ToString();
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

            s += HEX_PRE + TargetOffset.ToString(HEX_FM6);

            return s;
        }
    }
    public class ReturnStatement : Statement
    {
        public DataType ReturnType;
        public Expression RetValue;

        public override string ToString() => RET_S + ReturnType.ToPrettyString() + SPACE_S + RetValue;
    }
    public class ExitStatement : Statement
    {
        public override string ToString() => EXIT;
    }
    public unsafe class PushEnvStatement : Statement
    {
        public AnyInstruction* Target;
        public long TargetOffset;
        public Expression Parent;

        public override string ToString() => PUSHE + SPACE_S + Parent + SPACE_S + HEX_PRE + TargetOffset.ToString(HEX_FM6);
    }
    public unsafe class PopEnvStatement : Statement
    {
        public AnyInstruction* Target;
        public long TargetOffset;

        public override string ToString() => POPE + SPACE_S + HEX_PRE + TargetOffset.ToString(HEX_FM6);
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
        [DebuggerHidden]
        public static UnaryOperator  UnaryOp (this AnyInstruction instr, uint bcv)
        {
            if (bcv > 0xE)
                switch (instr.OpCode.VersionF)
                {
                    case FOpCode.Neg:
                        return UnaryOperator.Negation;
                    case FOpCode.Not:
                        return UnaryOperator.Complement;
                    case FOpCode.Ret:
                        return UnaryOperator.Return;
                    case FOpCode.Exit:
                        return UnaryOperator.Exit;
                    case FOpCode.Conv:
                        return UnaryOperator.Convert;
                    case FOpCode.Dup:
                        return UnaryOperator.Duplicate;
                }
            else
                switch (instr.OpCode.VersionE)
                {
                    case EOpCode.Neg:
                        return UnaryOperator.Negation;
                    case EOpCode.Not:
                        return UnaryOperator.Complement;
                    case EOpCode.Ret:
                        return UnaryOperator.Return;
                    case EOpCode.Exit:
                        return UnaryOperator.Exit;
                    case EOpCode.Conv:
                        return UnaryOperator.Convert;
                    case EOpCode.Dup:
                        return UnaryOperator.Duplicate;
                }

            return 0;
        }
        [DebuggerHidden]
        public static BinaryOperator BinaryOp(this AnyInstruction instr, uint bcv)
        {
            if (bcv > 0xE)
                switch (instr.OpCode.VersionF)
                {
                    case FOpCode.Add:
                        return BinaryOperator.Addition;
                    case FOpCode.Sub:
                        return BinaryOperator.Subtraction;
                    case FOpCode.Mul:
                        return BinaryOperator.Multiplication;
                    case FOpCode.Div:
                        return BinaryOperator.Division;
                    case FOpCode.Rem:
                        return BinaryOperator.Remainder;
                    case FOpCode.Mod:
                        return BinaryOperator.Modulo;
                    case FOpCode.And:
                        return BinaryOperator.And;
                    case FOpCode.Or:
                        return BinaryOperator.Or;
                    case FOpCode.Xor:
                        return BinaryOperator.Xor;
                    case FOpCode.Shl:
                        return BinaryOperator.LeftShift;
                    case FOpCode.Shr:
                        return BinaryOperator.RightShift;
                    case FOpCode.Cmp:
                        switch (instr.DoubleType.ComparisonType)
                        {
                            case ComparisonType.Equality:
                                return BinaryOperator.Equality;
                            case ComparisonType.Inequality:
                                return BinaryOperator.Inequality;
                            case ComparisonType.GreaterThan:
                                return BinaryOperator.GreaterThan;
                            case ComparisonType.LowerThan:
                                return BinaryOperator.LowerThan;
                            case ComparisonType.GTOrEqual:
                                return BinaryOperator.GTOrEqual;
                            case ComparisonType.LTOrEqual:
                                return BinaryOperator.LTOrEqual;
                        }

                        return 0; // ?
                }
            else
                switch (instr.OpCode.VersionE)
                {
                    case EOpCode.Add:
                        return BinaryOperator.Addition;
                    case EOpCode.Sub:
                        return BinaryOperator.Subtraction;
                    case EOpCode.Mul:
                        return BinaryOperator.Multiplication;
                    case EOpCode.Div:
                        return BinaryOperator.Division;
                    case EOpCode.Rem:
                        return BinaryOperator.Remainder;
                    case EOpCode.Mod:
                        return BinaryOperator.Modulo;
                    case EOpCode.And:
                        return BinaryOperator.And;
                    case EOpCode.Or:
                        return BinaryOperator.Or;
                    case EOpCode.Xor:
                        return BinaryOperator.Xor;
                    case EOpCode.Shl:
                        return BinaryOperator.LeftShift;
                    case EOpCode.Shr:
                        return BinaryOperator.RightShift;
                    case EOpCode.Ceq:
                        return BinaryOperator.Equality;
                    case EOpCode.Cne:
                        return BinaryOperator.Inequality;
                    case EOpCode.Cgt:
                        return BinaryOperator.GreaterThan;
                    case EOpCode.Clt:
                        return BinaryOperator.LowerThan;
                    case EOpCode.Cge:
                        return BinaryOperator.GTOrEqual;
                    case EOpCode.Cle:
                        return BinaryOperator.LTOrEqual;
                }

            return 0;
        }

        [DebuggerHidden]
        public static ExpressionType ExprType(this AnyInstruction instr, uint bcv)
        {
            if (bcv > 0xE)
                switch (instr.OpCode.VersionF)
                {
                    case FOpCode.PushCst:
                    case FOpCode.PushGlb:
                    case FOpCode.PushVar:
                    case FOpCode.PushI16:
                        return instr.Push.Type == DataType.Variable ? ExpressionType.Variable : ExpressionType.Literal;
                    case FOpCode.Set:
                        return ExpressionType.Set;
                    case FOpCode.Call:
                        return ExpressionType.Call;
                }
            else
                switch (instr.OpCode.VersionE)
                {
                    case EOpCode.Push:
                        return instr.Push.Type == DataType.Variable ? ExpressionType.Variable : ExpressionType.Literal;
                    case EOpCode.Set:
                        return ExpressionType.Set;
                    case EOpCode.Call:
                        return ExpressionType.Call;
                }

            if (instr.UnaryOp (bcv) != 0)
                return ExpressionType.UnaryOp ;
            if (instr.BinaryOp(bcv) != 0)
                return ExpressionType.BinaryOp;

            return 0;
        }

        [DebuggerHidden]
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
        [DebuggerHidden]
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
                case BinaryOperator.Remainder:
                    return REMAIN;
                case BinaryOperator.RightShift:
                    return RIGHTSH;
                case BinaryOperator.Subtraction:
                    return DASH;
                case BinaryOperator.Xor:
                    return XOR;
            }

            return op.ToString().ToLowerInvariant();
        }
        [DebuggerHidden]
        public static string ToPrettyString(this EnvStackOperator op) => op.ToString().ToLowerInvariant();

        [DebuggerHidden]
        public static string Escape(this string s) =>
            "\"" +
                s.Replace("\\", "\\\\")
                 .Replace("\n", "\\n" )
                 .Replace("\r", "\\r" )
                 .Replace("\t", "\\t" )
                 .Replace("\b", "\\b" )
                 .Replace("\0", "\\0" )
                 .Replace("\"", "\\\"") + "\"";

        public static IEnumerable<T> WalkExprTree<T>(this Expression e, Func<Expression, IEnumerable<T>> fn)
        {
            if (fn == null)
                throw new ArgumentNullException(nameof(fn));

            var r = fn(e);

            if (e is UnaryOperatorExpression)
                r = r.Concat(WalkExprTree(((UnaryOperatorExpression)e).Input, fn));
            else if (e is BinaryOperatorExpression)
                r = r
                    .Concat(WalkExprTree(((BinaryOperatorExpression)e).Arg1, fn))
                    .Concat(WalkExprTree(((BinaryOperatorExpression)e).Arg2, fn));
            else if (e is CallExpression)
                r = r.Concat(((CallExpression)e).Arguments.SelectMany(fn));

            return r;
        }
        public static IEnumerable<T> WalkExprTree<T>(this Expression e, Func<Expression, T> fn)
        {
            if (fn == null)
                throw new ArgumentNullException(nameof(fn));

            return e.WalkExprTree<T>(_ => new T[] { fn(_) });
        }
        public static void WalkExprTree(this Expression e, Action<Expression> act)
        {
            if (act == null)
                throw new ArgumentNullException(nameof(act));

            var EmptyArr = new byte[0];

            foreach (var __ in // evaluate the enumerable, it is lazy by default
                e.WalkExprTree(_ =>
                {
                    act(_);
                    return EmptyArr;
                })) { }
        }
    }
}
