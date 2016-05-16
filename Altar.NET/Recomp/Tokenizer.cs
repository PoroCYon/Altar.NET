using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Altar.Recomp
{
    public static class Tokenizer
    {
        enum CommentType : byte
        {
            None,
            Line,
            Block
        }

        const TokenType NullTokenType = (TokenType)(-1);

        readonly static char[] WordSep = " \r\n\t".ToCharArray();
        readonly static string[] LineComments = { ";", "//" };
        readonly static string BlockCommentStart = "/*", BlockCommentEnd = "*/";
        readonly static string[] SpecialWords = { ":", "[]", "*" };
        readonly static string NewlineWord = "\n", Quote = "\"";

        static string Unescape(string s) =>
            s.Replace("\\\\", "\\").Replace("\\\"", "\"").Replace("\\b", "\b")
             .Replace("\\r" , "\r").Replace("\\n" , "\n").Replace("\\t", "\t");

        public static TokenKind KindOf(TokenType type)
        {
            if (type <= TokenType.Newline || type == TokenType.Magic)
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
            var rWhB = new StringBuilder();

            int line = 1;
            int lastLineIndex = 0;

            Func<char, bool> IsWordSep = c => (Array.IndexOf(WordSep, c) >= 0 || Char.IsWhiteSpace(c));
            Func<int> PeekChar = () => pos == code.Length ? -1 : code[pos  ];
            Func<int> ReadChar = () => pos == code.Length ? -1 : code[pos++];
            Func<Predicate<char>, string> ReadWhile = p =>
            {
                rWhB.Clear();

                while (true)
                {
                    var c = PeekChar();
                    if (c == -1 || !p((char)c))
                        break;

                    rWhB.Append((char)c);
                    ReadChar();
                }

                return rWhB.ToString();

            };
            Func<string, bool, bool> MatchString = (s, skip) =>
            {
                var cpos = pos;

                for (int i = 0; i < s.Length; i++)
                {
                    if (PeekChar() != s[i])
                    {
                        pos = cpos;
                        return false;
                    }

                    ReadChar();
                }

                if (!skip)
                    pos = cpos;

                return true;
            };
            Func<CommentType> IsComment = () =>
            {
                for (int i = 0; i < LineComments.Length; i++)
                    if (MatchString(LineComments[i], true))
                        return CommentType.Line;
                if (MatchString(BlockCommentStart, true))
                    return CommentType.Block;

                return CommentType.None;
            };
            Func<bool> SkipComments = () =>
            {
                var r = false;

                CommentType t;
                while ((t = IsComment()) != CommentType.None)
                {
                    switch (t)
                    {
                        case CommentType.Line:
                            ReadWhile(c => c != '\r' && c != '\n');
                            break;
                        case CommentType.Block:
                            ReadWhile(_ => !MatchString(BlockCommentEnd, false));
                            break;
                    }

                    r = true;
                }

                return r;
            };
            
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

                    // comment -> ignore
                    if (!inString && SkipComments()) // && can break early -> SkipComments is only called when !inString is true
                        break;

                    #region match special things
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
                            if (SkipComments())
                                goto BREAK_OUTER;

                            if (SpecialWords[si][i] != PeekChar())
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
                    #endregion

                    // change in-string state if the " isn't escaped
                    if (p == '"' && !inEsc)
                        inString = !inString;

                    // write char to current word
                    ReadChar();
                    _rwB.Append(p == '\r' ? '\n' : (char)p); // normalise to \n
                    if (p == '\r' && PeekChar() == '\n') ReadChar(); // merge CRLF

                    // '\' is the escape character, but the 2nd char in "\\" shouldn't be counted as escape
                    inEsc = p == '\\' && !inEsc;

                    var op = p;
                    p = PeekChar();

                    // merge whitespace continuations
                    while (!inString && op == p /* op != -1 (see earlier break) -> will break if p == -1 */ && IsWordSep((char)p))
                    {
                        // next char
                        op = p;
                        p = ReadChar();

                        if (SkipComments())
                            goto BREAK_OUTER;
                    }

                    if (p == -1 || ((IsWordSep((char)p) || IsWordSep((char)op)) && !inString))
                        break;
                }
            BREAK_OUTER:

                return _rwB.ToString();
            };

            while (pos < code.Length)
            {
                string w = ReadWord();
                int col = pos - lastLineIndex;

                if (!String.IsNullOrWhiteSpace(w))
                    w = w.Trim();

                var type = NullTokenType;

                switch (w.ToUpperInvariant())
                {
                    #region special cases
                    case "PUSH.CST":
                        type = TokenType.PushCst;
                        break;
                    case "PUSH.LOC":
                        type = TokenType.PushLoc;
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

                    case "!MAGIC":
                        type = TokenType.Magic;
                        break;
                    #endregion

                    default:
                        if (Utils.TryParseEnum(w, true, false, false, ref type))
                        {
                            // ignore
                            if ((type <= TokenType.PushI16 && type > TokenType.Cmp) || type > TokenType.Global)
                                type = NullTokenType;
                                //throw new FormatException($"Unexpected token '{type}' at line {line} and column {col}.");
                        }

                        if (type == NullTokenType)
                        {
                            long lval;
                            double fval;

                            if (w == SR.COLON)
                                type = TokenType.Colon;
                            else if (w == NewlineWord)
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
                            else if (w.StartsWith(Quote, StringComparison.Ordinal) && w.EndsWith(Quote, StringComparison.Ordinal))
                                yield return new StringToken { OrigString = w, Value = Unescape(w.Substring(1, w.Length - 2)), Line = line, Column = col };
                            else
                                yield return new WordToken { OrigString = w, Value = w, Line = line, Column = col };
                        }
                        break;
                }

                if (type != NullTokenType)
                    yield return new NormalToken { OrigString = w, Type = type, Line = line, Column = col };
            }

            yield break;
        }
    }
}
