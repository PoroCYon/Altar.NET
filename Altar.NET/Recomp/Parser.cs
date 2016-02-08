using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Altar.Decomp;

namespace Altar.Recomp
{
    public static class Parser
    {
        internal static string Pos(Token t) => $"at line {t.Line} and column {t.Column}";

        static IComparable LabelValue(Token t)
        {
            if (t is IntToken)
                return ((IntToken)t).Value;
            if (t is WordToken)
                return ((WordToken)t).Value;

            throw new InvalidOperationException($"Token '{t}' {Pos(t)} cannot be used as a label!");
        }
        static IComparable InnerValue(Token t)
        {
            if (t is IntToken)
                return ((IntToken)t).Value;
            if (t is WordToken)
                return ((WordToken)t).Value;
            if (t is FloatToken)
                return ((FloatToken)t).Value;
            if (t is StringToken)
                return ((StringToken)t).Value;

            throw new InvalidOperationException($"Token '{t}' {Pos(t)} has no inner value!");
        }

        static OpCodePair     TokenToOpCodes(Token t, ComparisonType comp = 0, DataType data = 0, InstanceType inst = (InstanceType)1)
        {
            if (t is NormalToken && ((NormalToken)t).Kind == TokenKind.OpCode)
                switch (((NormalToken)t).Type)
                {
                    case TokenType.Conv:
                        return new OpCodePair { VersionE = EOpCode.Conv, VersionF = FOpCode.Conv };
                    case TokenType.Mul:
                        return new OpCodePair { VersionE = EOpCode.Mul, VersionF = FOpCode.Mul };
                    case TokenType.Div:
                        return new OpCodePair { VersionE = EOpCode.Div, VersionF = FOpCode.Div };
                    case TokenType.Rem:
                        return new OpCodePair { VersionE = EOpCode.Rem, VersionF = FOpCode.Rem };
                    case TokenType.Mod:
                        return new OpCodePair { VersionE = EOpCode.Mod, VersionF = FOpCode.Mod };
                    case TokenType.Add:
                        return new OpCodePair { VersionE = EOpCode.Add, VersionF = FOpCode.Add };
                    case TokenType.Sub:
                        return new OpCodePair { VersionE = EOpCode.Sub, VersionF = FOpCode.Sub };
                    case TokenType.And:
                        return new OpCodePair { VersionE = EOpCode.And, VersionF = FOpCode.And };
                    case TokenType.Or:
                        return new OpCodePair { VersionE = EOpCode.Or, VersionF = FOpCode.Or };
                    case TokenType.Xor:
                        return new OpCodePair { VersionE = EOpCode.Xor, VersionF = FOpCode.Xor };
                    case TokenType.Neg:
                        return new OpCodePair { VersionE = EOpCode.Neg, VersionF = FOpCode.Neg };
                    case TokenType.Not:
                        return new OpCodePair { VersionE = EOpCode.Not, VersionF = FOpCode.Not };
                    case TokenType.Shl:
                        return new OpCodePair { VersionE = EOpCode.Shl, VersionF = FOpCode.Shl };
                    case TokenType.Shr:
                        return new OpCodePair { VersionE = EOpCode.Shr, VersionF = FOpCode.Shr };
                    case TokenType.Clt:
                        return new OpCodePair { VersionE = EOpCode.Clt, VersionF = FOpCode.Cmp };
                    case TokenType.Cle:
                        return new OpCodePair { VersionE = EOpCode.Cle, VersionF = FOpCode.Cmp };
                    case TokenType.Ceq:
                        return new OpCodePair { VersionE = EOpCode.Ceq, VersionF = FOpCode.Cmp };
                    case TokenType.Cne:
                        return new OpCodePair { VersionE = EOpCode.Cne, VersionF = FOpCode.Cmp };
                    case TokenType.Cge:
                        return new OpCodePair { VersionE = EOpCode.Cge, VersionF = FOpCode.Cmp };
                    case TokenType.Cgt:
                        return new OpCodePair { VersionE = EOpCode.Cgt, VersionF = FOpCode.Cmp };
                    case TokenType.Set:
                        return new OpCodePair { VersionE = EOpCode.Set, VersionF = FOpCode.Set };
                    case TokenType.Dup:
                        return new OpCodePair { VersionE = EOpCode.Dup, VersionF = FOpCode.Dup };
                    case TokenType.Ret:
                        return new OpCodePair { VersionE = EOpCode.Ret, VersionF = FOpCode.Ret };
                    case TokenType.Exit:
                        return new OpCodePair { VersionE = EOpCode.Exit, VersionF = FOpCode.Exit };
                    case TokenType.Pop:
                        return new OpCodePair { VersionE = EOpCode.Pop, VersionF = FOpCode.Pop };
                    case TokenType.Br:
                        return new OpCodePair { VersionE = EOpCode.Br, VersionF = FOpCode.Br };
                    case TokenType.Brt:
                        return new OpCodePair { VersionE = EOpCode.Brt, VersionF = FOpCode.Brt };
                    case TokenType.Brf:
                        return new OpCodePair { VersionE = EOpCode.Brf, VersionF = FOpCode.Brf };
                    case TokenType.PushEnv:
                        return new OpCodePair { VersionE = EOpCode.PushEnv, VersionF = FOpCode.PushEnv };
                    case TokenType.PopEnv:
                        return new OpCodePair { VersionE = EOpCode.PopEnv, VersionF = FOpCode.PopEnv };
                    case TokenType.Push:
                        FOpCode pt = FOpCode.PushCst;
                        switch (data)
                        {
                            case DataType.Variable:
                                if (inst <= InstanceType.Other)
                                    pt = FOpCode.PushVar;
                                break;
                            case DataType.Int16:
                                pt = FOpCode.PushI16;
                                break;
                        }
                        if (inst == InstanceType.Global)
                            pt = FOpCode.PushGlb;

                        return new OpCodePair { VersionE = EOpCode.Push, VersionF = pt };
                    case TokenType.Call:
                        return new OpCodePair { VersionE = EOpCode.Call, VersionF = FOpCode.Call };
                    case TokenType.Break:
                        return new OpCodePair { VersionE = EOpCode.Break, VersionF = FOpCode.Break };
                    case TokenType.Cmp:
                        EOpCode ct;
                        switch (comp)
                        {
                            case ComparisonType.LowerThan:
                                ct = EOpCode.Clt;
                                break;
                            case ComparisonType.LTOrEqual:
                                ct = EOpCode.Cle;
                                break;
                            case ComparisonType.Equality:
                                ct = EOpCode.Ceq;
                                break;
                            case ComparisonType.Inequality:
                                ct = EOpCode.Cne;
                                break;
                            case ComparisonType.GTOrEqual:
                                ct = EOpCode.Cge;
                                break;
                            case ComparisonType.GreaterThan:
                                ct = EOpCode.Cgt;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(comp));
                        }

                        return new OpCodePair { VersionE = ct, VersionF = FOpCode.Cmp };
                    case TokenType.PushCst:
                        return new OpCodePair { VersionE = EOpCode.Push, VersionF = FOpCode.PushCst };
                    case TokenType.PushGlb:
                        return new OpCodePair { VersionE = EOpCode.Push, VersionF = FOpCode.PushGlb };
                    case TokenType.PushVar:
                        return new OpCodePair { VersionE = EOpCode.Push, VersionF = FOpCode.PushVar };
                    case TokenType.PushI16:
                        return new OpCodePair { VersionE = EOpCode.Push, VersionF = FOpCode.PushI16 };
                }

            throw new ArgumentException($"Expected opcode token, got '{t}' {Pos(t)}.");
        }
        static DataType       TokenToData   (Token t)
        {
            if (!(t is NormalToken) || ((NormalToken)t).Kind != TokenKind.DataType)
                throw new ArgumentException($"Expected data type token, got '{t}' {Pos(t)}.");

            switch (((NormalToken)t).Type)
            {
                case TokenType.Double:
                    return DataType.Double;
                case TokenType.Single:
                    return DataType.Single;
                case TokenType.Int16:
                    return DataType.Int16;
                case TokenType.Int32:
                    return DataType.Int32;
                case TokenType.Int64:
                    return DataType.Int64;
                case TokenType.Bool:
                    return DataType.Boolean;
                case TokenType.Var:
                    return DataType.Variable;
                case TokenType.String:
                    return DataType.String;
#pragma warning disable 618
                case TokenType.Inst:
                    return DataType.Instance;
#pragma warning restore 618
            }

            throw new ArgumentException($"Expected data type token, got '{t}' {Pos(t)}.");
        }
        static InstanceType   TokenToInst   (Token t)
        {
            if (t is NormalToken && ((NormalToken)t).Kind == TokenKind.InstanceType)
                switch (((NormalToken)t).Type)
                {
                    case TokenType.Stog:
                        return InstanceType.StackTopOrGlobal;
                    case TokenType.Self:
                        return InstanceType.Self;
                    case TokenType.Other:
                        return InstanceType.Other;
                    case TokenType.All:
                        return InstanceType.All;
                    case TokenType.Noone:
                        return InstanceType.Noone;
                    case TokenType.Global:
                        return InstanceType.Global;
                }
            else if (t is IntToken)
                return (InstanceType)((IntToken)t).Value;

            throw new ArgumentException($"Expected instance type token, got '{t}' {Pos(t)}.");
        }
        static VariableType   TokenToVar    (Token t)
        {
            if (!(t is NormalToken) || ((NormalToken)t).Kind != TokenKind.VariableType)
                throw new ArgumentOutOfRangeException($"Expected variable type token, got '{t}' {Pos(t)}.");

            switch (((NormalToken)t).Type)
            {
                case TokenType.Array:
                    return VariableType.Array;
                case TokenType.StackTop:
                    return VariableType.StackTop;
            }

            throw new ArgumentException($"Expected variable type token, got '{t}' {Pos(t)}.");
        }
        static ComparisonType TokenToComp   (Token t)
        {
            if (!(t is NormalToken) || ((NormalToken)t).Kind != TokenKind.ComparisonType)
                throw new ArgumentOutOfRangeException($"Expected comparison token, got '{t}' {Pos(t)}.");

            switch (((NormalToken)t).Type)
            {
                case TokenType.Clt:
                case TokenType.LT:
                    return ComparisonType.LowerThan;
                case TokenType.Cle:
                case TokenType.LE:
                    return ComparisonType.LTOrEqual;
                case TokenType.Ceq:
                case TokenType.EQ:
                    return ComparisonType.Equality;
                case TokenType.Cne:
                case TokenType.NE:
                    return ComparisonType.Inequality;
                case TokenType.Cge:
                case TokenType.GE:
                    return ComparisonType.GTOrEqual;
                case TokenType.Cgt:
                case TokenType.GT:
                    return ComparisonType.GreaterThan;
            }

            throw new ArgumentException($"Expected comparison token, got '{t}' {Pos(t)}.");
        }

        public static IEnumerable<Instruction> Parse(IEnumerable<Token> tokens)
        {
            var q = new Queue<Token>(tokens);

            int instrs = 0;
            var labels = new Dictionary<IComparable, int>();

            #region Func<Token> Dequeue = () => { [...] };
            Func<Token> Dequeue = () =>
            {
                if (q.Count == 0)
                    throw new FormatException("Token expected, but EOF was reached.");

                return q.Dequeue();
            };
            #endregion
            Action<TokenType> Expect = tt =>
            {
                if (q.Count == 0)
                    throw new FormatException($"Expected token '{tt}', but EOF was reached.");

                var p = Dequeue();

                if (!(p is NormalToken) || ((NormalToken)p).Type != tt)
                    throw new FormatException($"Invalid token '{p}' {Pos(p)}, token '{tt}' expected.");
            };
            #region Action SkipWhitespace = () => { [...] };
            Action SkipWhitespace = () =>
            {
                while (q.Count > 0 && q.Peek() is NormalToken && ((NormalToken)q.Peek()).Type == TokenType.Whitespace)
                    q.Dequeue();
            };
            #endregion
            #region Action SkipWhitespaceAndLines = () => { [...] };
            Action SkipWhitespaceAndLines = () =>
            {
                while (q.Count > 0 && q.Peek() is NormalToken && (((NormalToken)q.Peek()).Type == TokenType.Whitespace || ((NormalToken)q.Peek()).Type == TokenType.Newline))
                    q.Dequeue();
            };
            #endregion
            Func<TokenKind, NormalToken> ExpectReadKind = k  =>
            {
                var tt = Dequeue();

                if (!(tt is NormalToken) || ((NormalToken)tt).Kind != k)
                    throw new FormatException($"Expected token of kind {k}, but got '{tt}' {Pos(tt)}.");

                return (NormalToken)tt;
            };
            Func<TokenType, NormalToken> ExpectReadType = ty =>
            {
                var tt = Dequeue();

                if (!(tt is NormalToken) || ((NormalToken)tt).Type != ty)
                    throw new FormatException($"Expected {ty} token, but got {tt} at line {tt.Line} and column {tt.Column}.");

                return (NormalToken)tt;
            };

            #region Func<VariableType> TryReadVariableType = () => { [...] };
            Func<VariableType> TryReadVariableType = () =>
            {
                var vt = VariableType.Normal;
                if (q.Count > 0)
                {
                    SkipWhitespace();

                    var p = q.Peek();

                    if (p is NormalToken && ((NormalToken)p).Kind == TokenKind.VariableType)
                    {
                        q.Dequeue();

                        switch (((NormalToken)p).Type)
                        {
                            case TokenType.StackTop:
                                vt = VariableType.StackTop;
                                break;
                            case TokenType.Array:
                                vt = VariableType.Array;
                                break;
                            default:
                                throw new InvalidOperationException("Unexpected error, this shouldn't happen.");
                        }
                    }
                }

                return vt;
            };
            #endregion
            #region Func<Tuple<string, InstanceType>> ReadInstanceType = () => { [...] };
            Func<Tuple<string, InstanceType>> ReadInstanceType = () =>
            {
                var o = Dequeue();

                if ((!(o is NormalToken) && !(o is WordToken))
                        || (o is NormalToken && ((NormalToken)o).Kind != TokenKind.InstanceType)
                        || (o is WordToken && !((WordToken)o).Value.StartsWith(SR.O_BRACKET, StringComparison.Ordinal)
                                           && !((WordToken)o).Value.EndsWith  (SR.C_BRACKET, StringComparison.Ordinal)))
                    throw new FormatException($"Variable owner must be an instance type or object name, but is '{o}', {Pos(o)}.");

                var inst = (InstanceType)1;
                string insn = null;

                if (o is NormalToken)
                    switch (((NormalToken)o).Type)
                    {
                        case TokenType.Stog:
                            inst = InstanceType.StackTopOrGlobal;
                            break;
                        case TokenType.Self:
                            inst = InstanceType.Self;
                            break;
                        case TokenType.Other:
                            inst = InstanceType.Other;
                            break;
                        case TokenType.All:
                            inst = InstanceType.All;
                            break;
                        case TokenType.Noone:
                            inst = InstanceType.Noone;
                            break;
                        case TokenType.Global:
                            inst = InstanceType.Global;
                            break;
                        default:
                            throw new InvalidOperationException("Unexpected error, this shouldn't happen.");
                    }
                else
                {
                    insn = ((WordToken)o).Value;
                    insn = insn.Substring(1, insn.Length - 2);
                }

                return Tuple.Create(insn, inst);
            };
            #endregion
            #region Func<string> ReadIdentifier = () => { [...] };
            Func<string> ReadIdentifier = () =>
            {
                var n = Dequeue();

                if (!(n is WordToken) && !(n is NormalToken) /* can be an instruction or type name... */)
                    throw new FormatException($"Identifier expected, but found '{n}', {Pos(n)}.");

                return n is WordToken ? ((WordToken)n).Value : n.OrigString;
            };
            #endregion

            Token t;
            while (q.Count > 0)
            {
                SkipWhitespace();

                t = Dequeue();

                if (!(t is NormalToken))
                {
                    var lv = LabelValue(t);

                    labels.Add(lv, instrs);
                    Expect(TokenType.Colon);
                    SkipWhitespaceAndLines();

                    yield return new Label { LabelValue = lv };

                    t = Dequeue();
                }

                if (!(t is NormalToken) || ((NormalToken)t).Kind != TokenKind.OpCode)
                    throw new FormatException($"OpCode expected, but got '{t}' {Pos(t)}.");

                var nt = (NormalToken)t;
                var opc = nt.Type;

                SkipWhitespace();

                switch (opc)
                {
                    #region doubletype
                    case TokenType.Conv:
                    case TokenType.Mul:
                    case TokenType.Div:
                    case TokenType.Rem:
                    case TokenType.Mod:
                    case TokenType.Add:
                    case TokenType.Sub:
                    case TokenType.And:
                    case TokenType.Or:
                    case TokenType.Xor:
                    case TokenType.Neg:
                    case TokenType.Not:
                    case TokenType.Shl:
                    case TokenType.Shr:
                        {
                            var t1 = TokenToData(ExpectReadKind(TokenKind.DataType));
                            SkipWhitespace();
                            Expect(TokenType.Colon);
                            SkipWhitespace();
                            var t2 = TokenToData(ExpectReadKind(TokenKind.DataType));

                            yield return new DoubleType { OpCode = TokenToOpCodes(nt), Type1 = t1, Type2 = t2 };
                        }
                        break;
                    #endregion
                    #region cmp
                    case TokenType.Clt:
                    case TokenType.Cle:
                    case TokenType.Ceq:
                    case TokenType.Cne:
                    case TokenType.Cge:
                    case TokenType.Cgt:
                        {
                            var t1 = TokenToData(ExpectReadKind(TokenKind.DataType));
                            SkipWhitespace();
                            Expect(TokenType.Colon);
                            SkipWhitespace();
                            var t2 = TokenToData(ExpectReadKind(TokenKind.DataType));

                            yield return new Compare { OpCode = TokenToOpCodes(nt), Type1 = t1, Type2 = t2, ComparisonType = TokenToComp(nt) };
                        }
                        break;
                    case TokenType.Cmp:
                        {
                            var ct = TokenToComp(ExpectReadKind(TokenKind.ComparisonType));
                            SkipWhitespace();
                            var t1 = TokenToData(ExpectReadKind(TokenKind.DataType      ));
                            SkipWhitespace();
                            Expect(TokenType.Colon);
                            SkipWhitespace();
                            var t2 = TokenToData(ExpectReadKind(TokenKind.DataType));

                            yield return new Compare { OpCode = TokenToOpCodes(nt, ct), Type1 = t1, Type2 = t2, ComparisonType = ct };
                        }
                        break;
                    #endregion
                    #region singletype
                    case TokenType.Dup:
                    case TokenType.Ret:
                    case TokenType.Exit:
                    case TokenType.Pop:
                        yield return new SingleType { OpCode = TokenToOpCodes(nt), Type = TokenToData(ExpectReadKind(TokenKind.DataType)) };
                        break;
                    #endregion
                    #region branch
                    case TokenType.Br:
                    case TokenType.Brt:
                    case TokenType.Brf:
                    case TokenType.PushEnv:
                    case TokenType.PopEnv:
                        yield return new Branch { OpCode = TokenToOpCodes(nt), Label = LabelValue(Dequeue()) };
                        break;
                    #endregion
                    #region set
                    case TokenType.Set:
                        {
                            var t1 = TokenToData(ExpectReadKind(TokenKind.DataType));
                            SkipWhitespace();
                            Expect(TokenType.Colon);
                            SkipWhitespace();
                            var t2 = TokenToData(ExpectReadKind(TokenKind.DataType));
                            SkipWhitespace();

                            var instu = ReadInstanceType();

                            SkipWhitespace();
                            Expect(TokenType.Colon);
                            SkipWhitespace();

                            var n = ReadIdentifier();
                            var t3 = TryReadVariableType();

                            yield return new Set { OpCode  = TokenToOpCodes(nt), Type1 = t1, Type2 = t2, InstanceType = instu.Item2, InstanceName = instu.Item1, TargetVariable = n, VariableType = t3 };
                        }
                        break;
                    #endregion
                    #region push
                    case TokenType.Push:
                    case TokenType.PushCst:
                    case TokenType.PushGlb:
                    case TokenType.PushI16:
                    case TokenType.PushVar:
                        {
                            var t1 = TokenToData(ExpectReadKind(TokenKind.DataType));
                            SkipWhitespace();

                            switch (t1)
                            {
                                case DataType.Variable:
                                    var instu = ReadInstanceType();

                                    SkipWhitespace();
                                    Expect(TokenType.Colon);
                                    SkipWhitespace();

                                    var n = ReadIdentifier();
                                    var t2 = TryReadVariableType();

                                    yield return new PushVariable { OpCode = TokenToOpCodes(nt, 0, t1, instu.Item2), Type = t1, InstanceType = instu.Item2, InstanceName = instu.Item1, VariableName = n, VariableType = t2 };
                                    break;
                                default:
                                    var v = InnerValue(Dequeue());

                                    yield return new PushConst { OpCode = TokenToOpCodes(nt), Type = t1, Value = v };
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region call
                    case TokenType.Call:
                        {
                            var t1 = TokenToData(ExpectReadKind(TokenKind.DataType));
                            SkipWhitespace();
                            Expect(TokenType.Colon);
                            SkipWhitespace();
                            var c = Dequeue();

                            if (!(c is IntToken))
                                throw new FormatException($"Call argument count must be an integer, but is '{c}', {Pos(c)}.");

                            SkipWhitespace();

                            var n = ReadIdentifier();
                            var t2 = TryReadVariableType();

                            yield return new Call { OpCode = TokenToOpCodes(nt), ReturnType = t1, Arguments = ((IntToken)c).Value, FunctionName = n, FunctionType = t2 };
                        }
                        break;
                    #endregion
                    #region break
                    case TokenType.Break:
                        {
                            var t1 = TokenToData(ExpectReadKind(TokenKind.DataType));
                            SkipWhitespace();
                            var s = Dequeue();

                            if (!(s is IntToken))
                                throw new FormatException($"Break signal must be an integer, but is '{s}', {Pos(s)}.");

                            yield return new Break { OpCode = TokenToOpCodes(nt), Type = t1, Signal = ((IntToken)s).Value };
                        }
                        break;
                    #endregion
                    default:
                        throw new InvalidOperationException("Unexpected error, this shouldn't happen.");
                }

                instrs++;

                if (q.Count > 0)
                {
                    SkipWhitespace();
                    ExpectReadType(TokenType.Newline);
                    SkipWhitespaceAndLines();
                }
            }

            yield break;
        }
    }
}
