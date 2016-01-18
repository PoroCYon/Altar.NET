using System;
using System.Collections.Generic;
using System.Linq;

namespace Altar
{
    static class SR
    {
        internal readonly static string
            SPACE_S   = " " ,
            COLON     = ":" ,
            COLON_S   = ": ",
            ASTERISK  = "*" ,
            HASH      = "#" ,
            C_BRACE   = "}" ,
            O_BRACE   = "{" ,
            COMMA_S   = ", ",
            UNDERSC   = "_" ,
            C_BRACKET = "]" ,
            O_BRACKET = "[" ,
            DOT       = "." ,
            C_PAREN   = ")" ,
            O_PAREN   = "(" ,
            DEL_CHAR  = "^H",
            LF_CHAR   = "\n",
            CR_CHAR   = "\r",
            TAB_CHAR  = "\t",
            BELL_CHAR = "\b",
            NUL_CHAR  = "\0",
            TILDE     = "~" ,
            DASH      = "-" ,
            PLUS      = "+" ,
            AMP       = "&" ,
            SLASH     = "/" ,
            EQUAL     = "==",
            GT        = ">" ,
            GTE       = ">=",
            NEQUAL    = "!=",
            LT        = "<" ,
            LTE       = "<=",
            MOD       = "%" ,
            VBAR      = "|" ,
            RIGHTSH   = ">>",
            LEFTSH    = "<<",
            XOR       = "^" ,

            INDENT2 = "  ",
            INDENT4 = "    ",

            EQ_S = " = ",

            BRACKETS = "[]",
            HEX_PRE  = "0x",
            HEX_FM8  = "X8",
            HEX_FM6  = "X6",

            DOUBLE_L = "d",
            SINGLE_L = "f",
            SHORT_L  = "s",
            LONG_L   = "L",

            TRUE  = "true" ,
            FALSE = "false",

            DATA_WIN = "data.win",

            DONE = "Done.",

            EXT_TXT     = ".txt",
            EXT_BIN     = ".bin",
            EXT_PNG     = ".png",
            EXT_WAV     = ".wav",
            EXT_INI     = ".ini",
            EXT_GML_ASM = ".gml.asm",
            EXT_GML_LSP = ".gml.lsp",

            FILE_STR = "strings.txt",
            FILE_VAR = "vars.txt"   ,
            FILE_FNS = "funcs.txt"  ,

            DIR_TEX  = "texture/",
            DIR_TXP  = "texpage/",
            DIR_WAV  = "audio/"  ,
            DIR_SND  = "sound/"  ,
            DIR_ROOM = "room/"   ,
            DIR_OBJ  = "object/" ,
            DIR_BG   = "bg/"     ,
            DIR_CODE = "code/"   ,
            DIR_SCR  = "script/" ,
            DIR_SPR  = "sprite/" ,
            DIR_FNT  = "font/"   ,
            DIR_PATH = "path/"   ,

            VAR  = "var" ,
            INST = "inst",
            BOOL = "bool",
            STOG = "stog",
            NULL = "null",

            CALL  = "call "  ,
            IFF   = "if !"   ,
            IFT   = "if "    ,
            GOTO  = "goto "  ,
            PUSHE = "pushenv",
            POPE  = "popenv" ,
            PUSH  = "push "  ,
            POP   = "pop"    ,
            DUP   = "dup"    ,
            BREAK = "break " ,
            RET_S = "ret "   ,
            EXIT  = "exit"   ,

            REMAIN = "rem"        ,
            ASSERT = "assert_neq:",

            ERR_NO_FORM   = "No 'FORM' header.",
            ERR_FILE_NF_1 = "File \""          ,
            ERR_FILE_NF_2 = "\" not found."    ;
    }
}
