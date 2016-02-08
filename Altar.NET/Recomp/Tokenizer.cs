using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Altar.Recomp
{
    public static class Tokenizer
    {
        readonly static char[] WordSep = " \r\n\t".ToCharArray();
        readonly static string[] SpecialWords = { ":", "[]", "*" };

        static string Unescape(string s) =>
            s.Replace("\\\\", "\\").Replace("\\\"", "\"").Replace("\\b", "\b")
             .Replace("\\r" , "\r").Replace("\\n" , "\n").Replace("\\t", "\t");

        public static TokenKind KindOf(TokenType type)
        {
            if (type <= TokenType.Newline)
                return TokenKind.Other;
            if (type <= TokenType.PushI16)
                return TokenKind.OpCode;
#pragma warning disable 618
            if (type <= TokenType.Inst)
                return TokenKind.DataType;
#pragma warning restore 618
            if (type <= TokenType.Global)
                return TokenKind.InstanceType;
            if (type <= TokenType.StackTop)
                return TokenKind.VariableType;
            if (type <= TokenType.GT)
                return TokenKind.ComparisonType;

            throw new ArgumentOutOfRangeException($"Invalid token type '{type}'. Is the KindOf method updated?");
        }

        public static IEnumerable<Token> Tokenize(string code)
        {
            int pos = 0;
            var _rwB = new StringBuilder();

            int line = 1;
            int lastLineIndex = 0;

            Func<char, bool> IsWordSep = c => (Array.IndexOf(WordSep, c) > 0 || Char.IsWhiteSpace(c));
            Func<int   > PeekChar = () => pos == code.Length ? -1 : code[pos  ];
            Func<int   > ReadChar = () => pos == code.Length ? -1 : code[pos++];
            #region Func<string> ReadWord = () => { [...] };
            // basically the lexer fn
            //TODO: URGENT REFACTOR NEEDED!
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

                    // comment -> ignore
                    // only line comments for now
                    if (p == ';' && !inString)
                    {
                        // read until end of line
                        do
                        {
                            ReadChar();
                            p = PeekChar();
                        } while (p != -1 && p != '\r' && p != '\n');
                        break; // return
                    }

                    var si = inString ? -1 : Array.FindIndex(SpecialWords, s => s[0] == p);
                    if (si > -1) // one of the special things (':', '*' or '[]')
                    {
                        // end now -> in next word
                        if (_rwB.Length != 0)
                            break;

                        var opos = pos;

                        ReadChar(); // next

                        for (int i = 1; i < SpecialWords[si].Length; i++)
                        {
                            var p_ = PeekChar();

                            // comment
                            if (p_ == ';')
                            {
                                do
                                {
                                    ReadChar();
                                    p_ = PeekChar();
                                } while (p_ != -1 && p_ != '\r' && p_ != '\n');
                                return _rwB.ToString();
                            }

                            if (SpecialWords[si][i] != p_)
                                goto IGNORE;

                            ReadChar();
                        }

                        // matches
                        pos = opos;

                        // write word to output
                        for (int i = 0; i < SpecialWords[si].Length; i++)
                            _rwB.Append((char)ReadChar());

                        break; // returns

                    IGNORE:
                        pos = opos;
                    }

                    if (p == '"' && !inEsc)
                        inString = !inString;

                    ReadChar();
                    _rwB.Append(p == '\r' ? '\n' : (char)p); // normalise to \n

                    if (p == '\r' && PeekChar() == '\n') ReadChar(); // merge CRLF

                    inEsc = p == '\\' && !inEsc;

                    var op = p;
                    p = PeekChar();

                    // merge whitespace continuations
                    while (!inString && op == p /* op != -1 (see earlier break) -> will break if p == -1 */ && IsWordSep((char)p))
                    {
                        // next char
                        op = p;
                        p = ReadChar();

                        // comment
                        if (p == ';')
                        {
                            do
                            {
                                ReadChar();
                                p = PeekChar();
                            } while (p != -1 && p != '\r' && p != '\n');
                            return _rwB.ToString();
                        }
                    }

                    if (p == -1 || ((IsWordSep((char)p) || IsWordSep((char)op)) && !inString))
                        break;
                }

                return _rwB.ToString();
            };
            #endregion

            while (pos < code.Length)
            {
                string w = ReadWord();
                int col = pos - lastLineIndex;

                if (!String.IsNullOrWhiteSpace(w))
                    w = w.Trim();

                var type = (TokenType)(-1);

                switch (w.ToUpperInvariant())
                {
                    #region special cases
                    case "PUSH.CST":
                        type = TokenType.PushCst;
                        break;
                    case "PUSH.GLB":
                        type = TokenType.PushGlb;
                        break;
                    case "PUSH.VAR":
                        type = TokenType.PushVar;
                        break;
                    case "PUSH.I16":
                        type = TokenType.PushI16;
                        break;

                    case "[]":
                        type = TokenType.Array;
                        break;
                    case "*":
                        type = TokenType.StackTop;
                        break;

                    case "<":
                        type = TokenType.LT;
                        break;
                    case "<=":
                        type = TokenType.LE;
                        break;
                    case "==":
                        type = TokenType.EQ;
                        break;
                    case "!=":
                        type = TokenType.NE;
                        break;
                    case ">=":
                        type = TokenType.GE;
                        break;
                    case ">":
                        type = TokenType.GT;
                        break;
                    #endregion

                    default:
                        if (Utils.TryParseEnum(w, true, false, false, ref type))
                        {
                            // ignore
                            if ((type <= TokenType.PushI16 && type > TokenType.Cmp) || type > TokenType.Global)
                                type = (TokenType)(-1);
                                //throw new FormatException($"Unexpected token '{type}' at line {line} and column {col}.");
                        }

                        if ((short)type == -1)
                        {
                            long lval;
                            double fval;

                            if (w == SR.COLON)
                                type = TokenType.Colon;
                            else if (w == "\n")
                            {
                                line++;
                                lastLineIndex = pos;
                                type = TokenType.Newline;
                            }
                            else if (String.IsNullOrWhiteSpace(w))
                                type = TokenType.Whitespace;
                            else if (Int64.TryParse(w, NumberStyles.Integer, CultureInfo.InvariantCulture, out lval)
                                    || (w.StartsWith(SR.HEX_PRE, StringComparison.OrdinalIgnoreCase)
                                        && Int64.TryParse(w, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out lval)))
                                yield return new IntToken { OrigString = w, Value = lval, Line = line, Column = col };
                            else if (Double.TryParse(w, NumberStyles.Float, CultureInfo.InvariantCulture, out fval))
                                yield return new FloatToken { OrigString = w, Value = fval, Line = line, Column = col };
                            else if (w.StartsWith("\"", StringComparison.Ordinal) && w.EndsWith("\"", StringComparison.Ordinal))
                                yield return new StringToken { OrigString = w, Value = Unescape(w.Substring(1, w.Length - 2)), Line = line, Column = col };
                            else
                                yield return new WordToken { OrigString = w, Value = w, Line = line, Column = col };
                        }
                        break;
                }

                if ((short)type != -1)
                    yield return new NormalToken { OrigString = w, Type = type, Line = line, Column = col };
            }

            yield break;
        }
    }
}
