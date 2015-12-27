using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Altar.NET
{
    using static SR;

    static class Program
    {
        readonly static string[] dirs =
        {
            "texture",
            "texpage",
            "sprite" ,
            "audio"  ,
            "sound"  ,
            "room"   ,
            "object" ,
            "bg"     ,
            "script" ,
            "code"   ,
            "font"   ,
            "path"
        };

        unsafe static void Main(string[] args)
        {
            var file = Path.GetFullPath(args.Length == 0 ? DATA_WIN : args[0]);

            if (!File.Exists(file))
                Console.WriteLine(ERR_FILE_NF_1 + file + ERR_FILE_NF_2);

            var cd = Path.GetFullPath(Environment.CurrentDirectory);
            Environment.CurrentDirectory = Path.GetDirectoryName(file);

            foreach (var s in dirs)
                if (!Directory.Exists(s))
                    Directory.CreateDirectory(s);

            using (var f = GMFile.GetFile(File.ReadAllBytes(file)))
            {
                var sb = new StringBuilder();


                //if (f.Audio->Count >= 0)
                //    return;

                #region strings
                var sep = Environment.NewLine; //Environment.NewLine + new string('-', 80) + Environment.NewLine;

                if (f.Strings->Count > 0)
                {
                    Console.Write("Fetching strings... ");

                    var strings = new string[(int)f.Strings->Count];

                    for (uint i = 0; i < f.Strings->Count; i++)
                        strings[i] = SectionReader.GetStringInfo(f, i);

                    File.WriteAllText(FILE_STR, String.Join(sep, strings));

                    Console.WriteLine(DONE);
                }
                #endregion

                #region textures
                if (f.Textures->Count > 0)
                {
                    Console.Write("Fetching textures... ");

                    for (uint i = 0; i < f.Textures->Count; i++)
                    {
                        var ti = SectionReader.GetTextureInfo(f, i);

                        File.WriteAllBytes(DIR_TEX + i + EXT_PNG, ti.PngData);
                    }

                    Console.WriteLine(DONE);
                }
                #endregion
                #region texture pages
                if (f.TexturePages->Count > 0)
                {
                    Console.Write("Fetching texture pages... ");

                    for (uint i = 0; i < f.TexturePages->Count; i++)
                    {
                        var tpi = SectionReader.GetTexPageInfo(f, i);

                        sb.Clear()
                            .Append("Position=").Append(tpi.Position).AppendLine()
                            .Append("Size=").Append(tpi.Size).AppendLine()
                            .Append("RenderOffset=").Append(tpi.RenderOffset).AppendLine()
                            .Append("SheetId=").Append(tpi.SpritesheetId).AppendLine();

                        File.WriteAllText(DIR_TXP + i + EXT_TXT, sb.ToString());
                    }

                    Console.WriteLine(DONE);
                }
                #endregion
                #region sprite
                if (f.Sprites->Count > 0)
                {
                    Console.Write("Fetching sprites... ");

                    for (uint i = 0; i < f.Sprites->Count; i++)
                    {
                        var si = SectionReader.GetSpriteInfo(f, i);

                        sb.Clear()
                            .Append("Size=").Append(si.Size).AppendLine()
                            .Append("TextureIndices=[").Append(String.Join(COMMA_S, si.TextureIndices)).Append(']').AppendLine();

                        File.WriteAllText(DIR_SPR + si.Name + EXT_TXT, sb.ToString());
                    }

                    Console.WriteLine(DONE);
                }
                #endregion

                #region sound
                if (f.Sounds->Count > 0)
                {
                    Console.Write("Fetching sounds... ");

                    for (uint i = 0; i < f.Sounds->Count; i++)
                    {
                        var si = SectionReader.GetSoundInfo(f, i);

                        sb.Clear()
                            .Append("Name=").AppendLine(si.Name)
                            .Append("Type=").AppendLine(si.Type)
                            .Append("File=").AppendLine(si.File)
                            .Append("Embedded=").Append(si.IsEmbedded).AppendLine()
                            .Append("AudioId=").Append(si.AudioId).AppendLine();

                        File.WriteAllText(DIR_SND + i + EXT_TXT, sb.ToString());
                    }

                    Console.WriteLine(DONE);
                }
                #endregion
                #region audio
                if (f.Audio->Count > 0)
                {
                    Console.Write("Fetching audio... ");

                    var ii = 0;
                    var infoTable =
                        Enumerable.Range(0, (int)f.Sounds->Count)
                            .Select(i => SectionReader.GetSoundInfo(f, (uint)i))
                            .Where(si => si.IsEmbedded)
                            .ToDictionary(_ => ii++, si => si);

                    for (uint i = 0; i < f.Audio->Count; i++)
                    {
                        var ai = SectionReader.GetAudioInfo(f, i);

                        File.WriteAllBytes(DIR_WAV + i + UNDERSC + infoTable[(int)i].Name + EXT_WAV, ai.Wave);
                    }

                    Console.WriteLine(DONE);
                }
                #endregion

                #region objects
                if (f.Objects->Count > 0)
                {
                    Console.Write("Fetching objects... ");

                    for (uint i = 0; i < f.Objects->Count; i++)
                    {
                        var oi = SectionReader.GetObjectInfo(f, i);

                        var text = sb.Clear().Append("SpriteIndex=").Append(oi.SpriteIndex).AppendLine().ToString();

                        File.WriteAllText(DIR_OBJ + oi.Name + EXT_BIN, text);
                    }

                    Console.WriteLine(DONE);
                }
                #endregion
                #region backgrounds
                if (f.Backgrounds->Count > 0)
                {
                    Console.Write("Fetching backgrounds... ");

                    for (uint i = 0; i < f.Backgrounds->Count; i++)
                    {
                        var bi = SectionReader.GetBgInfo(f, i);

                        File.WriteAllText(DIR_BG + bi.Name + EXT_TXT, "TexPageIndex=" + bi.TexPageIndex);
                    }

                    Console.WriteLine(DONE);
                }
                #endregion
                #region rooms
                if (f.Rooms->Count > 0)
                {
                    Console.Write("Fetching rooms... ");

                    for (uint i = 0; i < f.Rooms->Count; i++)
                    {
                        var ri = SectionReader.GetRoomInfo(f, i);

                        var t = "Size=" + ri.Size + "\nColour=" + ri.Colour.ToHexString() + "\0";

                        //TODO: serialize
                        //File.WriteAllBytes(DIR_ROOM + ri.Name + EXT_BIN, Encoding.ASCII.GetBytes(t).Concat(ri.Data).ToArray());
                    }

                    Console.WriteLine(DONE);
                }
                #endregion

                var vars = SectionReader.GetRefDefs(f, f.Variables);
                var fns  = SectionReader.GetRefDefs(f, f.Functions);

                #region variables
                if (vars.Length > 0)
                {
                    Console.Write("Fetching variables... ");

                    sb.Clear();

                    for (int i = 0; i < vars.Length; i++)
                    {
                        var v = vars[i];

                        sb.AppendLine(v.Name);
                    }

                    File.WriteAllText(FILE_VAR, sb.ToString());

                    Console.WriteLine(DONE);
                }
                #endregion
                #region functions
                if (fns.Length > 0)
                {
                    Console.Write("Fetching functions... ");

                    sb.Clear();

                    for (int i = 0; i < fns.Length; i++)
                    {
                        var fn = fns[i];

                        sb.AppendLine(fn.Name);
                    }

                    File.WriteAllText(FILE_FNS, sb.ToString());

                    Console.WriteLine(DONE);
                }
                #endregion

                #region script
                if (f.Scripts->Count > 0)
                {
                    Console.Write("Fetching scripts... ");

                    for (uint i = 0; i < f.Scripts->Count; i++)
                    {
                        var si = SectionReader.GetScriptInfo(f, i);

                        sb.Clear().Append("CodeId=").Append(si.CodeId).AppendLine();

                        File.WriteAllText(DIR_SCR + i + UNDERSC + si.Name + EXT_TXT, sb.ToString());
                    }

                    Console.WriteLine(DONE);
                }
                #endregion
                #region code
                if (f.Code->Count > 0)
                {
                    Console.Write("Fetching code... ");

                    var varAccs = Disassembler.GetReferenceTable(f, vars);
                    var  fnAccs = Disassembler.GetReferenceTable(f, fns );

                    //File.WriteAllText("vars.txt", String.Join(Environment.NewLine, varAccs.OrderBy(kvp => (long)kvp.Key).Select(kvp => ((ulong)kvp.Key - (ulong)f.RawData.IPtr).ToString("X8") + "->" + vars[kvp.Value].Name)));
                    //File.WriteAllText("funs.txt", String.Join(Environment.NewLine,  fnAccs.OrderBy(kvp => (long)kvp.Key).Select(kvp => ((ulong)kvp.Key - (ulong)f.RawData.IPtr).ToString("X8") + "->" +  fns[kvp.Value].Name)));

                    for (uint i = 0; i < f.Code->Count; i++)
                    {
                        var ci = Disassembler.DisassembleCode(f, i);
                        var s  = Disassembler.DisplayInstructions(f, varAccs, fnAccs, ci.Instructions);

                        File.WriteAllText(DIR_CODE + i + UNDERSC + ci.Name + EXT_GML_ASM, s);
                    }

                    Console.WriteLine(DONE);
                }
                #endregion

                #region other
                {
                    Console.Write("Fetching other chunks...");

                    var fonts = SectionReader.ListToByteArrays(f, f.Fonts, 0x7AC);
                    for (int i = 0; i < fonts.Length; i++)
                        File.WriteAllBytes(DIR_FNT  + i + EXT_BIN, fonts[i]);

                    var paths = SectionReader.ListToByteArrays(f, f.Paths, -0x38);
                    for (int i = 0; i < paths.Length; i++)
                        File.WriteAllBytes(DIR_PATH + i + EXT_BIN, paths[i]);

                    Console.WriteLine(DONE);
                }
                #endregion
            }

            Environment.CurrentDirectory = cd;
        }
    }
}
