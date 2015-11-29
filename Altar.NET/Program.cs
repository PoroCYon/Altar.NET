using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Altar.NET
{
    static class Program
    {
        readonly static string[] dirs =
        {
            "texture",
            "audio"  ,
            "room"   ,
            "object" ,
            "code"   ,
            "bg"
        };

        unsafe static void Main(string[] args)
        {
            var file = Path.GetFullPath(args.Length == 0 ? "data.win" : args[0]);

            if (!File.Exists(file))
                Console.WriteLine("File \"" + file + "\" not found.");

            var cd = Path.GetFullPath(Environment.CurrentDirectory);
            Environment.CurrentDirectory = Path.GetDirectoryName(file);

            foreach (var s in dirs)
                if (!Directory.Exists(s))
                    Directory.CreateDirectory(s);

            var f = *GMFile.GetFile(File.ReadAllBytes(file));
            try
            {
                var sep = Environment.NewLine + new string('-', 80) + Environment.NewLine;

                if (f.Audio->Count == 0 || f.Audio->Count > 0)
                    return;

                var r = Disassembler.DisplayInstructions(ref f, Disassembler.DisassembleCode(ref f, 1));
                File.WriteAllText("code-1-" + SectionReader.GetCodeInfo(ref f, 1).Name + ".gml.asm", r);

                if (f.Strings->Count > 0)
                {
                    Console.Write("Fetching strings... ");

                    var strings = new string[(int)f.Strings->Count];

                    for (uint i = 0; i < f.Strings->Count; i++)
                        strings[i] = SectionReader.GetStringInfo(ref f, i);

                    File.WriteAllText("strings.txt", String.Join(sep, strings));

                    Console.WriteLine("Done.");
                }

                if (f.Textures->Count > 0)
                {
                    Console.Write("Fetching textures... ");

                    for (uint i = 0; i < f.Textures->Count; i++)
                    {
                        var ti = SectionReader.GetTextureInfo(ref f, i);

                        File.WriteAllBytes("texture/" + i + ".png", ti.DataInfo);
                    }

                    Console.WriteLine("Done.");
                }

                if (f.Audio->Count > 0)
                {
                    Console.Write("Fetching audio... ");

                    for (uint i = 0; i < f.Audio->Count; i++)
                    {
                        var ai = SectionReader.GetAudioInfo(ref f, i);

                        File.WriteAllBytes("audio/" + i + ".wav", ai.RIFF);
                    }

                    Console.WriteLine("Done.");
                }

                if (f.Rooms->Count > 0)
                {
                    Console.Write("Fetching rooms... ");

                    for (uint i = 0; i < f.Rooms->Count; i++)
                    {
                        var ri = SectionReader.GetRoomInfo(ref f, i);

                        var t = "Size=" + ri.Size + "\nColour=" + ri.Colour.ToHexString() + "\0";

                        File.WriteAllBytes("room/" + ri.Name + ".bin", Encoding.ASCII.GetBytes(t).Concat(ri.Data).ToArray());
                    }

                    Console.WriteLine("Done.");
                }

                if (f.Objects->Count > 0)
                {
                    Console.Write("Fetching objects... ");

                    for (uint i = 0; i < f.Objects->Count; i++)
                    {
                        var oi = SectionReader.GetObjectInfo(ref f, i);

                        File.WriteAllBytes("object/" + oi.Name + ".bin", oi.Data);
                    }

                    Console.WriteLine("Done.");
                }

                if (f.Backgrounds->Count > 0)
                {
                    Console.Write("Fetching backgrounds... ");

                    for (uint i = 0; i < f.Backgrounds->Count; i++)
                    {
                        var bi = SectionReader.GetBgInfo(ref f, i);

                        File.WriteAllText("bg/" + bi.Name + ".txt", "TextureAddress=0x" + bi.TextureAddress.ToString("X8"));
                    }

                    Console.WriteLine("Done.");
                }

                {
                    var vars = SectionReader.GetRefDefs(ref f, f.Variables);

                    if (vars.Length > 0)
                    {
                        Console.Write("Fetching variables... ");

                        var sb = new StringBuilder();

                        for (int i = 0; i < vars.Length; i++)
                        {
                            var v = vars[i];

                            sb.Append('[').Append(v.Name).Append(']').AppendLine()
                                .Append("Occurrences=").Append(v.Occurrences).AppendLine()
                                .Append("FirstAddress=0x").Append(v.FirstAddress.ToString("X8")).AppendLine();
                        }

                        File.WriteAllText("vars.ini", sb.ToString());

                        Console.WriteLine("Done.");
                    }
                }
                {
                    var fns = SectionReader.GetRefDefs(ref f, f.Functions);

                    if (fns.Length > 0)
                    {
                        Console.Write("Fetching functions... ");

                        var sb = new StringBuilder();

                        for (int i = 0; i < fns.Length; i++)
                        {
                            var fn = fns[i];

                            sb.Append('[').Append(fn.Name).Append(']').AppendLine()
                                .Append("Occurrences=").Append(fn.Occurrences).AppendLine()
                                .Append("FirstAddress=0x").Append(fn.FirstAddress.ToString("X8")).AppendLine();
                        }

                        File.WriteAllText("funcs.ini", sb.ToString());

                        Console.WriteLine("Done.");
                    }
                }

                if (f.Code->Count > 0)
                {
                    Console.Write("Fetching code... ");

                    for (uint i = 0; i < f.Code->Count; i++)
                    {
                        var ci = SectionReader.GetCodeInfo(ref f, i);
                        var s = Disassembler.DisplayInstructions(ref f, Disassembler.DisassembleCode(ref f, i));

                        File.WriteAllText("code/" + ci.Name + ".gml.asm", s);
                    }

                    Console.WriteLine("Done.");
                }
            }
            finally
            {
                f.Dispose();
            }

            Environment.CurrentDirectory = cd;
        }
    }
}
