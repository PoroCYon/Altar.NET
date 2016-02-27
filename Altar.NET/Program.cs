using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using CommandLine;
using LitJson;
using Altar.Decomp;
using Altar.Recomp;
using Altar.Repack;
using Altar.Unpack;

using static Altar.SR;

namespace Altar
{
    // http://pastebin.com/9t783UNE

    using AsmParser = Altar.Recomp.Parser;
    using CLParser  = CommandLine.Parser;

    static class Program
    {
        static void Export(ExportOptions eo)
        {
            var file = Path.GetFullPath(String.IsNullOrEmpty(eo.File) ? DATA_WIN : eo.File);

            if (Directory.Exists(file) && !File.Exists(file))
                file += Path.DirectorySeparatorChar + DATA_WIN;
            if (!File.Exists(file))
                throw new ParserException("File \"" + file + "\" not found.");

            var od = (String.IsNullOrEmpty(eo.OutputDirectory) ? Path.GetDirectoryName(file) : eo.OutputDirectory) + Path.DirectorySeparatorChar;

            using (var f = GMFile.GetFile(file))
            {
                #region defaults
                if (!(eo.Disassemble || eo.String  || eo.Variables || eo.Functions
                        || eo.Audio  || eo.Background || eo.Decompile || eo.Font || eo.General
                        || eo.Object || eo.Options || eo.Path || eo.Room || eo.Script
                        || eo.Sound  || eo.Sprite  || eo.Texture || eo.TPag
                        || eo.ExportToProject || eo.Any))
                    eo.Any = true;

                if (eo.ExportToProject)
                {
                    eo.Disassemble = eo.String = eo.Variables = eo.Functions = false;

                    eo.Audio = eo.Background = eo.Decompile   = eo.Font = eo.General
                        = eo.Object = eo.Options = eo.Path    = eo.Room = eo.Script
                        = eo.Sound  = eo.Sprite  = eo.Texture = eo.TPag = true;
                }
                if (eo.Any)
                {
                    eo.Disassemble = false;

                    eo.Audio = eo.Background = eo.Decompile   = eo.Font = eo.General
                        = eo.Object = eo.Options = eo.Path    = eo.Room = eo.Script
                        = eo.Sound  = eo.Sprite  = eo.Texture = eo.TPag
                        = eo.String = eo.Variables = eo.Functions = true;
                }
                #endregion

                // ---

                #region GEN8
                if (eo.General)
                {
                    Console.WriteLine("Exporting manifest file...");

                    File.WriteAllText(od + "general.json", JsonMapper.ToJson(Serialize.SerializeGeneral(f.General)));
                }
                #endregion
                #region OPTN
                if (eo.Options)
                {
                    Console.WriteLine("Exporting options...");

                    File.WriteAllText(od + "options.json", JsonMapper.ToJson(Serialize.SerializeOptions(f.Options)));
                }
                #endregion

                #region STRG
                if (eo.String)
                {
                    Console.WriteLine("Dumping strings...");

                    File.WriteAllText(od + "strings.json", JsonMapper.ToJson(Serialize.SerializeStrings(f)));
                }
                #endregion
                #region VARI
                if (eo.Variables)
                {
                    Console.WriteLine("Dumping variables...");

                    File.WriteAllText(od + "variables.json", JsonMapper.ToJson(Serialize.SerializeVars(f)));
                }
                #endregion
                #region FUNC
                if (eo.Functions)
                {
                    Console.WriteLine("Dumping functions...");

                    File.WriteAllText(od + "functions.json", JsonMapper.ToJson(Serialize.SerializeFuncs(f)));
                }
                #endregion

                #region TXTR
                if (eo.Texture)
                {
                    Console.Write("Exporting texture sheets... ");
                    var cl = Console.CursorLeft;
                    var ct = Console.CursorTop;

                    if (!Directory.Exists(od + DIR_TEX))
                        Directory.CreateDirectory(od + DIR_TEX);

                    for (int i = 0; i < f.Textures.Length; i++)
                    {
                        Console.SetCursorPosition(cl, ct);
                        Console.WriteLine(O_PAREN + (i + 1) + SLASH + f.Textures.Length + C_PAREN);

                        File.WriteAllBytes(od + DIR_TEX + i + EXT_PNG, f.Textures[i].PngData);
                    }
                }
                #endregion
                #region AUDO
                if (eo.Audio)
                {
                    Console.Write("Exporting audio files... ");
                    var cl = Console.CursorLeft;
                    var ct = Console.CursorTop;

                    var infoTable = new Dictionary<int, SoundInfo>();

                    foreach (var s in f.Sound)
                        if ((s.IsEmbedded || s.IsCompressed) && s.AudioId != -1)
                            infoTable[s.AudioId] = s;

                    if (!Directory.Exists(od + DIR_WAV))
                        Directory.CreateDirectory(od + DIR_WAV);

                    for (int i = 0; i < f.Audio.Length; i++)
                    {
                        Console.SetCursorPosition(cl, ct);
                        Console.WriteLine(O_PAREN + (i + 1) + SLASH + f.Audio.Length + C_PAREN);

                        File.WriteAllBytes(od + DIR_WAV + infoTable[i].Name + SR.EXT_WAV, f.Audio[i].Wave);
                    }
                }
                #endregion
                #region CODE
                if (eo.Decompile)
                {
                    Console.Write("Decompiling code... ");
                    var cl = Console.CursorLeft;
                    var ct = Console.CursorTop;

                    if (!Directory.Exists(od + DIR_CODE))
                        Directory.CreateDirectory(od + DIR_CODE);

                    for (int i = 0; i < f.Code.Length; i++)
                    {
                        Console.SetCursorPosition(cl, ct);
                        Console.WriteLine(O_PAREN + (i + 1) + SLASH + f.Code.Length + C_PAREN);

                        File.WriteAllText(od + DIR_CODE + f.Code[i].Name + EXT_GML_LSP, Decompiler.DecompileCode(f, i, eo.AbsoluteAddresses));
                    }
                }
                if (eo.Disassemble)
                {
                    Console.Write("Disassembling bytecode... ");
                    var cl = Console.CursorLeft;
                    var ct = Console.CursorTop;

                    if (!Directory.Exists(od + DIR_CODE))
                        Directory.CreateDirectory(od + DIR_CODE);

                    for (int i = 0; i < f.Code.Length; i++)
                    {
                        Console.SetCursorPosition(cl, ct);
                        Console.WriteLine(O_PAREN + (i + 1) + SLASH + f.Code.Length + C_PAREN);

                        File.WriteAllText(od + DIR_CODE + f.Code[i].Name + EXT_GML_ASM, Disassembler.DisplayInstructions(f, i, eo.AbsoluteAddresses));
                    }
                }
                #endregion

                #region SCPT
                if (eo.Script)
                {
                    Console.Write("Exporting scripts... ");
                    var cl = Console.CursorLeft;
                    var ct = Console.CursorTop;

                    if (!Directory.Exists(od + DIR_SCR))
                        Directory.CreateDirectory(od + DIR_SCR);

                    for (int i = 0; i < f.Scripts.Length; i++)
                    {
                        Console.SetCursorPosition(cl, ct);
                        Console.WriteLine(O_PAREN + (i + 1) + SLASH + f.Scripts.Length + C_PAREN);

                        File.WriteAllText(od + DIR_SCR + f.Scripts[i].Name + EXT_JSON, JsonMapper.ToJson(Serialize.SerializeScript(f.Scripts[i], f.Code)));
                    }
                }
                #endregion
                #region TPAG
                if (eo.TPag)
                {
                    Console.Write("Exporting texture maps... ");
                    var cl = Console.CursorLeft;
                    var ct = Console.CursorTop;

                    if (!Directory.Exists(od + DIR_TXP))
                        Directory.CreateDirectory(od + DIR_TXP);

                    for (int i = 0; i < f.TexturePages.Length; i++)
                    {
                        Console.SetCursorPosition(cl, ct);
                        Console.WriteLine(O_PAREN + (i + 1) + SLASH + f.TexturePages.Length + C_PAREN);

                        File.WriteAllText(od + DIR_TXP + i + EXT_JSON, JsonMapper.ToJson(Serialize.SerializeTPag(f.TexturePages[i])));
                    }
                }
                #endregion
                #region SPRT
                if (eo.Sprite)
                {
                    Console.Write("Exporting sprites... ");
                    var cl = Console.CursorLeft;
                    var ct = Console.CursorTop;

                    if (!Directory.Exists(od + DIR_SPR))
                        Directory.CreateDirectory(od + DIR_SPR);

                    for (int i = 0; i < f.Sprites.Length; i++)
                    {
                        Console.SetCursorPosition(cl, ct);
                        Console.WriteLine(O_PAREN + (i + 1) + SLASH + f.Sprites.Length + C_PAREN);

                        File.WriteAllText(od + DIR_SPR + f.Sprites[i].Name + EXT_JSON, JsonMapper.ToJson(Serialize.SerializeSprite(f.Sprites[i])));
                    }
                }
                #endregion
                #region SOND
                if (eo.Sound)
                {
                    Console.Write("Exporting sounds... ");
                    var cl = Console.CursorLeft;
                    var ct = Console.CursorTop;

                    if (!Directory.Exists(od + DIR_SND))
                        Directory.CreateDirectory(od + DIR_SND);

                    for (int i = 0; i < f.Sound.Length; i++)
                    {
                        Console.SetCursorPosition(cl, ct);
                        Console.WriteLine(O_PAREN + (i + 1) + SLASH + f.Sound.Length + C_PAREN);

                        File.WriteAllText(od + DIR_SND + f.Sound[i].Name + EXT_JSON, JsonMapper.ToJson(Serialize.SerializeSound(f.Sound[i])));
                    }
                }
                #endregion

                #region OBJT
                if (eo.Object)
                {
                    Console.Write("Exporting objects... ");
                    var cl = Console.CursorLeft;
                    var ct = Console.CursorTop;

                    if (!Directory.Exists(od + DIR_OBJ))
                        Directory.CreateDirectory(od + DIR_OBJ);

                    for (int i = 0; i < f.Objects.Length; i++)
                    {
                        Console.SetCursorPosition(cl, ct);
                        Console.WriteLine(O_PAREN + (i + 1) + SLASH + f.Objects.Length + C_PAREN);

                        File.WriteAllText(od + DIR_OBJ + f.Objects[i].Name + EXT_JSON, JsonMapper.ToJson(Serialize.SerializeObj(f.Objects[i], f.Sprites, f.Objects)));
                    }
                }
                #endregion
                #region BGND
                if (eo.Background)
                {
                    Console.Write("Exporting backgrounds... ");
                    var cl = Console.CursorLeft;
                    var ct = Console.CursorTop;

                    if (!Directory.Exists(od + DIR_BG))
                        Directory.CreateDirectory(od + DIR_BG);

                    for (int i = 0; i < f.Backgrounds.Length; i++)
                    {
                        Console.SetCursorPosition(cl, ct);
                        Console.WriteLine(O_PAREN + (i + 1) + SLASH + f.Backgrounds.Length + C_PAREN);

                        File.WriteAllText(od + DIR_BG + f.Backgrounds[i].Name + EXT_JSON, JsonMapper.ToJson(Serialize.SerializeBg(f.Backgrounds[i])));
                    }
                }
                #endregion
                #region ROOM
                if (eo.Room)
                {
                    Console.Write("Exporting rooms... ");
                    var cl = Console.CursorLeft;
                    var ct = Console.CursorTop;

                    if (!Directory.Exists(od + DIR_ROOM))
                        Directory.CreateDirectory(od + DIR_ROOM);

                    for (int i = 0; i < f.Rooms.Length; i++)
                    {
                        Console.SetCursorPosition(cl, ct);
                        Console.WriteLine(O_PAREN + (i + 1) + SLASH + f.Rooms.Length + C_PAREN);

                        File.WriteAllText(od + DIR_ROOM + f.Rooms[i].Name + EXT_JSON, JsonMapper.ToJson(Serialize.SerializeRoom(f.Rooms[i], f.Backgrounds, f.Objects)));
                    }
                }
                #endregion

                #region FONT
                if (eo.Background)
                {
                    Console.Write("Exporting fonts... ");
                    var cl = Console.CursorLeft;
                    var ct = Console.CursorTop;

                    if (!Directory.Exists(od + DIR_FNT))
                        Directory.CreateDirectory(od + DIR_FNT);

                    for (int i = 0; i < f.Fonts.Length; i++)
                    {
                        Console.SetCursorPosition(cl, ct);
                        Console.WriteLine(O_PAREN + (i + 1) + SLASH + f.Fonts.Length + C_PAREN);

                        File.WriteAllText(od + DIR_FNT + f.Fonts[i].CodeName + EXT_JSON, JsonMapper.ToJson(Serialize.SerializeFont(f.Fonts[i])));
                    }
                }
                #endregion
                #region PATH
                if (eo.Path)
                {
                    Console.Write("Exporting paths... ");
                    var cl = Console.CursorLeft;
                    var ct = Console.CursorTop;

                    if (!Directory.Exists(od + DIR_PATH))
                        Directory.CreateDirectory(od + DIR_PATH);

                    for (int i = 0; i < f.Paths.Length; i++)
                    {
                        Console.SetCursorPosition(cl, ct);
                        Console.WriteLine(O_PAREN + (i + 1) + SLASH + f.Paths.Length + C_PAREN);

                        File.WriteAllText(od + DIR_PATH + f.Paths[i].Name + EXT_JSON, JsonMapper.ToJson(Serialize.SerializePath(f.Paths[i])));
                    }
                }
                #endregion

                if (eo.ExportToProject)
                {
                    Console.WriteLine("Emitting project file...");

                    File.WriteAllText(od + f.General.Name + EXT_JSON, JsonMapper.ToJson(Serialize.SerializeProject(f)));
                }
            }
        }

        // used to prove the chunk order doesn't matter
        unsafe static void SwapChunks(GMFileContent f)
        {
            int EmptyChunkSize = sizeof(SectionUnknown);

            var exntO = (long)f.Extensions - (long)f.RawData.BPtr;
            var tmlnO = (long)f.Timelines  - (long)f.RawData.BPtr;

            byte[] extnT = new byte[EmptyChunkSize];

            ILHacks.Cpblk((IntPtr)f.Extensions, extnT, 0, EmptyChunkSize);
            ILHacks.Cpblk((IntPtr)f.Timelines, (IntPtr)f.Extensions, EmptyChunkSize);
            ILHacks.Cpblk(extnT, (IntPtr)f.Timelines, 0, EmptyChunkSize);

            byte[] EVERYTHING = new byte[f.RawData.Size];

            ILHacks.Cpblk(f.RawData.IPtr, EVERYTHING, 0, f.RawData.Size);

            File.WriteAllBytes("data.fake.win", EVERYTHING);
        }

        [STAThread]
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture =
                CultureInfo.InvariantCulture;

            //var t =
            //    Tokenizer.Tokenize(
            //        File.ReadAllText(@"C:\Program Files (x86)\Steam\steamapps\Common\Undertale\datadump\code\gml_Script_SCR_TEXT.gml.asm")
            //    );
            //var p = AsmParser.Parse(t);

            //var recons = String.Join(Environment.NewLine, p.Select(i => i.ToString()));

            var o = new Options();

            CLParser.Default.ParseArgumentsStrict(args, o, (verb, vo) =>
            {
                if (vo == null)
                    return;

                switch (verb)
                {
                    case "export":
                        Export((ExportOptions)vo);
                        break;
                }
            });
        }
    }
}
