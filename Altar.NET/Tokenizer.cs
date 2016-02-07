using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Altar
{
    public enum TokenType
    {
        Whitespace,
        Colon     ,
        Newline   ,

        Conv   ,
        Mul    ,
        Div    ,
        Rem    ,
        Mod    ,
        Add    ,
        Sub    ,
        And    ,
        Or     ,
        Xor    ,
        Neg    ,
        Not    ,
        Shl    ,
        Shr    ,
        Clt    ,
        Cle    ,
        Ceq    ,
        Cge    ,
        Cgt    ,
        Set    ,
        Dup    ,
        Ret    ,
        Exit   ,
        Pop    ,
        Br     ,
        Brt    ,
        Brf    ,
        PushEnv,
        PopEnv ,
        Push   ,
        Call   ,
        Break  ,
        Cmp    ,
        PushCst,
        PushGlb,
        PushVar,
        PushI16,

        Double,
        Single,
        Int16 ,
        Int32 ,
        Int64 ,
        Bool  ,
        Var   ,
        String,
        Inst  ,

        Stog  ,
        Self  ,
        Other ,
        All   ,
        Noone ,
        Global,

        Array   ,
        StackTop,

        LT,
        LE,
        EQ,
        NE,
        GE,
        GT
    }

    public abstract class Token { }
    public class NormalToken : Token
    {
        public TokenType Type;
    }
    public class IntToken : Token
    {
        public long Value;
    }
    public class FloatToken : Token
    {
        public double Value;
    }
    public class StringToken : Token
    {
        public string Value;
    }
    public class WordToken : Token
    {
        public string Value;
    }

    public static class Tokenizer
    {
        readonly static char[] WordSep = " :\r\n\t".ToCharArray();

        static string Unescape(string s) =>
            s.Replace("\\\\", "\\").Replace("\\\"", "\"")
            .Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\t", "\t").Replace("\\b", "\b");

        public static IEnumerable<Token> Tokenize(string code)
        {
            int pos = 0;
            var _rwB = new StringBuilder();

            Func<char, bool> IsWordSep = c => (Array.IndexOf(WordSep, c) > 0 || Char.IsWhiteSpace(c));
            Func<int   > PeekChar = () => pos == code.Length ? -1 : code[pos  ];
            Func<int   > ReadChar = () => pos == code.Length ? -1 : code[pos++];
            #region Func<string> ReadWord = () => { [...] };
            // basically the lexer fn
            Func<string> ReadWord = () =>
            {
                _rwB.Clear();

                bool inString = false;
                bool inEsc    = false;

                while (true)
                {
                    var p = PeekChar();
                    if (p == -1)
                        break;

                    if (p == '"' && !inEsc) inString = !inString;

                    var r = ReadChar();
                    _rwB.Append(r == '\r' ? '\n' : r); // normalise to \n

                    if (p == '\r' && PeekChar() == '\n') ReadChar(); // merge CRLF

                    inEsc = p == '\\' ? inEsc = !inEsc : false;

                    var op = p;
                    p = PeekChar();

                    // merge whitespace continuations
                    while (op == p /* op != -1 (see earlier break) -> will break if p == -1 */ && IsWordSep((char)p))
                    {
                        // next char
                        op = p;
                        p = ReadChar();
                    }

                    if (p == -1 || (IsWordSep((char)p) && !inString))
                        break;
                }

                return _rwB.ToString();
            };
            #endregion

            while (pos < code.Length)
            {
                string w = ReadWord();
                if (!String.IsNullOrWhiteSpace(w))
                    w = w.Trim();

                switch (w)
                {
                    case "conv":
                        yield return new NormalToken { Type = TokenType.Conv };
                        break;
                    case "mul":
                        yield return new NormalToken { Type = TokenType.Mul };
                        break;
                    case "div":
                        yield return new NormalToken { Type = TokenType.Div };
                        break;
                    case "rem":
                        yield return new NormalToken { Type = TokenType.Rem };
                        break;
                    case "mod":
                        yield return new NormalToken { Type = TokenType.Mod };
                        break;
                    case "add":
                        yield return new NormalToken { Type = TokenType.Add };
                        break;
                    case "sub":
                        yield return new NormalToken { Type = TokenType.Sub };
                        break;
                    case "and":
                        yield return new NormalToken { Type = TokenType.And };
                        break;
                    case "or":
                        yield return new NormalToken { Type = TokenType.Or };
                        break;
                    case "xor":
                        yield return new NormalToken { Type = TokenType.Xor };
                        break;
                    case "neg":
                        yield return new NormalToken { Type = TokenType.Neg };
                        break;
                    case "not":
                        yield return new NormalToken { Type = TokenType.Not };
                        break;
                    case "shl":
                        yield return new NormalToken { Type = TokenType.Shl };
                        break;
                    case "shr":
                        yield return new NormalToken { Type = TokenType.Shr };
                        break;
                    case "clt":
                        yield return new NormalToken { Type = TokenType.Clt };
                        break;
                    case "cle":
                        yield return new NormalToken { Type = TokenType.Cle };
                        break;
                    case "ceq":
                        yield return new NormalToken { Type = TokenType.Ceq };
                        break;
                    case "cge":
                        yield return new NormalToken { Type = TokenType.Cge };
                        break;
                    case "cgt":
                        yield return new NormalToken { Type = TokenType.Cgt };
                        break;
                    case "set":
                        yield return new NormalToken { Type = TokenType.Set };
                        break;
                    case "dup":
                        yield return new NormalToken { Type = TokenType.Dup };
                        break;
                    case "ret":
                        yield return new NormalToken { Type = TokenType.Ret };
                        break;
                    case "exit":
                        yield return new NormalToken { Type = TokenType.Exit };
                        break;
                    case "pop":
                        yield return new NormalToken { Type = TokenType.Pop };
                        break;
                    case "br":
                        yield return new NormalToken { Type = TokenType.Br };
                        break;
                    case "brt":
                        yield return new NormalToken { Type = TokenType.Brt };
                        break;
                    case "brf":
                        yield return new NormalToken { Type = TokenType.Brf };
                        break;
                    case "pushenv":
                        yield return new NormalToken { Type = TokenType.PushEnv };
                        break;
                    case "popenv":
                        yield return new NormalToken { Type = TokenType.PopEnv };
                        break;
                    case "push":
                        yield return new NormalToken { Type = TokenType.Push };
                        break;
                    case "call":
                        yield return new NormalToken { Type = TokenType.Call };
                        break;
                    case "break":
                        yield return new NormalToken { Type = TokenType.Break };
                        break;
                    case "cmp":
                        yield return new NormalToken { Type = TokenType.Cmp };
                        break;
                    case "push.cst":
                        yield return new NormalToken { Type = TokenType.PushCst };
                        break;
                    case "push.glb":
                        yield return new NormalToken { Type = TokenType.PushGlb };
                        break;
                    case "push.var":
                        yield return new NormalToken { Type = TokenType.PushVar };
                        break;
                    case "push.i16":
                        yield return new NormalToken { Type = TokenType.PushI16 };
                        break;

                    case "double":
                        yield return new NormalToken { Type = TokenType.Double };
                        break;
                    case "single":
                        yield return new NormalToken { Type = TokenType.Single };
                        break;
                    case "int16":
                        yield return new NormalToken { Type = TokenType.Int16 };
                        break;
                    case "int32":
                        yield return new NormalToken { Type = TokenType.Int32 };
                        break;
                    case "int64":
                        yield return new NormalToken { Type = TokenType.Int64 };
                        break;
                    case "bool":
                        yield return new NormalToken { Type = TokenType.Bool };
                        break;
                    case "var":
                        yield return new NormalToken { Type = TokenType.Var };
                        break;
                    case "string":
                        yield return new NormalToken { Type = TokenType.String };
                        break;
                    case "inst":
                        yield return new NormalToken { Type = TokenType.Inst };
                        break;

                    case "stog":
                        yield return new NormalToken { Type = TokenType.Stog };
                        break;
                    case "self":
                        yield return new NormalToken { Type = TokenType.Self };
                        break;
                    case "other":
                        yield return new NormalToken { Type = TokenType.Other };
                        break;
                    case "all":
                        yield return new NormalToken { Type = TokenType.All };
                        break;
                    case "noone":
                        yield return new NormalToken { Type = TokenType.Noone };
                        break;
                    case "global":
                        yield return new NormalToken { Type = TokenType.Global };
                        break;

                    case "[]":
                        yield return new NormalToken { Type = TokenType.Array };
                        break;
                    case "*":
                        yield return new NormalToken { Type = TokenType.StackTop };
                        break;

                    case "<":
                        yield return new NormalToken { Type = TokenType.LT };
                        break;
                    case "<=":
                        yield return new NormalToken { Type = TokenType.LE };
                        break;
                    case "==":
                        yield return new NormalToken { Type = TokenType.EQ };
                        break;
                    case "!=":
                        yield return new NormalToken { Type = TokenType.NE };
                        break;
                    case ">=":
                        yield return new NormalToken { Type = TokenType.GE };
                        break;
                    case ">":
                        yield return new NormalToken { Type = TokenType.GT };
                        break;
                    default:
                        long lval;
                        double fval;

                        if (w == SR.COLON)
                            yield return new NormalToken { Type = TokenType.Colon };
                        else if (w == "\n")
                            yield return new NormalToken { Type = TokenType.Newline };
                        else if (String.IsNullOrWhiteSpace(w))
                            yield return new NormalToken { Type = TokenType.Whitespace };
                        else if (Int64.TryParse(w, NumberStyles.Integer, CultureInfo.InvariantCulture, out lval) || Int64.TryParse(w, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out lval))
                            yield return new IntToken { Value = lval };
                        else if (Double.TryParse(w, NumberStyles.Float, CultureInfo.InvariantCulture, out fval))
                            yield return new FloatToken { Value = fval };
                        else if (w.StartsWith("\"", StringComparison.Ordinal) && w.EndsWith("\"", StringComparison.Ordinal))
                            yield return new StringToken { Value = Unescape(w.Substring(1, w.Length - 2)) };
                        else
                            yield return new WordToken { Value = w };
                        break;
                }
            }

            yield break;
        }
    }
}
