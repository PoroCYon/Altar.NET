using System;
using System.Collections.Generic;
using System.Linq;

namespace Altar.Recomp
{
    public enum TokenType : short
    {
        // other
        Whitespace,
        Colon     ,
        Newline   ,

        // opcode
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
        Cne    ,
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
        PushLoc,
        PushGlb,
        PushVar,
        PushI16,

        // datatype
        Double,
        Single,
        Int16 ,
        Int32 ,
        Int64 ,
        Bool  ,
        Var   ,
        String,
        /// <summary>
        /// Unused
        /// </summary>
        [Obsolete("Unused")]
        Inst  ,

        // instancetype
        Stog  ,
        Self  ,
        Other ,
        All   ,
        Noone ,
        Global,
        Local ,

        // variabletype
        Array   ,
        StackTop,

        // comparisontype
        LT,
        LE,
        EQ,
        NE,
        GE,
        GT,

        Magic
    }
    public enum TokenKind : sbyte
    {
        Other         ,
        OpCode        ,
        DataType      ,
        InstanceType  ,
        VariableType  ,
        ComparisonType
    }

    public abstract class Token
    {
        public int Line;
        public int Column;

        public string OrigString;
    }

    public class NormalToken : Token
    {
        public TokenType Type;

        public TokenKind Kind => Tokenizer.KindOf(Type);

        public override string ToString() => Type.ToString();
    }
    public class IntToken : Token
    {
        public long Value;

        public override string ToString() => Value.ToString();
    }
    public class FloatToken : Token
    {
        public double Value;

        public override string ToString() => Value.ToString();
    }
    public class StringToken : Token
    {
        public string Value;

        public override string ToString() => Value.ToString();
    }
    public class WordToken : Token
    {
        public string Value;

        public override string ToString() => Value.ToString();
    }
}
