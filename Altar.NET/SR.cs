using System;
using System.Collections.Generic;
using System.Linq;

namespace Altar.NET
{
    static class SR
    {
        internal readonly static string
            SPACE_S     = " " ,
            COLON     = ":" ,
            ASTERISK  = "*" ,
            HASH      = "#" ,
            C_BRACE   = "}" ,
            O_BRACE   = "{" ,
            COMMA_S   = ", ",
            UNDERSC   = "_" ,
            C_BRACKET = "]" ,
            O_BRACKET = "[" ,
            DEL_CHAR  = "^H",

            BRACKETS = "[]",
            HEX_PRE  = "0x",
            HEX_FM8  = "X8",
            HEX_FM6  = "X6",

            TRUE  = "true",
            FALSE = "false",

            DATA_WIN = "data.win",

            DONE = "Done.",

            EXT_TXT     = ".txt",
            EXT_BIN     = ".bin",
            EXT_PNG     = ".png",
            EXT_WAV     = ".wav",
            EXT_INI     = ".ini",
            EXT_GML_ASM = ".gml.asm",

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

            ERR_NO_FORM   = "No 'FORM' header.",
            ERR_FILE_NF_1 = "File \""          ,
            ERR_FILE_NF_2 = "\" not found."    ;
    }
}
