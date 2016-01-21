using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Altar
{
    // http://pastebin.com/9t783UNE

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

        [STAThread]
        unsafe static void Main(string[] args)
        {
            var file = Path.GetFullPath(args.Length == 0 ? DATA_WIN : args[0]);

            if (Directory.Exists(file) && !File.Exists(file))
                file += Path.DirectorySeparatorChar + DATA_WIN;

            if (!File.Exists(file))
                Console.WriteLine(ERR_FILE_NF_1 + file + ERR_FILE_NF_2);

            var cd = Path.GetFullPath(Environment.CurrentDirectory);
            Environment.CurrentDirectory = Path.GetDirectoryName(file);

            foreach (var s in dirs)
                if (!Directory.Exists(s))
                    Directory.CreateDirectory(s);

            Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture =
                CultureInfo.InvariantCulture;

            using (var f = GMFile.GetFile(File.ReadAllBytes(file)))
            {
                var sb = new StringBuilder();

                #region init stuff
                //TODO: serialize
                var gen8 = SectionReader.GetGeneralInfo(f);
                var optn = SectionReader.GetOptionInfo(f);

                var vars = gen8.IsNewBCVersion ? SectionReader.GetRefDefs(f, f.Variables) : SectionReader.GetRefDefsWithOthers(f, f.Variables);
                var fns  = gen8.IsNewBCVersion ? SectionReader.GetRefDefs(f, f.Functions) : SectionReader.GetRefDefsWithLength(f, f.Functions);

                var varAccs = Disassembler.GetReferenceTable(f, vars);
                var  fnAccs = Disassembler.GetReferenceTable(f, fns );

                var rdata = new RefData
                {
                    Variables = vars,
                    Functions = fns ,

                    VarAccessors  = varAccs,
                    FuncAccessors =  fnAccs
                };
                #endregion

                //var c__ = Disassembler.DisassembleCode(f, 1);
                ////var d = Decompiler.DecompileCode(f, rdata, c__);
                //var d = Disassembler.DisplayInstructions(f, rdata, c__);

                //System.Windows.Forms.Clipboard.SetText(d);

                //if (f.Audio->Count >= 0)
                //    return;

                //TODO: use an actual serialization lib or something

                //goto SKIP;

                #region general
                {
                    Console.Write("Reading header data... ");

                    var gi = SectionReader.GetGeneralInfo(f);

                    sb.Clear()

                        .Append("Name="       ).AppendLine(gi.Name         )
                        .Append("FileName="   ).AppendLine(gi.FileName     )
                        .Append("Config="     ).AppendLine(gi.Configuration)
                        .Append("DisplayName=").AppendLine(gi.DisplayName  )

                        .Append("Debug="     ).Append(gi.IsDebug        ).AppendLine()
                        .Append("BCVersion=" ).Append(gi.BytecodeVersion).AppendLine()
                        .Append("GameId="    ).Append(gi.GameId         ).AppendLine()
                        .Append("Version="   ).Append(gi.Version        ).AppendLine()
                        .Append("WindowSize=").Append(gi.WindowSize     ).AppendLine()

                        .Append("Timestamp=").AppendLine(gi.Timestamp.ToString(SHORT_L /* 's': sortable */))

                        .Append("LicenseMD5=[").Append(String.Join(COMMA_S, gi.LicenseMD5Hash)).AppendLine(C_BRACKET)
                        .Append("LicenseCRC=").Append(HEX_PRE).AppendLine(gi.LicenceCRC32.ToString(HEX_FM8))

                        .Append("WeirdNums=[").Append(String.Join(COMMA_S, gi.WeirdNumbers)).AppendLine(C_BRACKET);

                    File.WriteAllText("general.txt", sb.ToString());

                    Console.WriteLine(DONE);
                }
                #endregion
                #region options
                {
                    Console.Write("Reading option data... ");

                    var oi = SectionReader.GetOptionInfo(f);

                    sb.Clear().AppendLine("Constants=[");

                    foreach (var kvp in oi.Constants)
                        sb.Append(INDENT2).Append(kvp.Key).Append(EQ_S)
                            .Append(kvp.Value.Escape()).AppendLine(COMMA_S);

                    sb.AppendLine(C_BRACKET);

                    File.WriteAllText("option.txt", sb.ToString());

                    Console.WriteLine(DONE);
                }
                #endregion

                #region strings
                if (f.Strings->Count > 0)
                {
                    var sep = Environment.NewLine; //Environment.NewLine + new string('-', 80) + Environment.NewLine;

                    Console.Write("Reading strings... ");

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
                    Console.Write("Reading textures... ");

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
                    Console.Write("Reading texture pages (maps)... ");

                    for (uint i = 0; i < f.TexturePages->Count; i++)
                    {
                        var tpi = SectionReader.GetTexPageInfo(f, i);

                        sb.Clear()
                            .Append("Position="    ).Append(tpi.Position     ).AppendLine()
                            .Append("Size="        ).Append(tpi.Size         ).AppendLine()
                            .Append("RenderOffset=").Append(tpi.RenderOffset ).AppendLine()
                            .Append("BoundingBox=" ).Append(tpi.BoundingBox  ).AppendLine()
                            .Append("SheetId="     ).Append(tpi.SpritesheetId).AppendLine();

                        File.WriteAllText(DIR_TXP + i + EXT_TXT, sb.ToString());
                    }

                    Console.WriteLine(DONE);
                }
                #endregion
                #region sprite
                if (f.Sprites->Count > 0)
                {
                    Console.Write("Reading sprites... ");

                    for (uint i = 0; i < f.Sprites->Count; i++)
                    {
                        var si = SectionReader.GetSpriteInfo(f, i);

                        sb.Clear()
                            .Append("Size="    ).Append(si.Size    ).AppendLine()
                            .Append("Bounding=").Append(si.Bounding).AppendLine()
                            .Append("BBoxMode=").Append(si.BBoxMode).AppendLine()
                            .Append("SepMasks=").Append(si.SepMasks).AppendLine()
                            .Append("Origin="  ).Append(si.Origin  ).AppendLine()

                            .Append("TextureIndices=[").Append(String.Join(COMMA_S, si.TextureIndices)).Append(']').AppendLine();

                        File.WriteAllText(DIR_SPR + si.Name + EXT_TXT, sb.ToString());
                    }

                    Console.WriteLine(DONE);
                }
                #endregion

                #region sound
                if (f.Sounds->Count > 0)
                {
                    Console.Write("Reading sounds... ");

                    for (uint i = 0; i < f.Sounds->Count; i++)
                    {
                        var si = SectionReader.GetSoundInfo(f, i);

                        sb.Clear()
                            .Append("Type="      ).AppendLine(si.Type)
                            .Append("File="      ).AppendLine(si.File)
                            .Append("Embedded="  ).Append(si.IsEmbedded  ).AppendLine()
                            .Append("Compressed=").Append(si.IsCompressed).AppendLine()
                            .Append("AudioId="   ).Append(si.AudioId     ).AppendLine()
                            .Append("Volume="    ).Append(si.VolumeMod   ).AppendLine()
                            .Append("Pitch="     ).Append(si.PitchMod    ).AppendLine()
                            .Append("Pan="       ).Append(si.PanMod      ).AppendLine();

                        File.WriteAllText(DIR_SND + si.Name + EXT_TXT, sb.ToString());
                    }

                    Console.WriteLine(DONE);
                }
                #endregion
                #region audio
                if (f.Audio->Count > 0)
                {
                    Console.Write("Reading audio... ");

                    var sounds = Enumerable.Range(0, (int)f.Sounds->Count)
                                    .Select(i => SectionReader.GetSoundInfo(f, (uint)i));

                    var infoTable = new Dictionary<int, SoundInfo>();

                    foreach (var s in sounds)
                        if ((s.IsEmbedded || s.IsCompressed) && s.AudioId != -1)
                            infoTable[s.AudioId] = s;

                    for (int i = 0; i < f.Audio->Count; i++)
                    {
                        var ai = SectionReader.GetAudioInfo(f, (uint)i);

                        File.WriteAllBytes(DIR_WAV + infoTable[i].Name + EXT_WAV, ai.Wave);
                    }

                    Console.WriteLine(DONE);
                }
                #endregion

                #region objects
                if (f.Objects->Count > 0)
                {
                    Console.Write("Reading objects... ");

                    for (uint i = 0; i < f.Objects->Count; i++)
                    {
                        var oi = SectionReader.GetObjectInfo(f, i);

                        sb.Clear()
                            .Append("SpriteIndex=").Append(oi.SpriteIndex ).AppendLine()
                            .Append("Visible="    ).Append(oi.IsVisible   ).AppendLine()
                            .Append("Solid="      ).Append(oi.IsSolid     ).AppendLine()
                            .Append("Depth="      ).Append(oi.Depth       ).AppendLine()
                            .Append("Persistent=" ).Append(oi.IsPersistent).AppendLine()

                            .Append("ParentId=" ).AppendLine(oi.ParentId ?.ToString() ?? String.Empty)
                            .Append("TexMaskId=").AppendLine(oi.TexMaskId?.ToString() ?? String.Empty)

                            .AppendLine("Physics={")
                            .Append(INDENT2).Append("Density="       ).Append(oi.Physics.Density       ).AppendLine()
                            .Append(INDENT2).Append("Restitution="   ).Append(oi.Physics.Restitution   ).AppendLine()
                            .Append(INDENT2).Append("Group="         ).Append(oi.Physics.Group         ).AppendLine()
                            .Append(INDENT2).Append("LinearDamping=" ).Append(oi.Physics.LinearDamping ).AppendLine()
                            .Append(INDENT2).Append("AngularDamping=").Append(oi.Physics.AngularDamping).AppendLine()
                            .Append(INDENT2).Append("Unknown0="      ).Append(oi.Physics.Unknown0      ).AppendLine()
                            .Append(INDENT2).Append("Friction="      ).Append(oi.Physics.Friction      ).AppendLine()
                            .Append(INDENT2).Append("Unknown1="      ).Append(oi.Physics.Unknown1      ).AppendLine()
                            .Append(INDENT2).Append("Kinematic="     ).Append(oi.Physics.Kinematic     ).AppendLine()
                            .AppendLine(C_BRACE)

                            .Append("OtherFloats=[").Append(String.Join(COMMA_S, oi.OtherFloats)).AppendLine(C_BRACKET)
                            .AppendLine("ShapePoints=[");

                        foreach (var p in oi.ShapePoints)
                            sb.Append(INDENT2).Append(p.ToString()).AppendLine(COMMA_S);
                        sb.AppendLine(C_BRACKET);

                        File.WriteAllText(DIR_OBJ + oi.Name + EXT_TXT, sb.ToString());
                    }

                    Console.WriteLine(DONE);
                }
                #endregion
                #region backgrounds
                if (f.Backgrounds->Count > 0)
                {
                    Console.Write("Reading backgrounds... ");

                    for (uint i = 0; i < f.Backgrounds->Count; i++)
                    {
                        var bi = SectionReader.GetBgInfo(f, i);

                        File.WriteAllText(DIR_BG + bi.Name + EXT_TXT, "TPagIndex=" + bi.TexPageIndex);
                    }

                    Console.WriteLine(DONE);
                }
                #endregion
                #region rooms
                if (f.Rooms->Count > 0)
                {
                    Console.Write("Reading rooms... ");

                    for (uint i = 0; i < f.Rooms->Count; i++)
                    {
                        var ri = SectionReader.GetRoomInfo(f, i);

                        var t = "Size=" + ri.Size + Environment.NewLine + "Colour=" + ri.Colour.ToHexString() + "\0";

                        sb.Clear()
                            .Append("Caption=").AppendLine(ri.Caption)

                            .Append("Size="       ).Append(ri.Size        ).AppendLine()
                            .Append("Speed="      ).Append(ri.Speed       ).AppendLine()
                            .Append("Persist="    ).Append(ri.IsPersistent).AppendLine()
                            .Append("Colour="     ).Append(ri.Colour      ).AppendLine()
                            .Append("EnableViews=").Append(ri.EnableViews ).AppendLine()
                            .Append("ShowColour=" ).Append(ri.ShowColour  ).AppendLine()

                            .Append("World="         ).Append(ri.World         ).AppendLine()
                            .Append("Bounding="      ).Append(ri.Bounding      ).AppendLine()
                            .Append("Gravity="       ).Append(ri.Gravity       ).AppendLine()
                            .Append("MetresPerPixel=").Append(ri.MetresPerPixel).AppendLine();

                        //TODO: serialize arrays
                        File.WriteAllText(DIR_ROOM + ri.Name + EXT_TXT, sb.ToString());
                    }

                    Console.WriteLine(DONE);
                }
                #endregion

                #region variables
                if (vars.Length > 0)
                {
                    Console.Write("Reading variables... ");

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
                    Console.Write("Reading functions... ");

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

            //SKIP:

                #region script
                if (f.Scripts->Count > 0)
                {
                    Console.Write("Reading scripts... ");

                    for (uint i = 0; i < f.Scripts->Count; i++)
                    {
                        var si = SectionReader.GetScriptInfo(f, i);

                        sb.Clear().Append("CodeId=").Append(si.CodeId).AppendLine();

                        File.WriteAllText(DIR_SCR + si.Name + EXT_TXT, sb.ToString());
                    }

                    Console.WriteLine(DONE);
                }
                #endregion
                #region code
                if (f.Code->Count > 0)
                {
                    Console.Write("Reading code... ");

                    for (uint i = 0; i < f.Code->Count; i++)
                    {
                        Console.WriteLine(i + "/" + f.Code->Count);

                        var ci = Disassembler.DisassembleCode(f, i);
                        var s  = Disassembler.DisplayInstructions/*Decompiler.DecompileCode*/(f, rdata, ci);

                        File.WriteAllText(DIR_CODE + ci.Name + EXT_GML_ASM, s);
                    }

                    Console.WriteLine(DONE);
                }
                #endregion

                #region fonts
                if (f.Fonts->Count > 0)
                {
                    Console.Write("Reading fonts... ");

                    for (uint i = 0; i < f.Fonts->Count; i++)
                    {
                        var fi = SectionReader.GetFontInfo(f, i);

                        sb.Clear()
                            .Append("SysName=").AppendLine(fi.SystemName)
                            .Append("EmSize=" ).Append(fi.EmSize  ).AppendLine()
                            .Append("Bold="   ).Append(fi.IsBold  ).AppendLine()
                            .Append("Italic=" ).Append(fi.IsItalic).AppendLine()

                            .Append("AntiAlias=").Append(fi.AntiAliasing).AppendLine()
                            .Append("Charset="  ).Append(fi.Charset     ).AppendLine()

                            .Append("TexPagId=").Append(fi.TexPagId).AppendLine()
                            .Append("Scale="   ).Append(fi.Scale   ).AppendLine()

                            .Append("Charset=").AppendLine(O_BRACKET);

                        foreach (var c in fi.Characters)
                        {
                            sb.Append(INDENT2).AppendLine(O_BRACE);

                            sb.Append(INDENT4).Append("Char='");

                            switch (c.Character)
                            {
                                case (char)0x7F:
                                    sb.Append(DEL_CHAR);
                                    break;
                                case '\n':
                                    sb.Append(LF_CHAR);
                                    break;
                                case '\r':
                                    sb.Append(CR_CHAR);
                                    break;
                                case '\t':
                                    sb.Append(TAB_CHAR);
                                    break;
                                case '\b':
                                    sb.Append(BELL_CHAR);
                                    break;
                                case '\0':
                                    sb.Append(NUL_CHAR);
                                    break;
                                default:
                                    sb.Append(c.Character);
                                    break;
                            }

                            sb.Append('\'').AppendLine()
                                .Append(INDENT4).Append("Frame=" ).Append(c.TPagFrame).AppendLine()
                                .Append(INDENT4).Append("Shift=" ).Append(c.Shift    ).AppendLine()
                                .Append(INDENT4).Append("Offset=").Append(c.Offset   ).AppendLine();

                            sb.Append(INDENT2).Append(C_BRACE).AppendLine(COMMA_S);
                        }
                        sb.AppendLine(C_BRACKET);

                        File.WriteAllText(DIR_FNT + fi.CodeName + EXT_TXT, sb.ToString());
                    }

                    Console.WriteLine(DONE);
                }
                #endregion
                #region paths
                if (f.Paths->Count > 0)
                {
                    Console.Write("Reading paths... ");

                    for (uint i = 0; i < f.Paths->Count; i++)
                    {
                        var pi = SectionReader.GetPathInfo(f, i);

                        sb.Clear()
                            .Append("Smooth="   ).Append(pi.IsSmooth ).AppendLine()
                            .Append("Closed="   ).Append(pi.IsClosed ).AppendLine()
                            .Append("Precision=").Append(pi.Precision).AppendLine()
                            .AppendLine("Points=[");

                        foreach (var p in pi.Points)
                        {
                            sb  .Append(INDENT2).AppendLine(O_BRACE)
                                .Append(INDENT4).Append("Position=").Append(p.Position).AppendLine()
                                .Append(INDENT4).Append("Speed="   ).Append(p.Speed   ).AppendLine()
                                .Append(INDENT2).Append(C_BRACE).AppendLine(COMMA_S);
                        }

                        sb.AppendLine(C_BRACKET);

                        File.WriteAllText(DIR_PATH + pi.Name + EXT_TXT, sb.ToString());
                    }

                    Console.WriteLine(DONE);
                }
                #endregion
            }

            Environment.CurrentDirectory = cd;
        }
    }
}
