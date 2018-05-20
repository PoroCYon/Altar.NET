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
            var ret = new SectionGeneral
            {
                Debug = ge.IsDebug,
                FilenameOffset = (uint)stringOffsets[ge.FileName],
                ConfigOffset = (uint)stringOffsets[ge.Configuration],
                NameOffset = (uint)stringOffsets[ge.Name],
                DisplayNameOffset = (uint)stringOffsets[ge.DisplayName],
                GameID = ge.GameID,
                WindowSize = ge.WindowSize,
                BytecodeVersion = (Int24)ge.BytecodeVersion,
                Major = ge.Version.Major,
                Minor = ge.Version.Minor,
                Release = ge.Version.Build,
                Build = ge.Version.Revision,

                Info = ge.InfoFlags,
                ActiveTargets = ge.ActiveTargets,
                AppID = ge.SteamAppID
            };
            var stringOffsetOffsets = new int[]
            {
                (int)Marshal.OffsetOf(typeof(SectionGeneral), "FilenameOffset") - 3,
                (int)Marshal.OffsetOf(typeof(SectionGeneral), "ConfigOffset") - 3,
                (int)Marshal.OffsetOf(typeof(SectionGeneral), "NameOffset") - 3,
                (int)Marshal.OffsetOf(typeof(SectionGeneral), "DisplayNameOffset") - 3
            };

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
            data.Buffer.Write(new FontCharEntry
            {
                Character = fc.Character,
                TexPagFrame = fc.TPagFrame,
                Shift = fc.Shift,
                Offset = fc.Offset
            });
        }

        public static void WriteFont(BBData data, FontInfo fi, IDictionary<string, int> stringOffsets, int[] texPagOffsets)
        {
            var fe = new FontEntry
            {
                CodeName = fi.CodeName == null ? 0 : (uint)stringOffsets[fi.CodeName],
                SystemName = (uint)stringOffsets[fi.SystemName],
                EmSize = fi.EmSize,
                Bold = fi.IsBold ? DwordBool.True : DwordBool.False,
                Italic = fi.IsItalic ? DwordBool.True : DwordBool.False,
                _ignore0 = fi.Characters[0].Character,
                Charset = fi.Charset,
                AntiAliasing = fi.AntiAliasing,
                TPagOffset = (uint)texPagOffsets[fi.TexPagId],
                Scale = fi.Scale
            };
            foreach (var character in fi.Characters)
            {
                if (character.Character > fe._ignore1)
                {
                    fe._ignore1 = character.Character;
                }
            }
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
            var tpe = new TexPageEntry
            {
                Source = tpi.Source,
                Dest = tpi.Destination,
                Size = tpi.Size,
                SpritesheetId = (ushort)tpi.SpritesheetId
            };
            data.Buffer.Write(tpe);
        }

        public static int[] WriteTexturePages(BBData data, TexturePageInfo[] texturePages)
        {
            return WriteList(data, texturePages, WriteTexturePage);
        }

        private static void WriteObject(BBData data, ObjectInfo oi, IDictionary<string, int> stringOffsets)
        {
            var oe = new ObjectEntry
            {
                Name = (uint)stringOffsets[oi.Name],
                SpriteIndex = oi.SpriteIndex,
                Visible = oi.IsVisible ? DwordBool.True : DwordBool.False,
                Solid = oi.IsSolid ? DwordBool.True : DwordBool.False,
                Depth = oi.Depth,
                Persistent = oi.IsPersistent ? DwordBool.True : DwordBool.False,

                ParentId = oi.ParentId == null ? -1 : (int)oi.ParentId,
                MaskId = oi.TexMaskId == null ? -1 : (int)oi.TexMaskId,

                HasPhysics = oi.Physics != null ? DwordBool.True : DwordBool.False,
                IsSensor = oi.IsSensor ? DwordBool.True : DwordBool.False,
                CollisionShape = oi.CollisionShape
            };

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
            var se = new SoundEntry
            {
                NameOffset = (uint)stringOffsets[si.Name],
                TypeOffset = (uint)stringOffsets[si.Type],
                FileOffset = (uint)stringOffsets[si.File],

                Volume = si.VolumeMod,
                Pitch = si.PitchMod,

                GroupID = (si.Group == null || si.Group.Length == 0) ? 0 : Array.IndexOf(audioGroups, si.Group),
                AudioID = si.AudioID,
                Flags = SoundEntryFlags.Normal
            };

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
            var se = new SpriteEntry
            {
                Name = (uint)stringOffsets[si.Name],
                Size = si.Size,
                Bounding = si.Bounding,
                BBoxMode = si.BBoxMode,
                Origin = si.Origin,

                SeparateColMasks = si.SeparateColMasks ? DwordBool.True : DwordBool.False
            };

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
            data.Buffer.Write(new BgEntry
            {
                Name = (uint)stringOffsets[bi.Name],
                TextureOffset = bi.TexPageIndex.HasValue ? (uint)texPagOffsets[bi.TexPageIndex.Value] : 0
            });
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
            var tmp = new BinBuffer();
            tmp.Write(new PathEntry
            {
                Name = (uint)stringOffsets[pi.Name],
                IsSmooth = pi.IsSmooth ? DwordBool.True : DwordBool.False,
                IsClosed = pi.IsClosed ? DwordBool.True : DwordBool.False,
                Precision = pi.Precision,

                PointCount = (uint)pi.Points.Length
            });
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
            data.Buffer.Write(new ScriptEntry
            {
                Name = (uint)stringOffsets[si.Name],
                CodeId = si.CodeId
            });
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
            data.Buffer.Write(new RoomBgEntry
            {
                IsEnabled = rb.IsEnabled ? DwordBool.True : DwordBool.False,
                IsForeground = rb.IsForeground ? DwordBool.True : DwordBool.False,
                Position = rb.Position,
                TileX = rb.TileX ? DwordBool.True : DwordBool.False,
                TileY = rb.TileY ? DwordBool.True : DwordBool.False,
                Speed = rb.Speed,
                Stretch = rb.StretchSprite ? DwordBool.True : DwordBool.False,

                DefIndex = rb.BgIndex ?? 0xFFFFFFFF
            });
        }

        private static void WriteRoomView(BBData data, RoomView rv)
        {
            data.Buffer.Write(new RoomViewEntry
            {
                IsEnabled = rv.IsEnabled ? DwordBool.True : DwordBool.False,
                Port = rv.Port,
                View = rv.View,
                Border = rv.Border,
                Speed = rv.Speed,

                ObjectId = rv.ObjectId ?? 0xFFFFFFFF
            });
        }

        private static void WriteRoomObj(BBData data, RoomObject ro)
        {
            data.Buffer.Write(new RoomObjEntry
            {
                DefIndex = ro.DefIndex,
                Position = ro.Position,
                Scale = ro.Scale,
                Colour = ro.Colour,
                Rotation = ro.Rotation,

                InstanceID = ro.InstanceID,
                CreateCodeID = ro.CreateCodeID
            });
            data.Buffer.Write(0xFFFFFFFF);
        }

        private static void WriteRoomTile(BBData data, RoomTile rt)
        {
            data.Buffer.Write(new RoomTileEntry
            {
                DefIndex = rt.DefIndex,
                Position = rt.Position,
                SourcePos = rt.SourcePosition,
                Size = rt.Size,
                Scale = rt.Scale,
                Colour = rt.Colour,

                TileDepth = rt.Depth,
                InstanceID = rt.InstanceID
            });
        }

        private static void WriteRoom(BBData data, RoomInfo ri, IDictionary<string, int> stringOffsets)
        {
            var re = new RoomEntry
            {
                Name = (uint)stringOffsets[ri.Name],
                Caption = (uint)stringOffsets[ri.Caption],
                Size = ri.Size,
                Speed = ri.Speed,
                Persistent = ri.IsPersistent ? DwordBool.True : DwordBool.False,
                Colour = ri.Colour,

                DrawBackgroundColour = ri.DrawBackgroundColour ? DwordBool.True : DwordBool.False,
                _unknown = ri._unknown,

                Flags = 0,
                
                World = ri.World,
                Bounding = ri.Bounding,
                Gravity = ri.Gravity,
                MetresPerPixel = ri.MetresPerPixel
            };
            if (ri.EnableViews       ) re.Flags |= RoomEntryFlags.EnableViews;
            if (ri.ShowColour        ) re.Flags |= RoomEntryFlags.ShowColour;
            if (ri.ClearDisplayBuffer) re.Flags |= RoomEntryFlags.ClearDisplayBuffer;

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
            data.Buffer.Write(new RefDefEntry
            {
                NameOffset = (uint)stringOffsets[rd.Name],
                Occurrences = rd.Occurrences,
                FirstAddress = rd.FirstOffset
            });
        }

        private static void WriteRefDefWithOthers(BBData data, ReferenceDef rd, IDictionary<string, int> stringOffsets)
        {
            data.Buffer.Write(new RefDefEntryWithOthers
            {
                NameOffset = (uint)stringOffsets[rd.Name],
                _pad0 = rd.unknown1,
                _pad1 = rd.unknown2,
                Occurrences = rd.Occurrences,
                FirstAddress = rd.FirstOffset
            });
        }

        public static int[] WriteRefDefs(BBData data, ReferenceDef[] variables, IDictionary<string, int> stringOffsets, bool IsOldBCVersion, bool isFunction)
        {
            if (!IsOldBCVersion && isFunction)
            {
                data.Buffer.Write(variables.Length);
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
                    data.Buffer.Write(new FunctionLocalEntry
                    {
                        Index = i,
                        Name = (uint)stringOffsets[func.LocalNames[i]]
                    });
                }
            }

            return stringOffsetOffsets.ToArray();
        }

        public static void Reassemble(BBData data, Decomp.AnyInstruction[] instructions, uint bytecodeVersion)
        {
            foreach (var inst in instructions)
            {
                var instdata = new BinBuffer();
                instdata.Write(inst);
                uint size;
                unsafe
                {
                    size = Decomp.DisasmExt.Size(&inst, bytecodeVersion)*4;
                }
                data.Buffer.Write(instdata, 0, (int)size, 0);
            }
        }

        private static void WriteCode(BBData data, CodeInfo ci, IDictionary<string, int> stringOffsets, uint bytecodeVersion)
        {
            data.Buffer.Write(stringOffsets[ci.Name]);
            data.Buffer.Write(ci.Size);
            if (bytecodeVersion > 0xE)
            {
                data.Buffer.Write(1);
                data.Buffer.Write(8);
                data.Buffer.Write(0);
            }
            Reassemble(data, ci.InstructionsCopy, bytecodeVersion);
        }

        public static int[] WriteCodes(BBData data, GMFile f, IDictionary<string, int> stringOffsets)
        {
            var offsets = WriteList(data, f.Code, (fd, ci) => WriteCode(fd, ci, stringOffsets, f.General.BytecodeVersion));
            var stringOffsetOffsets = new int[f.Code.Length];
            for (int i = 0; i < f.Code.Length; i++)
            {
                stringOffsetOffsets[i] = offsets[i] + 8;
            }
            return stringOffsetOffsets;
        }
    }
}
