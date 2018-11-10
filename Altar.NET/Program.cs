using Altar.Decomp;
using Altar.Recomp;
using Altar.Repack;
using Altar.Unpack;
using CommandLine;
using LitJson;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

using static Altar.SR;

namespace Altar
{
    // http://pastebin.com/9t783UNE

    using CLParser = CommandLine.Parser;

    unsafe static class Program
    {
        static bool quiet, nopp;
        static ExportOptions eos;

        static void Write    (string s) { if (!eos.Quiet) Console.Write    (s); }
        static void WriteLine(string s) { if (!eos.Quiet) Console.WriteLine(s); }
        static void GetCurPos(out int l, out int t)
        {
            if (nopp)
            {
                l = t = 0;
                return;
            }

            l = Console.CursorLeft;
            t = Console.CursorTop ;
        }
        static void SetCurPos(int l, int t)
        {
            if (!nopp)
                Console.SetCursorPosition(l, t);
        }
        static void SetCAndWr(int l, int t, string s)
        {
            if (!quiet && !nopp)
            {
                Console.SetCursorPosition(l, t);
                Console.Write(s);
            }
        }
        static void WrAndGetC(string s, out int l, out int t)
        {
            if (!quiet)
            {
                Console.Write(s);

                if (nopp)
                {
                    l = t = 0;
                    return;
                }

                l = Console.CursorLeft;
                t = Console.CursorTop ;
            }
            else l = t = 0;
        }

        static void Export(ExportOptions eo)
        {
            var file = Path.GetFullPath(String.IsNullOrEmpty(eo.File) ? DATA_WIN : eo.File);

            try // see #17
            {
                int i = Console.CursorLeft;
                i = i-- + 1;
            }
            catch
            {
                eo.NoPrecProg = true;
            }

            if (Directory.Exists(file) && !File.Exists(file))
                file += Path.DirectorySeparatorChar + DATA_WIN;
            if (!File.Exists(file))
                throw new ParserException("File \"" + file + "\" not found.");

            var od = (String.IsNullOrEmpty(eo.OutputDirectory) ? Path.GetDirectoryName(file) : eo.OutputDirectory) + Path.DirectorySeparatorChar;

            if (!Directory.Exists(od))
            {
                Directory.CreateDirectory(od);
            }

            using (var f = GMFile.GetFile(file))
            {
                #region defaults
                if (!(eo.Disassemble || eo.String  || eo.Variables || eo.Functions
                        || eo.Audio  || eo.Background || eo.Decompile || eo.Font || eo.General
                        || eo.Object || eo.Options || eo.Path || eo.Room || eo.Script
                        || eo.Sound  || eo.Sprite  || eo.Texture || eo.TPag || eo.AudioGroups
                        || eo.Shader || eo.ExportToProject || eo.Any || eo.DumpUnknownChunks
                        || eo.DumpEmptyChunks || eo.DumpAllChunks))
                    eo.Any = true;

                if (eo.ExportToProject)
                {
                    eo.Disassemble = eo.String = eo.Variables = eo.Functions
                        = eo.Audio = eo.Background = eo.Font = eo.General
                        = eo.Object = eo.Options = eo.Path = eo.Room = eo.Script
                        = eo.Sound = eo.Sprite = eo.Texture = eo.TPag = eo.AudioGroups
                        = eo.Shader = eo.DumpUnknownChunks = true;
                }
                if (eo.Any)
                {
                    eo.Disassemble = false;

                    eo.Audio = eo.Background = eo.Decompile   = eo.Font = eo.General
                        = eo.Object = eo.Options = eo.Path    = eo.Room = eo.Script
                        = eo.Sound  = eo.Sprite  = eo.Texture = eo.TPag
                        = eo.String = eo.Variables = eo.Functions = eo.DumpUnknownChunks
                        = eo.AudioGroups = eo.Shader = true;
                }
                #endregion

                eos = eo;
                quiet=eo.Quiet;
                nopp=eo.NoPrecProg;

                // ---
                #region GEN8
                if (eo.General && f.Content.General != null)
                {
                    WriteLine("Exporting manifest file...");

                    File.WriteAllText(od + "general.json", JsonMapper.ToJson(Serialize.SerializeGeneral(f.General)));
                }
                #endregion
                #region OPTN
                if (eo.Options && f.Content.Options != null)
                {
                    WriteLine("Exporting options...");

                    File.WriteAllText(od + "options.json", JsonMapper.ToJson(Serialize.SerializeOptions(f.Options)));
                }
                #endregion

                #region STRG
                if (eo.String && f.Strings != null)
                {
                    WriteLine("Dumping strings...");

                    File.WriteAllText(od + "strings.json", JsonMapper.ToJson(Serialize.SerializeStrings(f)));
                }
                #endregion
                #region VARI
                if (eo.Variables && f.RefData.Variables != null)
                {
                    WriteLine("Dumping variables...");

                    File.WriteAllText(od + "variables.json", JsonMapper.ToJson(Serialize.SerializeVars(f)));
                }
                #endregion
                #region FUNC
                if (eo.Functions && f.RefData.Functions != null)
                {
                    WriteLine("Dumping functions...");

                    File.WriteAllText(od + "functions.json", JsonMapper.ToJson(Serialize.SerializeFuncs(f)));
                }
                #endregion
                int cl = 0, ct = 0;
                #region AGRP
                if (eo.AudioGroups && f.AudioGroups != null)
                {
                    WriteLine("Dumping audio groups...");

                    File.WriteAllText(od+"audiogroups.json", JsonMapper.ToJson(Serialize.SerializeAudioGroups(f)));
                }
                if (eo.DetachedAgrp && f.AudioGroups != null)
                {
                    WriteLine("Dumping audio from detached audio groups...");

                    for (int i = 1; i < f.AudioGroups.Length; ++i)
                    {
                        WrAndGetC(DASH_ + f.AudioGroups[i] + O_PAREN + i +
                                SLASH + (f.AudioGroups.Length - 1) + C_PAREN + SPACE_S,
                                out cl, out ct);

                        var agrpfn = Path.GetDirectoryName(file) + Path.DirectorySeparatorChar
                            + AGRPF + i + D_DAT;
                        if (!File.Exists(agrpfn))
                        {
                            Console.Error.WriteLine("Eep: file '" + agrpfn + "' doesn't exist, skipping...");
                            continue;
                        }

                        var infoTable = new Dictionary<int, SoundInfo>();

                        for (uint iii = 0; iii < f.Sound.Length; ++iii)
                        {
                            var s = f.Sound[iii];
                            if ((s.IsEmbedded || s.IsCompressed) && s.AudioID != -1
                                    && s.GroupID == i)
                                infoTable[s.AudioID] = s;
                        }

                        var odgrp = od + DIR_AGRP + f.AudioGroups[i];
                        if (!Directory.Exists(odgrp))
                            Directory.CreateDirectory(odgrp);

                        using (var af = GMFile.GetFile(agrpfn))
                        {
                            for (int j = 0; j < af.Audio.Length; ++j)
                            {
                                SetCAndWr(cl, ct, O_PAREN + (j + 1) + SLASH +
                                        (af.Audio.Length - 1) + C_PAREN);
                                File.WriteAllBytes(odgrp + Path.DirectorySeparatorChar
                                        + infoTable[j].Name + SR.EXT_WAV, af.Audio[j].Wave);
                            }
                        }
                        Console.WriteLine();
                    }
                    f.AudioGroups.Clear();
                }
                #endregion

                #region TXTR
                if (eo.Texture && f.Textures != null)
                {
                    WrAndGetC("Exporting texture sheets... ", out cl, out ct);

                    if (!Directory.Exists(od + DIR_TEX))
                        Directory.CreateDirectory(od + DIR_TEX);

                    for (int i = 0; i < f.Textures.Length; i++)
                    {
                        if (f.Textures[i].PngData == null)
                            continue;

                        SetCAndWr(cl, ct, O_PAREN + (i + 1) + SLASH + f.Textures.Length + C_PAREN);

                        File.WriteAllBytes(od + DIR_TEX + i + EXT_PNG, f.Textures[i].PngData);
                    }
                    Console.WriteLine();
                    f.Textures.Clear();
                }
                #endregion
                #region AUDO
                if (eo.Audio && f.Audio != null)
                {
                    WrAndGetC("Exporting audio files... ", out cl, out ct);

                    var infoTable = new Dictionary<int, SoundInfo>();

                    foreach (var s in f.Sound)
                        if ((s.IsEmbedded || s.IsCompressed) && s.AudioID != -1
                                && s.GroupID == 0) // not from audiogroup$n.dat
                            infoTable[s.AudioID] = s;

                    if (!Directory.Exists(od + DIR_WAV))
                        Directory.CreateDirectory(od + DIR_WAV);

                    for (int i = 0; i < f.Audio.Length; i++)
                    {
                        SetCAndWr(cl, ct, O_PAREN + (i + 1) + SLASH + f.Audio.Length + C_PAREN);

                        File.WriteAllBytes(od + DIR_WAV + infoTable[i].Name + SR.EXT_WAV, f.Audio[i].Wave);
                    }
                    Console.WriteLine();
                    f.Audio.Clear();
                }
                #endregion
                #region CODE
                if (eo.Decompile && f.Code != null)
                {
                    WrAndGetC("Decompiling code... ", out cl, out ct);

                    if (!Directory.Exists(od + DIR_CODE))
                        Directory.CreateDirectory(od + DIR_CODE);

                    for (int i = 0; i < f.Code.Length; i++)
                    {
                        SetCAndWr(cl, ct, O_PAREN + (i + 1) + SLASH + f.Code.Length + C_PAREN);

                        try
                        {
                            File.WriteAllText(od + DIR_CODE + f.Code[i].Name + EXT_GML_LSP, Decompiler.DecompileCode(f, i, eo.AbsoluteAddresses));
                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine($"Error: Failed to decompile {f.Code[i].Name}, ignoring...");
#if DEBUG
                            Console.Error.WriteLine(e);
#endif
                        }
                    }
                    Console.WriteLine();
                }
                if (eo.Disassemble && f.Code != null)
                {
                    WrAndGetC("Disassembling bytecode... ", out cl, out ct);

                    if (!Directory.Exists(od + DIR_CODE))
                        Directory.CreateDirectory(od + DIR_CODE);

                    for (int i = 0; i < f.Code.Length; i++)
                    {
                        SetCAndWr(cl, ct, O_PAREN + (i + 1) + SLASH + f.Code.Length + C_PAREN);

                        File.WriteAllText(od + DIR_CODE + f.Code[i].Name + EXT_GML_ASM, Disassembler.DisplayInstructions(f, i, eo.AbsoluteAddresses));

                        /*
                        // Dump binary code to separate files. Useful for debugging de-/re-assembly/de-/re-compilation.
                        BinBuffer bb = new BinBuffer();
                        for (uint j = 0; j < f.Code[i].Instructions.Length; j++)
                        {
                            var instr = f.Code[i].Instructions[j];
                            var isize = DisasmExt.Size(instr, f.General.BytecodeVersion)*4;

                            bb.Write((IntPtr)instr, (int)isize);
                        }
                        bb.Position = 0;
                        File.WriteAllBytes(od + DIR_CODE + f.Code[i].Name + EXT_BIN, bb.ReadBytes(bb.Size));
                        */
                    }
                    Console.WriteLine();
                }
                if (f.Code != null) f.Code.Clear();
                #endregion

                #region SCPT
                if (eo.Script && f.Scripts != null && f.Code != null)
                {
                    WrAndGetC("Exporting scripts... ", out cl, out ct);

                    if (!Directory.Exists(od + DIR_SCR))
                        Directory.CreateDirectory(od + DIR_SCR);

                    for (int i = 0; i < f.Scripts.Length; i++)
                    {
                        SetCAndWr(cl, ct, O_PAREN + (i + 1) + SLASH + f.Scripts.Length + C_PAREN);

                        File.WriteAllText(od + DIR_SCR + f.Scripts[i].Name + EXT_JSON, JsonMapper.ToJson(Serialize.SerializeScript(f.Scripts[i], f.Code)));
                    }
                    Console.WriteLine();
                    f.Scripts.Clear();
                }
                #endregion
                #region TPAG
                if (eo.TPag && f.TexturePages != null)
                {
                    WrAndGetC("Exporting texture maps... ", out cl, out ct);

                    if (!Directory.Exists(od + DIR_TXP))
                        Directory.CreateDirectory(od + DIR_TXP);

                    for (int i = 0; i < f.TexturePages.Length; i++)
                    {
                        SetCAndWr(cl, ct, O_PAREN + (i + 1) + SLASH + f.TexturePages.Length + C_PAREN);

                        File.WriteAllText(od + DIR_TXP + i + EXT_JSON, JsonMapper.ToJson(Serialize.SerializeTPag(f.TexturePages[i])));
                    }
                    Console.WriteLine();
                    f.TexturePages.Clear();
                }
                #endregion
                #region SPRT
                if (eo.Sprite && f.Sprites != null)
                {
                    WrAndGetC("Exporting sprites... ", out cl, out ct);

                    if (!Directory.Exists(od + DIR_SPR))
                        Directory.CreateDirectory(od + DIR_SPR);

                    for (int i = 0; i < f.Sprites.Length; i++)
                    {
                        SetCAndWr(cl, ct, O_PAREN + (i + 1) + SLASH + f.Sprites.Length + C_PAREN);

                        File.WriteAllText(od + DIR_SPR + f.Sprites[i].Name + EXT_JSON, JsonMapper.ToJson(Serialize.SerializeSprite(f.Sprites[i])));
                    }
                    Console.WriteLine();
                    f.Sprites.Clear();
                }
                #endregion
                #region SOND
                if (eo.Sound && f.Sound != null)
                {
                    WrAndGetC("Exporting sounds... ", out cl, out ct);

                    if (!Directory.Exists(od + DIR_SND))
                        Directory.CreateDirectory(od + DIR_SND);

                    for (int i = 0; i < f.Sound.Length; i++)
                    {
                        SetCAndWr(cl, ct, O_PAREN + (i + 1) + SLASH + f.Sound.Length + C_PAREN);

                        File.WriteAllText(od + DIR_SND + f.Sound[i].Name + EXT_JSON, JsonMapper.ToJson(Serialize.SerializeSound(f.Sound[i])));
                    }
                    Console.WriteLine();
                    f.Sound.Clear();
                }
                #endregion

                #region OBJT
                if (eo.Object && f.Objects != null && f.Sprites != null)
                {
                    WrAndGetC("Exporting objects... ", out cl, out ct);

                    if (!Directory.Exists(od + DIR_OBJ))
                        Directory.CreateDirectory(od + DIR_OBJ);

                    for (int i = 0; i < f.Objects.Length; i++)
                    {
                        SetCAndWr(cl, ct, O_PAREN + (i + 1) + SLASH + f.Objects.Length + C_PAREN);

                        File.WriteAllText(od + DIR_OBJ + f.Objects[i].Name + EXT_JSON, JsonMapper.ToJson(Serialize.SerializeObj(f.Objects[i], f.Sprites, f.Objects)));
                    }
                    Console.WriteLine();
                }
                #endregion
                #region BGND
                if (eo.Background && f.Backgrounds != null)
                {
                    WrAndGetC("Exporting backgrounds... ", out cl, out ct);

                    if (!Directory.Exists(od + DIR_BG))
                        Directory.CreateDirectory(od + DIR_BG);

                    for (int i = 0; i < f.Backgrounds.Length; i++)
                    {
                        SetCAndWr(cl, ct, O_PAREN + (i + 1) + SLASH + f.Backgrounds.Length + C_PAREN);

                        File.WriteAllText(od + DIR_BG + f.Backgrounds[i].Name + EXT_JSON, JsonMapper.ToJson(Serialize.SerializeBg(f.Backgrounds[i])));
                    }
                    Console.WriteLine();
                }
                #endregion
                #region ROOM
                if (eo.Room && f.Rooms != null)
                {
                    WrAndGetC("Exporting rooms... ", out cl, out ct);

                    if (!Directory.Exists(od + DIR_ROOM))
                        Directory.CreateDirectory(od + DIR_ROOM);

                    for (int i = 0; i < f.Rooms.Length; i++)
                    {
                        SetCAndWr(cl, ct, O_PAREN + (i + 1) + SLASH + f.Rooms.Length + C_PAREN);

                        File.WriteAllText(od + DIR_ROOM + f.Rooms[i].Name + EXT_JSON, JsonMapper.ToJson(Serialize.SerializeRoom(f.Rooms[i], f.Backgrounds, f.Objects)));
                    }
                    Console.WriteLine();
                }
                if (f.Backgrounds != null)f.Backgrounds.Clear();
                if (f.Objects != null) f.Objects.Clear();
                if (f.Rooms != null) f.Rooms.Clear();
                #endregion

                #region FONT
                if (eo.Font && f.Fonts != null)
                {
                    WrAndGetC("Exporting fonts... ", out cl, out ct);

                    if (!Directory.Exists(od + DIR_FNT))
                        Directory.CreateDirectory(od + DIR_FNT);

                    for (int i = 0; i < f.Fonts.Length; i++)
                    {
                        SetCAndWr(cl, ct, O_PAREN + (i + 1) + SLASH + f.Fonts.Length + C_PAREN);

                        File.WriteAllText(od + DIR_FNT + f.Fonts[i].CodeName + EXT_JSON, JsonMapper.ToJson(Serialize.SerializeFont(f.Fonts[i])));
                    }
                    Console.WriteLine();
                    f.Fonts.Clear();
                }
                #endregion
                #region PATH
                if (eo.Path && f.Paths != null)
                {
                    WrAndGetC("Exporting paths... ", out cl, out ct);

                    if (!Directory.Exists(od + DIR_PATH))
                        Directory.CreateDirectory(od + DIR_PATH);

                    for (int i = 0; i < f.Paths.Length; i++)
                    {
                        SetCAndWr(cl, ct, O_PAREN + (i + 1) + SLASH + f.Paths.Length + C_PAREN);

                        File.WriteAllText(od + DIR_PATH + f.Paths[i].Name + EXT_JSON, JsonMapper.ToJson(Serialize.SerializePath(f.Paths[i])));
                    }
                    Console.WriteLine();
                    f.Paths.Clear();
                }
                #endregion
                #region SHDR
                if (eo.Shader && f.Shaders != null)
                {
                    WrAndGetC("Exporting shaders... ", out cl, out ct);

                    if (!Directory.Exists(od + DIR_SHDR))
                        Directory.CreateDirectory(od + DIR_SHDR);

                    for (int i = 0; i < f.Shaders.Length; i++)
                    {
                        SetCAndWr(cl, ct, O_PAREN + (i + 1) + SLASH + f.Shaders.Length + C_PAREN);

                        File.WriteAllText(od + DIR_SHDR + f.Shaders[i].Name + EXT_JSON, JsonMapper.ToJson(Serialize.SerializeShader(f.Shaders[i])));
                    }
                    Console.WriteLine();
                }
                #endregion
                List<IntPtr> chunks = null;

                if (eo.DumpUnknownChunks || eo.DumpAllChunks)
                {
                    chunks = new List<IntPtr>(6);
                    Action<IntPtr> DumpUnk = _unk =>
                    {
                        var unk = (SectionUnknown*)_unk;

                        if (unk == null || (unk->IsEmpty() && !eo.DumpEmptyChunks))
                            return;

                        WriteLine($"Dumping {unk->Header.MagicString()} chunk...");

                        var len = unk->Header.Size;
                        byte[] buf = new byte[len];
                        uint* src = &unk->Unknown;

                        if (len != 0)
                            ILHacks.Cpblk<byte>((void*)src, buf, 0, (int)len);

                        File.WriteAllBytes(od + unk->Header.MagicString() + EXT_BIN, buf);
                    };

                    var c = f.Content;

                    if (eo.DumpAllChunks)
                        chunks.AddRange(c.Chunks.Values);

                    // TODO: how to filter out unknowns?

                    for (int i = 0; i < chunks.Count; i++)
                        DumpUnk(chunks[i]);
                }

                if (eo.ExportToProject)
                {
                    WriteLine("Emitting project file...");

                    File.WriteAllText(od + f.General.Name + EXT_JSON, JsonMapper.ToJson(Serialize.SerializeProject(f, eo, chunks)));
                }
            }
        }

        // EXTN: SectionCountOffset<Extension>
        // {
        //     STRG*
        //     STRG*
        //     STRG*
        //     CountOffsetList<T>
        //     {
        //         STRG*
        //     }
        //     STRG*
        //     STRG*
        //     int32 02 00 00 00
        //
        //     CountOffsetList<T>
        //     {
        //         // 01
        //         STRG** (1)
        //         int32 10 04 00 00 (offset?)
        //         STRG*  (1)
        //         int32 01 00 00 00
        //         int32 0C 00 00 00
        //         int32 02 00 00 00
        //
        //         // 02
        //         STRG*
        //         int32 00 00 00 00
        //         STRG*
        //         int32 02 00 00 00
        //         int32 0C 00 00 00
        //         int32 02 00 00 00
        //     }
        //
        //     STRG*
        //     int32 00 00 00 00
        //     int32[4] 66 82 4C A8  69 1E 7C 85  5B 70 AC 11  67 C0 D2 D5 // WTF? hash?
        // }

        static void Import(ImportOptions opt)
        {
            var file = Path.GetFullPath(opt.File);

            if (!File.Exists(file))
                throw new FileNotFoundException("Project file not found", file);

            var baseDir = Path.GetDirectoryName(file) + Path.DirectorySeparatorChar;

            JsonData projFile = JsonMapper.ToObject(File.OpenText(file));
            GMFile f = Deserialize.ReadFile(baseDir, projFile);

            File.WriteAllBytes(Path.GetFullPath(opt.OutputFile), WriteFile(baseDir, projFile, f));
        }

        public static byte[] WriteFile(string baseDir, JsonData projFile, GMFile f)
        {
            Console.WriteLine($"Preparing strings...");
            var stringsChunkBuilder = new StringsChunkBuilder();
            stringsChunkBuilder.AddStrings(f.Strings);

            var texpChunk = new BBData(new BinBuffer(), new int[0]);
            Console.WriteLine($"Preparing textures...");
            int[] texPagOffsets = SectionWriter.WriteTexturePages(texpChunk, f.TexturePages);

            var codeChunk = new BBData(new BinBuffer(), new int[0]);
            Console.WriteLine($"Preparing code...");
            var codeChunkStringOffsetOffsets = Assembler.WriteCodes(codeChunk, f, stringsChunkBuilder);

            var offsets = new int[0];
            BBData writer = new BBData(new BinBuffer(), offsets);
            writer.Buffer.Write(SectionHeaders.Form);
            writer.Buffer.Write(0);

            var stringOffsetOffsets = new List<int>();
            int stringsDataPosition = 0;
            var texpOffsetOffsets = new List<int>();
            int texpChunkPosition = 0;
            var codeOffsetOffsets = new List<int>();
            int codeChunkPosition = 0;

            foreach (SectionHeaders chunkId in f.ChunkOrder)
            {
                Console.WriteLine($"Writing {chunkId}...");
                BBData chunk = new BBData(new BinBuffer(), new int[0]);
                int[] chunkStringOffsetOffsets = null;
                int[] chunkTexpOffsetOffsets = null;
                int[] chunkCodeOffsetOffsets = null;
                switch (chunkId)
                {
                    case SectionHeaders.General:
                        chunkStringOffsetOffsets = SectionWriter.WriteGeneral(chunk, f.General, f.Rooms, stringsChunkBuilder);
                        break;
                    case SectionHeaders.Options:
                        chunkStringOffsetOffsets = SectionWriter.WriteOptions(chunk, f.Options, stringsChunkBuilder);
                        break;
                    case SectionHeaders.Sounds:
                        chunkStringOffsetOffsets = SectionWriter.WriteSounds(chunk, f.Sound, stringsChunkBuilder, f.AudioGroups);
                        break;
                    case SectionHeaders.AudioGroup:
                        chunkStringOffsetOffsets = SectionWriter.WriteAudioGroups(chunk, f.AudioGroups, stringsChunkBuilder);
                        break;
                    case SectionHeaders.Sprites:
                        SectionWriter.WriteSprites(chunk, f.Sprites, stringsChunkBuilder, texPagOffsets,
                            out chunkStringOffsetOffsets, out chunkTexpOffsetOffsets);
                        break;
                    case SectionHeaders.Backgrounds:
                        SectionWriter.WriteBackgrounds(chunk, f.Backgrounds, stringsChunkBuilder, texPagOffsets,
                            out chunkStringOffsetOffsets, out chunkTexpOffsetOffsets);
                        break;
                    case SectionHeaders.Paths:
                        chunkStringOffsetOffsets = SectionWriter.WritePaths(chunk, f.Paths, stringsChunkBuilder);
                        break;
                    case SectionHeaders.Scripts:
                        chunkStringOffsetOffsets = SectionWriter.WriteScripts(chunk, f.Scripts, stringsChunkBuilder);
                        break;
                    case SectionHeaders.Fonts:
                        SectionWriter.WriteFonts(chunk, f.Fonts, stringsChunkBuilder, texPagOffsets,
                            out chunkStringOffsetOffsets, out chunkTexpOffsetOffsets);
                        break;
                    case SectionHeaders.Objects:
                        chunkStringOffsetOffsets = SectionWriter.WriteObjects(chunk, f.Objects, stringsChunkBuilder);
                        break;
                    case SectionHeaders.Rooms:
                        chunkStringOffsetOffsets = SectionWriter.WriteRooms(chunk, f.Rooms, stringsChunkBuilder);
                        break;
                    case SectionHeaders.TexturePage:
                        chunk = texpChunk;
                        texpChunkPosition = writer.Buffer.Position + 8;
                        break;
                    case SectionHeaders.Code:
                        chunk = codeChunk;
                        chunkStringOffsetOffsets = codeChunkStringOffsetOffsets;
                        codeChunkPosition = writer.Buffer.Position + 8;
                        break;
                    case SectionHeaders.Variables:
                        if (f.VariableExtra != null)
                            foreach (var e in f.VariableExtra)
                                chunk.Buffer.Write(e);
                        SectionWriter.WriteRefDefs(chunk, f.RefData.Variables, stringsChunkBuilder, f.General.IsOldBCVersion, false,
                            out chunkStringOffsetOffsets, out chunkCodeOffsetOffsets);
                        break;
                    case SectionHeaders.Functions:
                        SectionWriter.WriteRefDefs(chunk, f.RefData.Functions, stringsChunkBuilder, f.General.IsOldBCVersion, true,
                            out chunkStringOffsetOffsets, out chunkCodeOffsetOffsets);
                        chunkStringOffsetOffsets = chunkStringOffsetOffsets.Concat(SectionWriter.WriteFunctionLocals(chunk, f.FunctionLocals, stringsChunkBuilder)).ToArray();
                        break;
                    case SectionHeaders.Strings:
                        var stringOffsets = stringsChunkBuilder.WriteStringsChunk(chunk);
                        stringsDataPosition = writer.Buffer.Position + stringOffsets[0] + 12;
                        // for Textures chunk up next
                        SectionWriter.Pad(chunk, 0x80, writer.Buffer.Position + 8);
                        break;
                    case SectionHeaders.Textures:
                        SectionWriter.WriteTextures(chunk, f.Textures);
                        break;
                    case SectionHeaders.Audio:
                        SectionWriter.WriteAudio(chunk, f.Audio, writer.Buffer.Position);
                        break;
                    case SectionHeaders.Shaders:
                        chunkStringOffsetOffsets = SectionWriter.WriteShaders(chunk, f.Shaders, stringsChunkBuilder);
                        break;
                    default:
                        var chunkName = chunkId.ToChunkName();
                        Console.Error.WriteLine($"Note: Don't know how to handle {chunkName}");
                        string chunkFile = null;
                        if (projFile.Has("chunks") && projFile["chunks"].IsArray)
                        {
                            foreach (JsonData jd in projFile["chunks"])
                            {
                                if (jd.IsString)
                                {
                                    if (Path.GetFileNameWithoutExtension((string)jd) == chunkName)
                                    {
                                        chunkFile = (string)jd;
                                        break;
                                    }
                                }
                            }
                        }
                        BinBuffer chunkData;
                        if (chunkFile == null)
                        {
                            Console.Error.WriteLine($"Note: Chunk {chunkName} was not dumped, assuming it's empty");
                            chunkData = new BinBuffer();
                        }
                        else
                        {
                            Console.Error.WriteLine($"Note: Loading {chunkName} from dump");
                            try
                            {
                                chunkData = new BinBuffer(File.ReadAllBytes(Path.Combine(baseDir, chunkFile)));
                            }
                            catch (Exception e)
                            {
                                Console.Error.WriteLine($"Error loading {chunkName}, using empty");
                                Console.Error.WriteLine(e);
                                chunkData = new BinBuffer();
                            }
                        }
                        chunk = new BBData(chunkData, new int[0]);
                        break;
                }
                if (chunkStringOffsetOffsets != null)
                {
                    foreach (var offset in chunkStringOffsetOffsets)
                    {
                        stringOffsetOffsets.Add(offset + writer.Buffer.Position);
                    }
                }
                chunkStringOffsetOffsets = null;
                if (chunkTexpOffsetOffsets != null)
                {
                    foreach (var offset in chunkTexpOffsetOffsets)
                    {
                        texpOffsetOffsets.Add(offset + writer.Buffer.Position);
                    }
                }
                chunkTexpOffsetOffsets = null;
                if (chunkCodeOffsetOffsets != null)
                {
                    foreach (var offset in chunkCodeOffsetOffsets)
                    {
                        codeOffsetOffsets.Add(offset + writer.Buffer.Position);
                    }
                }
                chunkCodeOffsetOffsets = null;
                SectionWriter.WriteChunk(writer, chunkId, chunk);
            }

            writer.Buffer.Position = 4;
            writer.Buffer.Write(writer.Buffer.Size - 8);
            writer.Buffer.Position = writer.Buffer.Size;

            foreach (var stringOffset in stringOffsetOffsets)
            {
                writer.Buffer.Position = stringOffset;
                var o = writer.Buffer.ReadInt32();
                //bb.Position -= sizeof(int);
                writer.Buffer.Write(o + stringsDataPosition);
            }

            foreach (var texpOffset in texpOffsetOffsets)
            {
                writer.Buffer.Position = texpOffset;
                var o = writer.Buffer.ReadInt32();
                //bb.Position -= sizeof(int);
                writer.Buffer.Write(o + texpChunkPosition);
            }

            foreach (var codeOffset in codeOffsetOffsets)
            {
                writer.Buffer.Position = codeOffset;
                var o = writer.Buffer.ReadInt32();
                //bb.Position -= sizeof(int);
                writer.Buffer.Write(o + codeChunkPosition);
            }

            writer.Buffer.Position = 0;
            return writer.Buffer.ReadBytes(writer.Buffer.Size);
        }

        [STAThread]
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture =
                CultureInfo.InvariantCulture;

            var o = new Options();
            CLParser.Default.ParseArgumentsStrict(args, o, (verb, vo) =>
            {
                if (vo == null)
                    return;

                try
                {
                    switch (verb)
                    {
                        case "export":
                            Export((ExportOptions)vo);
                            break;
                        case "import":
                            Import((ImportOptions)vo);
                            break;
                    }
                }
                catch (Exception)
                {
                    Console.Error.WriteLine($"An error occured during {verb}");
                    throw;
                }
                Console.WriteLine("Done");
            });
        }
    }
}
