using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using CommandLine;
using CommandLine.Text;
using LitJson;

namespace Altar
{
    // http://pastebin.com/9t783UNE

    using static SR;

    static class Program
    {
        class ExportOptions
        {
            [Option('g', HelpText = "Export manifest file.", MutuallyExclusiveSet = "EXPORT")]
            public bool General
            {
                get;
                set;
            }
            [Option('o', HelpText = "Export options.", MutuallyExclusiveSet = "EXPORT")]
            public bool Options
            {
                get;
                set;
            }
            [Option('n', HelpText = "Export sound definitions (NOTE: this does NOT export the audio files themselves, use -a instead).", MutuallyExclusiveSet = "EXPORT")]
            public bool Sound
            {
                get;
                set;
            }
            [Option('s', HelpText = "Export sprite definitions (NOTE: this does NOT export the texture files themselves, use -t instead).", MutuallyExclusiveSet = "EXPORT")]
            public bool Sprite
            {
                get;
                set;
            }
            [Option('b', HelpText = "Export background definitions.", MutuallyExclusiveSet = "EXPORT")]
            public bool Background
            {
                get;
                set;
            }
            [Option('p', HelpText = "Export path definitions.", MutuallyExclusiveSet = "EXPORT")]
            public bool Path
            {
                get;
                set;
            }
            [Option('i', HelpText = "Export script definitions (NOTE: this does NOT export the decompiled or decompiled code, use -c or -d instead).", MutuallyExclusiveSet = "EXPORT")]
            public bool Script
            {
                get;
                set;
            }
            [Option('f', HelpText = "Export font definitions.", MutuallyExclusiveSet = "EXPORT")]
            public bool Font
            {
                get;
                set;
            }
            [Option('o', HelpText = "Export object definitions.", MutuallyExclusiveSet = "EXPORT")]
            public bool Object
            {
                get;
                set;
            }
            [Option('r', HelpText = "Export room definitions.", MutuallyExclusiveSet = "EXPORT")]
            public bool Room
            {
                get;
                set;
            }
            [Option('m', HelpText = "Export texture maps (NOTE: does not contain the textures themselves, use -t instead).", MutuallyExclusiveSet = "EXPORT")]
            public bool TPag
            {
                get;
                set;
            }
            [Option('t', HelpText = "Export texture sheets.", MutuallyExclusiveSet = "EXPORT")]
            public bool Texture
            {
                get;
                set;
            }
            [Option('a', HelpText = "Export audio files.", MutuallyExclusiveSet = "EXPORT")]
            public bool Audio
            {
                get;
                set;
            }
            [Option('c', HelpText = "Decompile code.", MutuallyExclusiveSet = "EXPORT")]
            public bool Decompile
            {
                get;
                set;
            }
            [Option('d', HelpText = "Disassemble code (NOTE: displays GM:S VMASM, NOT high-level code, use -c instead).", MutuallyExclusiveSet = "EXPORT")]
            public bool Disassemble
            {
                get;
                set;
            }
            [Option('u', HelpText = "Dump strings.", MutuallyExclusiveSet = "EXPORT")]
            public bool String
            {
                get;
                set;
            }
            [Option('w', HelpText = "Dump variable names.", MutuallyExclusiveSet = "EXPORT")]
            public bool Variables
            {
                get;
                set;
            }
            [Option('h', HelpText = "Dump function names.", MutuallyExclusiveSet = "EXPORT")]
            public bool Functions
            {
                get;
                set;
            }
            [Option('*', "any", HelpText = "Export everything (except -d).", MutuallyExclusiveSet = "EXPORT")]
            public bool Any
            {
                get;
                set;
            }

            [Option("absolute", HelpText = "Use absolute instead of relative offsets in code disassembly/decompilation. Ignored if -c or -d aren't provided.", MutuallyExclusiveSet = "EXPORT")]
            public bool AbsoluteAddresses
            {
                get;
                set;
            }

            [Option("project", HelpText = "Export everything (except -d, -w and -h) to a project-like structure and emit a project file that can be rebuilt (in a later version).", MutuallyExclusiveSet = "PROJ")]
            public bool ExportToProject
            {
                get;
                set;
            }

            [Option("file", Required = true, DefaultValue = "data.win", HelpText = "Specifies the data.win file to export.")]
            public string File
            {
                get;
                set;
            }
            [Option("out", HelpText = "Specifies the output folder.")]
            public string OutputDirectory
            {
                get;
                set;
            }

            [HelpOption]
            public string GetUsage() => HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
        }
        class Options
        {
            [VerbOption("export", HelpText = "Export contents of a data.win file. See export --help for more info.")]
            public ExportOptions Export
            {
                get;
                set;
            }

            [HelpVerbOption]
            public string GetUsage(string v) => HelpText.AutoBuild(this, v);
        }

        [STAThread]
        unsafe static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture =
                CultureInfo.InvariantCulture;

            var o = new Options();

            Parser.Default.ParseArgumentsStrict(args, o, (verb, vo) =>
            {
                if (vo == null)
                    return;

                switch (verb)
                {
                    case "export":
                        {
                            var eo = (ExportOptions)vo;

                            var file = Path.GetFullPath(String.IsNullOrEmpty(eo.File) ? DATA_WIN : eo.File);

                            if (Directory.Exists(file) && !File.Exists(file))
                                file += Path.DirectorySeparatorChar + DATA_WIN;
                            if (!File.Exists(file))
                                throw new ParserException("File \"" + file + "\" not found.");

                            var od = (String.IsNullOrEmpty(eo.OutputDirectory) ? Path.GetDirectoryName(file) : eo.OutputDirectory) + Path.DirectorySeparatorChar;

                            using (var f = GMFile.GetFile(file))
                            {
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

                                #region GEN8
                                if (eo.General)
                                {
                                    Console.WriteLine("Exporting manifest file...");

                                    File.WriteAllText(od + "general.json", JsonMapper.ToJson(Serialise.SerialiseGeneral(f)));
                                }
                                #endregion
                                #region OPTN
                                if (eo.Options)
                                {
                                    Console.WriteLine("Exporting options...");

                                    File.WriteAllText(od + "options.json", JsonMapper.ToJson(Serialise.SerialiseOptions(f)));
                                }
                                #endregion

                                #region STRG
                                if (eo.String)
                                {
                                    Console.WriteLine("Dumping strings...");

                                    File.WriteAllText(od + "strings.json", JsonMapper.ToJson(Serialise.SerialiseStrings(f)));
                                }
                                #endregion
                                #region VARI
                                if (eo.Variables)
                                {
                                    Console.WriteLine("Dumping variables...");

                                    File.WriteAllText(od + "variables.json", JsonMapper.ToJson(Serialise.SerialiseVars(f)));
                                }
                                #endregion
                                #region FUNC
                                if (eo.Functions)
                                {
                                    Console.WriteLine("Dumping functions...");

                                    File.WriteAllText(od + "functions.json", JsonMapper.ToJson(Serialise.SerialiseFuncs(f)));
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

                                        File.WriteAllText(od + DIR_CODE + i + EXT_GML_ASM, Disassembler.DisplayInstructions(f, i, eo.AbsoluteAddresses));
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

                                        File.WriteAllText(od + DIR_SCR + f.Scripts[i].Name + EXT_JSON, JsonMapper.ToJson(Serialise.SerialiseScript(f, i)));
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

                                        File.WriteAllText(od + DIR_TXP + i + EXT_JSON, JsonMapper.ToJson(Serialise.SerialiseTPag(f, i)));
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

                                        File.WriteAllText(od + DIR_SPR + f.Sprites[i].Name + EXT_JSON, JsonMapper.ToJson(Serialise.SerialiseSprite(f, i)));
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

                                        File.WriteAllText(od + DIR_SND + f.Sound[i].Name + EXT_JSON, JsonMapper.ToJson(Serialise.SerialiseSound(f, i)));
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

                                        File.WriteAllText(od + DIR_OBJ + f.Objects[i].Name + EXT_JSON, JsonMapper.ToJson(Serialise.SerialiseObj(f, i)));
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

                                        File.WriteAllText(od + DIR_BG + f.Backgrounds[i].Name + EXT_JSON, JsonMapper.ToJson(Serialise.SerialiseBg(f, i)));
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

                                        File.WriteAllText(od + DIR_ROOM + f.Rooms[i].Name + EXT_JSON, JsonMapper.ToJson(Serialise.SerialiseRoom(f, i)));
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

                                        File.WriteAllText(od + DIR_FNT + f.Fonts[i].CodeName + EXT_JSON, JsonMapper.ToJson(Serialise.SerialiseFonts(f, i)));
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

                                        File.WriteAllText(od + DIR_PATH + f.Paths[i].Name + EXT_JSON, JsonMapper.ToJson(Serialise.SerialisePath(f, i)));
                                    }
                                }
                                #endregion

                                if (eo.ExportToProject)
                                {
                                    Console.WriteLine("Emitting project file...");

                                    File.WriteAllText(od + f.General.Name + EXT_JSON, JsonMapper.ToJson(Serialise.SerialiseProject(f)));
                                }
                            }
                        }
                        break;
                }
            });
        }
    }
}
