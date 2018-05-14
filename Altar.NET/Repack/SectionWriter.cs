using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Altar.Repack
{
    /* original (offset refs only)
     * GEN8 -> STRG
     * OPTN -> STRG
     *-EXTN
     * SOND -> STRG
     *-AGRP
     * SPRT -> STRG, TPAG
     * BGND -> STRG, TPAG
     * PATH -> STRG
     * SCPT -> STRG
     *-SHDR
     * FONT -> STRG, TPAG
     *-TMLN
     * OBJT -> STRG
     * ROOM -> STRG
     *-DAFL
     * TPAG
     * CODE -> STRG
     * VARI -> STRG, CODE!
     * FUNC -> STRG, CODE!
     * STRG
     * TXTR
     * AUDO
     *
     * useful order:
     *-EXTN
     *-AGRP
     *-SHDR
     *-TMLN
     *-DAFL
     * STRG
     * TXTR
     * AUDO
     * TPAG
     * GEN8 -> STRG
     * OPTN -> STRG
     * CODE -> STRG
     * SOND -> STRG
     * PATH -> STRG
     * SCPT -> STRG
     * OBJT -> STRG
     * ROOM -> STRG
     * SPRT -> STRG, TPAG
     * BGND -> STRG, TPAG
     * FONT -> STRG, TPAG
     * VARI -> STRG, CODE!
     * FUNC -> STRG, CODE!
     */

    //TODO: dump the empty chunks? -> non-empty examples?
    //TODO: what to do with unknown data? -> add to JSON?

    public class BBData
    {
        public BinBuffer Buffer;
        public int[] OffsetOffsets;

        public BBData(BinBuffer bb, int[] offs)
        {
            Buffer        = bb;
            OffsetOffsets = offs;
        }
    }

    public static class SectionWriter
    {
        static void UpdateOffsets(BBData data, int parentOffset)
        {
            var offs = data.OffsetOffsets;
            var bb = data.Buffer;

            for (int i = 0; i < offs.Length; i++)
            {
                bb.Position = offs[i];
                var o = bb.ReadInt32();
                //bb.Position -= sizeof(int);
                bb.Write(o + parentOffset);
                offs[i] += parentOffset;
            }

            data.Buffer = bb;
            data.OffsetOffsets = offs;
        }

        public static void Write(BinBuffer self, BBData data)
        {
            UpdateOffsets(data, self.Position);

            self.Write(data.Buffer);
        }
        public static int WriteOffset(BinBuffer self, int offset)
        {
            var r = self.Position;

            self.Write(offset);

            return r;
        }

        public static int[] WriteList(BBData data, BBData[] datas)
        {
            var bb = data.Buffer;

            bb.Write(datas.Length);

            var allOffs = data.OffsetOffsets.ToList();

            var offAcc = bb.Position + datas.Length * sizeof(int); // after all offsets
            int[] offsets = new int[datas.Length];
            for (int i = 0; i < datas.Length; i++)
            {
                allOffs.Add(bb.Position);
                bb.Write(offAcc);
                offsets[i] = offAcc;

                offAcc += datas[i].Buffer.Size;
            }

            for (int i = 0; i < datas.Length; i++)
            {
                Write(bb, datas[i]);
                allOffs.AddRange(datas[i].OffsetOffsets); // updated by Write
            }

            data.OffsetOffsets = allOffs.ToArray();
            return offsets;
        }

        public static int[] WriteList<T>(BBData data, T[] things,
            Action<BBData, T> writeThing)
        {
            BBData[] datas = new BBData[things.Length];

            for (int i = 0; i < things.Length; i++)
            {
                BBData thingdata = new BBData(new BinBuffer(), new int[0]);
                writeThing(thingdata, things[i]);
                datas[i] = thingdata;
            }

            return WriteList(data, datas);
        }

        public static int[] WriteList<T>(BBData data, T[] things,
            Action<BBData, T, IDictionary<string, int>> writeThing,
            IDictionary<string, int> stringOffsets)
        {
            return WriteList(data, things, (thingdata, thing) => writeThing(thingdata, thing, stringOffsets));
        }

        public static int[] WriteList<T>(BBData data, T[] things,
            Action<BBData, T, IDictionary<string, int>, int[]> writeThing,
            IDictionary<string, int> stringOffsets, int[] texPagOffsets)
        {
            return WriteList(data, things, (thingdata, thing) => writeThing(thingdata, thing, stringOffsets, texPagOffsets));
        }

        public static void WriteChunk(BBData data, SectionHeaders chunk, BBData inner)
        {
            var bb = data.Buffer;

            bb.Write((uint)chunk);
            bb.Write(inner.Buffer.Size);

            Write(bb, inner);
        }

        public static void WriteIffWad(BBData data, IDictionary<SectionHeaders, BBData> chunks)
        {
            foreach (var kvp in chunks) WriteChunk(data, kvp.Key, kvp.Value);
        }

        public static void WriteString(BBData data, String s)
        {
            var bb = data.Buffer;
            bb.Write(s);
            bb.Write((byte)0);
        }

        public static IDictionary<string, int> WriteStrings(BBData data, String[] strings)
        {
            int[] offsets = WriteList(data, strings, WriteString);

            Dictionary<string, int> stringOffsets = new Dictionary<string, int>(strings.Length);
            for (int i = 0; i < strings.Length; i++)
            {
                stringOffsets[strings[i]] = offsets[i];
            }
            return stringOffsets;
        }

        public static int[] WriteGeneral(BBData data, GeneralInfo ge, IDictionary<string, int> stringOffsets)
        {
            var ret = new SectionGeneral();
            var stringOffsetOffsets = new int[4];

            ret.Debug = ge.IsDebug;
            ret.FilenameOffset = (uint)stringOffsets[ge.FileName];
            stringOffsetOffsets[0] = (int)Marshal.OffsetOf(typeof(SectionGeneral), "FilenameOffset") - 3;
            ret.ConfigOffset = (uint)stringOffsets[ge.Configuration];
            stringOffsetOffsets[1] = (int)Marshal.OffsetOf(typeof(SectionGeneral), "ConfigOffset") - 3;
            ret.NameOffset = (uint)stringOffsets[ge.Name];
            stringOffsetOffsets[2] = (int)Marshal.OffsetOf(typeof(SectionGeneral), "NameOffset") - 3;
            ret.DisplayNameOffset = (uint)stringOffsets[ge.DisplayName];
            stringOffsetOffsets[3] = (int)Marshal.OffsetOf(typeof(SectionGeneral), "DisplayNameOffset") - 3;
            ret.GameID = ge.GameID;
            ret.WindowSize = ge.WindowSize;
            ret.BytecodeVersion = (Int24)ge.BytecodeVersion;
            ret.Major = ge.Version.Major;
            ret.Minor = ge.Version.Minor;
            ret.Release = ge.Version.Build;
            ret.Build = ge.Version.Revision;

            ret.Info = ge.InfoFlags;
            ret.ActiveTargets = ge.ActiveTargets;
            ret.AppID = ge.SteamAppID;

            for (int i = 0; i < 4; i++)
                unsafe
                {
                    ret._unknown[i] = ge.unknown[i];
                }

            unsafe
            {
                Marshal.Copy(ge.LicenseMD5Hash, 0, (IntPtr)ret.MD5, 0x10);
            }

            ret.CRC32 = ge.LicenceCRC32;

            ret.Timestamp = (ulong)(ge.Timestamp.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            if (ge.WeirdNumbers == null)
            {
                ret.NumberCount = 0;
            }
            else
            {
                ret.NumberCount = (uint)ge.WeirdNumbers.Length;
            }

            var tmp = new BinBuffer();
            tmp.Write(ret);
            data.Buffer.Write(tmp, 0, tmp.Size - 12, 8);

            for (int i = 0; i < ret.NumberCount; i++)
                data.Buffer.Write(ge.WeirdNumbers[i]);

            return stringOffsetOffsets;
        }

        public static int[] WriteOptions(BBData data, OptionInfo opt, IDictionary<string, int> stringOffsets)
        {
            var ret = new SectionOptions();
            var stringOffsetOffsets = new List<int>();
            
            unsafe
            {
                for (int i = 0; i < 2; i++)
                {
                    ret._pad0[i] = opt._pad0[i];
                }
                for (int i = 0; i < 0xC; i++)
                {
                    ret._pad1[i] = opt._pad1[i];
                }
            }
            ret.GEN8FlagsDup = opt.InfoFlags;

            if (opt.Constants == null)
            {
                ret.ConstMap.Count = 0;
            }
            else
            {
                ret.ConstMap.Count = (uint)opt.Constants.Count;
            }

            ret.ConstMap.Offsets = 0xABCDEF01;

            var tmp = new BinBuffer();
            tmp.Write(ret);
            data.Buffer.Write(tmp, 0, tmp.Size - 12, 8);

            if (opt.Constants != null)
            {
                foreach (var kvp in opt.Constants)
                {
                    stringOffsetOffsets.Add(data.Buffer.Position + 8);
                    data.Buffer.Write(stringOffsets[kvp.Key]);
                    stringOffsetOffsets.Add(data.Buffer.Position + 8);
                    data.Buffer.Write(stringOffsets[kvp.Value]);
                }
            }

            return stringOffsetOffsets.ToArray();
        }

        public static void Pad(BBData data, int amount, int offset)
        {
            var pad = (amount-1) - ((data.Buffer.Position + offset - 1) % amount);
            for (int j = 0; j < pad; j++)
            {
                data.Buffer.WriteByte(0);
            }
        }

        public static void WriteTextures(BBData data, TextureInfo[] textures)
        {
            BBData[] datas = new BBData[textures.Length];

            for (int i = 0; i < textures.Length; i++)
            {
                BBData texturedata = new BBData(new BinBuffer(), new int[0]);
                texturedata.Buffer.Write(0); // TextureEntry._pad
                texturedata.Buffer.Write(0); // TextureEntry.Offset
                datas[i] = texturedata;
            }

            int[] offsets = WriteList(data, datas);

            int[] secondaryOffsets = new int[textures.Length];
            for (int i = 0; i < textures.Length; i++)
            {
                Pad(data, 0x80, 8);
                var p = data.Buffer.Position;
                data.Buffer.Position = offsets[i]+4;
                secondaryOffsets[i] = data.Buffer.Position;
                data.Buffer.Write(p);
                data.Buffer.Position = p;
                data.Buffer.Write(textures[i].PngData);
            }
            data.OffsetOffsets = data.OffsetOffsets.Concat(secondaryOffsets).ToArray();

            // ???
            data.Buffer.WriteByte(0);
            data.Buffer.WriteByte(0);
        }

        private static void WriteFontCharEntry(BBData data, FontCharacter fc)
        {
            var ce = new FontCharEntry();
            ce.Character = fc.Character;
            ce.TexPagFrame = fc.TPagFrame;
            ce.Shift = fc.Shift;
            ce.Offset = fc.Offset;
            data.Buffer.Write(ce);
        }

        public static void WriteFont(BBData data, FontInfo fi, IDictionary<string, int> stringOffsets, int[] texPagOffsets)
        {
            var fe = new FontEntry();
            fe.CodeName = fi.CodeName == null ? 0 : (uint)stringOffsets[fi.CodeName];
            fe.SystemName = (uint)stringOffsets[fi.SystemName];
            fe.EmSize = fi.EmSize;
            fe.Bold = fi.IsBold ? DwordBool.True : DwordBool.False;
            fe.Italic = fi.IsItalic ? DwordBool.True : DwordBool.False;
            fe._ignore0 = fi.Characters[0].Character;
            fe.Charset = fi.Charset;
            fe.AntiAliasing = fi.AntiAliasing;
            foreach (var character in fi.Characters)
            {
                if (character.Character > fe._ignore1)
                {
                    fe._ignore1 = character.Character;
                }
            }
            fe.TPagOffset = (uint)texPagOffsets[fi.TexPagId];
            fe.Scale = fi.Scale;
            data.Buffer.Write(fe);

            data.Buffer.Position -= 8;

            WriteList(data, fi.Characters, WriteFontCharEntry);
        }

        public static void WriteFonts(BBData data,
            FontInfo[] fonts,
            IDictionary<string, int> stringOffsets,
            int[] texPagOffsets,
            out int[] stringOffsetOffsets,
            out int[] texpOffsetOffsets)
        {
            int[] offsets = WriteList(data, fonts, WriteFont, stringOffsets, texPagOffsets);

            stringOffsetOffsets = new int[fonts.Length * 2];
            texpOffsetOffsets = new int[fonts.Length];

            for (int i = 0; i < fonts.Length; i++)
            {
                stringOffsetOffsets[i*2] = offsets[i] + (int)Marshal.OffsetOf(typeof(FontEntry), "CodeName") + 8;
                stringOffsetOffsets[i*2+1] = offsets[i] + (int)Marshal.OffsetOf(typeof(FontEntry), "SystemName") + 8;
                texpOffsetOffsets[i] = offsets[i] + (int)Marshal.OffsetOf(typeof(FontEntry), "TPagOffset") + 8;
            }

            // pad?
            for (int i = 0; i < 0x80; i++)
            {
                data.Buffer.Write((ushort)i);
            }
            for (int i = 0; i < 0x80; i++)
            {
                data.Buffer.Write((ushort)0x3F);
            }
        }

        public static void WriteAudio(BBData data, AudioInfo[] waves, int offset)
        {
            BBData[] datas = new BBData[waves.Length];

            for (int i = 0; i < waves.Length; i++)
            {
                BBData audiodata = new BBData(new BinBuffer(), new int[0]);
                audiodata.Buffer.Write(waves[i].Wave.Length); // AudioEntry.Length
                audiodata.Buffer.Write(waves[i].Wave);
                if (i != waves.Length-1) Pad(audiodata, 0x4, i == 0 ? 8+offset : 0);
                datas[i] = audiodata;
            }

            WriteList(data, datas);
        }

        private static void WriteTexturePage(BBData data, TexturePageInfo tpi)
        {
            var tpe = new TexPageEntry();
            tpe.Source = tpi.Source;
            tpe.Dest = tpi.Destination;
            tpe.Size = tpi.Size;
            tpe.SpritesheetId = (ushort)tpi.SpritesheetId;
            data.Buffer.Write(tpe);
        }

        public static int[] WriteTexturePages(BBData data, TexturePageInfo[] texturePages)
        {
            return WriteList(data, texturePages, WriteTexturePage);
        }

        private static void WriteObject(BBData data, ObjectInfo oi, IDictionary<string, int> stringOffsets)
        {
            var oe = new ObjectEntry();

            oe.Name = (uint)stringOffsets[oi.Name];
            oe.SpriteIndex = oi.SpriteIndex;
            oe.Visible = oi.IsVisible ? DwordBool.True : DwordBool.False;
            oe.Solid = oi.IsSolid ? DwordBool.True : DwordBool.False;
            oe.Depth = oi.Depth;
            oe.Persistent = oi.IsPersistent ? DwordBool.True : DwordBool.False;

            oe.ParentId = oi.ParentId == null ? -1 : (int)oi.ParentId;
            oe.MaskId = oi.TexMaskId == null ? -1 : (int)oi.TexMaskId;

            oe.HasPhysics = oi.Physics != null ? DwordBool.True : DwordBool.False;
            oe.IsSensor = oi.IsSensor ? DwordBool.True : DwordBool.False;
            oe.CollisionShape = oi.CollisionShape;

            if (oi.Physics != null) oe.Physics = (ObjectPhysics)oi.Physics;
            
            for (int i = 0; i < oi.OtherFloats.Length; i++)
            {
                unsafe
                {
                    oe.Rest.MoreFloats[i] = oi.OtherFloats[i];
                }
            }

            var hasMore = oi.OtherFloats.Length > 0;
            
            if (hasMore)
            {
                if (oi.ShapePoints == null) oe.Rest.ShapePoints_IfMoreFloats.Count = 0xFFFFFFFF;
                else oe.Rest.ShapePoints_IfMoreFloats.Count = (uint)oi.ShapePoints.Length << 1;
                BinBuffer tmp = new BinBuffer();
                tmp.Write(oe);
                data.Buffer.Write(tmp.AsByteArray(), 0, 4+(int)Marshal.OffsetOf(typeof(ObjectEntry), "Rest")+(int)Marshal.OffsetOf(typeof(ObjectRest), "ShapePoints_IfMoreFloats"));
            }
            else
            {
                if (oi.ShapePoints == null) oe.Rest.ShapePoints.Count = 0xFFFFFFFF;
                else oe.Rest.ShapePoints.Count = (uint)oi.ShapePoints.Length << 1;
                BinBuffer tmp = new BinBuffer();
                tmp.Write(oe);
                data.Buffer.Write(tmp.AsByteArray(), 0, 4 + (int)Marshal.OffsetOf(typeof(ObjectEntry), "Rest") + (int)Marshal.OffsetOf(typeof(ObjectRest), "ShapePoints"));
            }
            if (oi.ShapePoints != null)
            {
                var ptdata = new BinBuffer();
                var ptoffsets = new int[oi.ShapePoints.Length << 1];

                for (int i = 0; i < oi.ShapePoints.Length; i++)
                {
                    var point = oi.ShapePoints[i];
                    if (point.X != -0xDEAD)
                    {
                        ptoffsets[i*2] = ptdata.Position;
                        ptdata.Write(point.X);
                    }
                    if (point.Y != -0xC0DE)
                    {
                        ptoffsets[i*2+1] = ptdata.Position;
                        ptdata.Write(point.Y);
                    }
                }

                var ptdataoffset = data.Buffer.Position + oi.ShapePoints.Length * 8;
                var offsetOffsets = new List<int>();
                for (int i = 0; i < oi.ShapePoints.Length; i++)
                {
                    var point = oi.ShapePoints[i];
                    if (point.X == -0xDEAD)
                    {
                        data.Buffer.Write(0xFFFFFFFF);
                    }
                    else
                    {
                        offsetOffsets.Add(data.Buffer.Position);
                        data.Buffer.Write(ptoffsets[i*2] + ptdataoffset);
                    }
                    if (point.Y == -0xC0DE)
                    {
                        data.Buffer.Write(0xFFFFFFFF);
                    }
                    else
                    {
                        offsetOffsets.Add(data.Buffer.Position);
                        data.Buffer.Write(ptoffsets[i*2+1] + ptdataoffset);
                    }
                }
                data.OffsetOffsets = offsetOffsets.ToArray();

                data.Buffer.Write(ptdata);
            }
        }

        public static int[] WriteObjects(BBData data, ObjectInfo[] objects, IDictionary<string, int> stringOffsets)
        {
            int[] offsets = WriteList(data, objects, WriteObject, stringOffsets);
            var stringOffsetOffsets = new int[objects.Length];

            for (int i = 0; i < objects.Length; i++)
            {
                stringOffsetOffsets[i] = offsets[i] + (int)Marshal.OffsetOf(typeof(ObjectEntry), "Name") + 8;
            }
            return stringOffsetOffsets;
        }

        private static void WriteSound(BBData data, SoundInfo si, IDictionary<string, int> stringOffsets, string[] audioGroups)
        {
            var se = new SoundEntry();

            se.NameOffset = (uint)stringOffsets[si.Name];
            se.TypeOffset = (uint)stringOffsets[si.Type];
            se.FileOffset = (uint)stringOffsets[si.File];

            se.Volume = si.VolumeMod;
            se.Pitch = si.PitchMod;

            if (si.Group == null || si.Group.Length == 0)
            {
                se.GroupID = 0;
            }
            else
            {
                se.GroupID = Array.IndexOf(audioGroups, si.Group);
            }

            se.AudioID = si.AudioID;
            se.Flags = SoundEntryFlags.Normal;
            if (si.IsEmbedded) se.Flags |= SoundEntryFlags.Embedded;
            if (si.IsCompressed) se.Flags |= SoundEntryFlags.Compressed;

            data.Buffer.Write(se);
        }

        public static int[] WriteSounds(BBData data, SoundInfo[] sounds, IDictionary<string, int> stringOffsets, string[] audioGroups)
        {
            int[] offsets = WriteList(data, sounds, (sounddata, sound) => WriteSound(sounddata, sound, stringOffsets, audioGroups));
            var stringOffsetOffsets = new int[sounds.Length*3];

            for (int i = 0; i < sounds.Length; i++)
            {
                stringOffsetOffsets[i*3] = offsets[i] + (int)Marshal.OffsetOf(typeof(SoundEntry), "NameOffset") + 8;
                stringOffsetOffsets[i*3+1] = offsets[i] + (int)Marshal.OffsetOf(typeof(SoundEntry), "TypeOffset") + 8;
                stringOffsetOffsets[i*3+2] = offsets[i] + (int)Marshal.OffsetOf(typeof(SoundEntry), "FileOffset") + 8;
            }
            return stringOffsetOffsets;
        }

        private static void WriteSprite(BBData data, SpriteInfo si, IDictionary<string, int> stringOffsets, int[] texPagOffsets)
        {
            var se = new SpriteEntry();

            se.Name = (uint)stringOffsets[si.Name];
            se.Size = si.Size;
            se.Bounding = si.Bounding;
            se.BBoxMode = si.BBoxMode;
            se.Origin = si.Origin;

            se.SeparateColMasks = si.SeparateColMasks ? DwordBool.True : DwordBool.False;

            se.Textures.Count = si.TextureIndices == null ? 0xFFFFFFFF : (uint)si.TextureIndices.Length;
            var tmp = new BinBuffer();
            tmp.Write(se);
            data.Buffer.Write(tmp, 0, tmp.Size - 4, 0);

            if (si.TextureIndices != null)
            {
                foreach (var ti in si.TextureIndices)
                {
                    data.Buffer.Write(texPagOffsets[ti]);
                }
                
                if (si.CollisionMasks == null)
                {
                    data.Buffer.Write(0xFFFFFFFF);
                }
                else
                {
                    data.Buffer.Write(si.SeparateColMasks ? (uint)si.CollisionMasks.Length : 1);
                    foreach (var mask in si.CollisionMasks)
                    {
                        int w = mask.GetLength(0);
                        for (int y = 0; y < mask.GetLength(1); y++)
                        {
                            for (int x = 0; x < w; x += 8)
                            {
                                byte b = 0;
                                if (x     < w && mask[x,     y]) b |= 0b00000001;
                                if (x + 1 < w && mask[x + 1, y]) b |= 0b00000010;
                                if (x + 2 < w && mask[x + 2, y]) b |= 0b00000100;
                                if (x + 3 < w && mask[x + 3, y]) b |= 0b00001000;
                                if (x + 4 < w && mask[x + 4, y]) b |= 0b00010000;
                                if (x + 5 < w && mask[x + 5, y]) b |= 0b00100000;
                                if (x + 6 < w && mask[x + 6, y]) b |= 0b01000000;
                                if (x + 7 < w && mask[x + 7, y]) b |= 0b10000000;
                                data.Buffer.Write(b);
                            }
                        }
                    }
                }
            }
            Pad(data, 4, 0);
        }

        public static void WriteSprites(BBData data,
            SpriteInfo[] sprites,
            IDictionary<string, int> stringOffsets,
            int[] texPagOffsets,
            out int[] stringOffsetOffsets,
            out int[] texpOffsetOffsetsArray)
        {
            int[] offsets = WriteList(data, sprites, WriteSprite, stringOffsets, texPagOffsets);
            stringOffsetOffsets = new int[sprites.Length];
            var texpOffsetOffsetsList = new List<int>();
            for (int i = 0; i < sprites.Length; i++)
            {
                stringOffsetOffsets[i] = offsets[i] + (int)Marshal.OffsetOf(typeof(SpriteEntry), "Name") + 8;
                var si = sprites[i];
                if (si.TextureIndices != null)
                {
                    var texIdxOffs = offsets[i] + (int)Marshal.OffsetOf(typeof(SpriteEntry), "Textures") + 12;
                    for (int j = 0; j < si.TextureIndices.Length; j++)
                    {
                        texpOffsetOffsetsList.Add(texIdxOffs + j * sizeof(int));
                    }
                }
            }
            texpOffsetOffsetsArray = texpOffsetOffsetsList.ToArray();
        }

        private static void WriteBackground(BBData data, BackgroundInfo bi, IDictionary<string, int> stringOffsets, int[] texPagOffsets)
        {
            var be = new BgEntry();
            be.Name = (uint)stringOffsets[bi.Name];
            be.TextureOffset = bi.TexPageIndex.HasValue ? (uint)texPagOffsets[bi.TexPageIndex.Value] : 0;
            data.Buffer.Write(be);
        }

        public static void WriteBackgrounds(BBData data,
            BackgroundInfo[] backgrounds,
            IDictionary<string, int> stringOffsets,
            int[] texPagOffsets,
            out int[] stringOffsetOffsets,
            out int[] texpOffsetOffsetsArray)
        {
            int[] offsets = WriteList(data, backgrounds, WriteBackground, stringOffsets, texPagOffsets);
            stringOffsetOffsets = new int[backgrounds.Length];
            var texpOffsetOffsetsList = new List<int>();
            for (int i = 0; i < backgrounds.Length; i++)
            {
                stringOffsetOffsets[i] = offsets[i] + (int)Marshal.OffsetOf(typeof(BgEntry), "Name") + 8;
                if (backgrounds[i].TexPageIndex.HasValue) texpOffsetOffsetsList.Add(offsets[i] + (int)Marshal.OffsetOf(typeof(BgEntry), "TextureOffset") + 8);
            }
            texpOffsetOffsetsArray = texpOffsetOffsetsList.ToArray();
        }

        private static void WritePath(BBData data, PathInfo pi, IDictionary<string, int> stringOffsets)
        {
            var pe = new PathEntry();

            pe.Name = (uint)stringOffsets[pi.Name];
            pe.IsSmooth = pi.IsSmooth ? DwordBool.True : DwordBool.False;
            pe.IsClosed = pi.IsClosed ? DwordBool.True : DwordBool.False;
            pe.Precision = pi.Precision;

            pe.PointCount = (uint)pi.Points.Length;

            var tmp = new BinBuffer();
            tmp.Write(pe);
            data.Buffer.Write(tmp, 0, tmp.Size - 12, 0);
            
            foreach (var pt in pi.Points)
                data.Buffer.Write(pt);
        }

        public static int[] WritePaths(BBData data, PathInfo[] paths, IDictionary<string, int> stringOffsets)
        {
            int[] offsets = WriteList(data, paths, WritePath, stringOffsets);
            var stringOffsetOffsets = new int[paths.Length];

            for (int i = 0; i < paths.Length; i++)
            {
                stringOffsetOffsets[i] = offsets[i] + (int)Marshal.OffsetOf(typeof(PathEntry), "Name") + 8;
            }
            return stringOffsetOffsets;
        }

        private static void WriteScript(BBData data, ScriptInfo si, IDictionary<string, int> stringOffsets)
        {
            var se = new ScriptEntry();

            se.Name = (uint)stringOffsets[si.Name];
            se.CodeId = si.CodeId;

            data.Buffer.Write(se);
        }

        public static int[] WriteScripts(BBData data, ScriptInfo[] scripts, IDictionary<string, int> stringOffsets)
        {
            int[] offsets = WriteList(data, scripts, WriteScript, stringOffsets);
            var stringOffsetOffsets = new int[scripts.Length];
            for (int i = 0; i < scripts.Length; i++)
            {
                stringOffsetOffsets[i] = offsets[i] + (int)Marshal.OffsetOf(typeof(ScriptEntry), "Name") + 8;
            }
            return stringOffsetOffsets;
        }

        private static void WriteRoomBg(BBData data, RoomBackground rb)
        {
            var rbe = new RoomBgEntry();

            rbe.IsEnabled    = rb.IsEnabled     ? DwordBool.True : DwordBool.False;
            rbe.IsForeground = rb.IsForeground  ? DwordBool.True : DwordBool.False;
            rbe.Position     = rb.Position;
            rbe.TileX        = rb.TileX         ? DwordBool.True : DwordBool.False;
            rbe.TileY        = rb.TileY         ? DwordBool.True : DwordBool.False;
            rbe.Speed        = rb.Speed;
            rbe.Stretch      = rb.StretchSprite ? DwordBool.True : DwordBool.False;

            rbe.DefIndex  = rb.BgIndex.HasValue ? rb.BgIndex.Value : 0xFFFFFFFF;

            data.Buffer.Write(rbe);
        }

        private static void WriteRoomView(BBData data, RoomView rv)
        {
            var rve = new RoomViewEntry();

            rve.IsEnabled = rv.IsEnabled ? DwordBool.True : DwordBool.False;
            rve.Port      = rv.Port;
            rve.View      = rv.View;
            rve.Border    = rv.Border;
            rve.Speed     = rv.Speed;

            rve.ObjectId = rv.ObjectId.HasValue ? rv.ObjectId.Value : 0xFFFFFFFF;

            data.Buffer.Write(rve);
        }

        private static void WriteRoomObj(BBData data, RoomObject ro)
        {
            var roe = new RoomObjEntry();

            roe.DefIndex = ro.DefIndex;
            roe.Position = ro.Position;
            roe.Scale    = ro.Scale;
            roe.Colour   = ro.Colour;
            roe.Rotation = ro.Rotation;

            roe.InstanceID   = ro.InstanceID;
            roe.CreateCodeID = ro.CreateCodeID;

            data.Buffer.Write(roe);
            data.Buffer.Write(0xFFFFFFFF);
        }

        private static void WriteRoomTile(BBData data, RoomTile rt)
        {
            var rte = new RoomTileEntry();

            rte.DefIndex  = rt.DefIndex;
            rte.Position  = rt.Position;
            rte.SourcePos = rt.SourcePosition;
            rte.Size      = rt.Size;
            rte.Scale     = rt.Scale;
            rte.Colour    = rt.Colour;

            rte.TileDepth  = rt.Depth;
            rte.InstanceID = rt.InstanceID;

            data.Buffer.Write(rte);
        }

        private static void WriteRoom(BBData data, RoomInfo ri, IDictionary<string, int> stringOffsets)
        {
            var re = new RoomEntry();

            re.Name       = (uint)stringOffsets[ri.Name];
            re.Caption    = (uint)stringOffsets[ri.Caption];
            re.Size       = ri.Size;
            re.Speed      = ri.Speed;
            re.Persistent = ri.IsPersistent ? DwordBool.True : DwordBool.False;
            re.Colour     = ri.Colour;

            re.DrawBackgroundColour = ri.DrawBackgroundColour ? DwordBool.True : DwordBool.False;
            re._unknown = ri._unknown;

            re.Flags = 0;
            if (ri.EnableViews       ) re.Flags |= RoomEntryFlags.EnableViews;
            if (ri.ShowColour        ) re.Flags |= RoomEntryFlags.ShowColour;
            if (ri.ClearDisplayBuffer) re.Flags |= RoomEntryFlags.ClearDisplayBuffer;

            re.World          = ri.World          ;
            re.Bounding       = ri.Bounding       ;
            re.Gravity        = ri.Gravity        ;
            re.MetresPerPixel = ri.MetresPerPixel ;

            var bgOffsetOffset   = (int)Marshal.OffsetOf(typeof(RoomEntry), "BgOffset");
            var viewOffsetOffset = (int)Marshal.OffsetOf(typeof(RoomEntry), "ViewOffset");
            var objOffsetOffset  = (int)Marshal.OffsetOf(typeof(RoomEntry), "ObjOffset");
            var tileOffsetOffset = (int)Marshal.OffsetOf(typeof(RoomEntry), "TileOffset");

            data.Buffer.Write(re);
            data.OffsetOffsets = new int[] { bgOffsetOffset, viewOffsetOffset, objOffsetOffset, tileOffsetOffset };

            data.Buffer.Position = bgOffsetOffset;
            data.Buffer.Write(data.Buffer.Size);
            data.Buffer.Position = data.Buffer.Size;
            WriteList(data, ri.Backgrounds, WriteRoomBg);

            data.Buffer.Position = viewOffsetOffset;
            data.Buffer.Write(data.Buffer.Size);
            data.Buffer.Position = data.Buffer.Size;
            WriteList(data, ri.Views, WriteRoomView);

            data.Buffer.Position = objOffsetOffset;
            data.Buffer.Write(data.Buffer.Size);
            data.Buffer.Position = data.Buffer.Size;
            WriteList(data, ri.Objects, WriteRoomObj);

            data.Buffer.Position = tileOffsetOffset;
            data.Buffer.Write(data.Buffer.Size);
            data.Buffer.Position = data.Buffer.Size;
            WriteList(data, ri.Tiles, WriteRoomTile);
        }

        public static int[] WriteRooms(BBData data, RoomInfo[] rooms, IDictionary<string, int> stringOffsets)
        {
            int[] offsets = WriteList(data, rooms, WriteRoom, stringOffsets);
            var stringOffsetOffsets = new int[rooms.Length*2];
            for (int i = 0; i < rooms.Length; i++)
            {
                stringOffsetOffsets[i*2] = offsets[i] + (int)Marshal.OffsetOf(typeof(RoomEntry), "Name") + 8;
                stringOffsetOffsets[i*2+1] = offsets[i] + (int)Marshal.OffsetOf(typeof(RoomEntry), "Caption") + 8;
            }
            return stringOffsetOffsets;
        }

        private static void WriteRefDef(BBData data, ReferenceDef rd, IDictionary<string, int> stringOffsets)
        {
            var rde = new RefDefEntry();

            rde.NameOffset = (uint)stringOffsets[rd.Name];
            rde.Occurrences = rd.Occurrences;
            rde.FirstAddress = rd.FirstOffset;

            data.Buffer.Write(rde);
        }

        private static void WriteRefDefWithOthers(BBData data, ReferenceDef rd, IDictionary<string, int> stringOffsets)
        {
            var rde = new RefDefEntryWithOthers();

            rde.NameOffset = (uint)stringOffsets[rd.Name];
            rde._pad0 = 0xFFFFFFFF;
            rde.Occurrences = rd.Occurrences;
            rde.FirstAddress = rd.FirstOffset;

            data.Buffer.Write(rde);
        }

        public static int[] WriteRefDefs(BBData data, ReferenceDef[] variables, IDictionary<string, int> stringOffsets, bool IsOldBCVersion, bool isFunction)
        {
            if (!IsOldBCVersion)
            {
                if (isFunction)
                {
                    data.Buffer.Write(variables.Length);
                }
                else
                {
                    data.Buffer.Write(0);
                    data.Buffer.Write(0);
                    data.Buffer.Write(0);
                }
            }
            var stringOffsetOffsets = new int[variables.Length];
            for (int i = 0; i < variables.Length; i++)
            {
                if (IsOldBCVersion || isFunction)
                {
                    stringOffsetOffsets[i] = data.Buffer.Position + (int)Marshal.OffsetOf(typeof(RefDefEntry), "NameOffset") + 8;
                    WriteRefDef(data, variables[i], stringOffsets);
                }
                else
                {
                    stringOffsetOffsets[i] = data.Buffer.Position + (int)Marshal.OffsetOf(typeof(RefDefEntryWithOthers), "NameOffset") + 8;
                    WriteRefDefWithOthers(data, variables[i], stringOffsets);
                }
            }
            return stringOffsetOffsets;
        }

        public static int[] WriteFunctionLocals(BBData data, FunctionLocalsInfo[] functions, IDictionary<string, int> stringOffsets)
        {
            var stringOffsetOffsets = new List<int>();

            data.Buffer.Write(functions.Length);

            foreach (var func in functions)
            {
                data.Buffer.Write((uint)func.LocalNames.Length);
                stringOffsetOffsets.Add(data.Buffer.Position + 8);
                data.Buffer.Write((uint)stringOffsets[func.FunctionName]);
                for (uint i = 0; i < func.LocalNames.Length; i++)
                {
                    stringOffsetOffsets.Add(data.Buffer.Position + 12);
                    var fle = new FunctionLocalEntry();
                    fle.Index = i;
                    fle.Name = (uint)stringOffsets[func.LocalNames[i]];
                    data.Buffer.Write(fle);
                }
            }

            return stringOffsetOffsets.ToArray();
        }
    }
}
