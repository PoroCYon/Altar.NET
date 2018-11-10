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
            Buffer = bb;
            OffsetOffsets = offs;
        }
    }

    public class StringsChunkBuilder : Deserialize.StringsListBuilder
    {
        BBData stringsData;
        IDictionary<string, int> stringOffsets;
        IList<int> offsetList;

        public StringsChunkBuilder()
        {
            stringsData = new BBData(new BinBuffer(), new int[0]);
            stringOffsets = new Dictionary<string, int>();
            offsetList = new List<int>();
        }

        private void WriteString(BBData data, String s)
        {
            data.Buffer.Write(s);
            data.Buffer.WriteByte(0);
        }

        public override int AddString(String s)
        {
            base.AddString(s);
            int offset = stringsData.Buffer.Position;
            WriteString(stringsData, s);
            stringOffsets[s] = offset;
            offsetList.Add(offset);
            return offset;
        }

        public uint GetOffset(String s)
        {
            if (stringOffsets.TryGetValue(s, out int offset))
            {
                return (uint)offset;
            }
            else
            {
                return (uint)AddString(s);
            }
        }

        public int[] WriteStringsChunk(BBData data)
        {
            var bb = data.Buffer;

            bb.Write(offsetList.Count);

            var allOffs = data.OffsetOffsets.ToList();

            var offAcc = bb.Position + offsetList.Count * sizeof(int); // after all offsets
            var offsets = new List<int>(offsetList.Count);
            for (int i = 0; i < offsetList.Count; i++)
            {
                allOffs.Add(bb.Position);
                bb.Write(offsetList[i] + offAcc);
                offsets.Add(offsetList[i] + offAcc);
            }

            bb.Write(stringsData.Buffer);
            allOffs.AddRange(stringsData.OffsetOffsets); // updated by Write

            data.OffsetOffsets = allOffs.ToArray();
            return offsets.ToArray();
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
            var offsets = new List<int>(datas.Length);
            for (int i = 0; i < datas.Length; i++)
            {
                if (datas[i] == null)
                {
                    bb.Write(0xFFFFFFFF);
                }
                else
                {
                    allOffs.Add(bb.Position);
                    bb.Write(offAcc);
                    offsets.Add(offAcc);

                    offAcc += datas[i].Buffer.Size;
                }
            }

            for (int i = 0; i < datas.Length; i++)
            {
                if (datas[i] != null)
                {
                    Write(bb, datas[i]);
                    allOffs.AddRange(datas[i].OffsetOffsets); // updated by Write
                }
            }

            data.OffsetOffsets = allOffs.ToArray();
            return offsets.ToArray();
        }

        public static int[] WriteList<T>(BBData data, T[] things,
            Action<BBData, T> writeThing)
        {
            if (things == null)
            {
                data.Buffer.Write(0xFFFFFFFF);
                return new int[0];
            }
            BBData[] datas = new BBData[things.Length];

            for (int i = 0; i < things.Length; i++)
            {
                if (things[i] != null)
                {
                    BBData thingdata = new BBData(new BinBuffer(), new int[0]);
                    writeThing(thingdata, things[i]);
                    datas[i] = thingdata;
                }
            }

            return WriteList(data, datas);
        }

        public static int[] WriteList<T>(BBData data, T[] things,
            Action<BBData, T, StringsChunkBuilder> writeThing,
            StringsChunkBuilder strings)
        {
            return WriteList(data, things, (thingdata, thing) => writeThing(thingdata, thing, strings));
        }

        public static int[] WriteList<T>(BBData data, T[] things,
            Action<BBData, T, StringsChunkBuilder, int[]> writeThing,
            StringsChunkBuilder strings, int[] texPagOffsets)
        {
            return WriteList(data, things, (thingdata, thing) => writeThing(thingdata, thing, strings, texPagOffsets));
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

        public static int[] WriteGeneral(BBData data, GeneralInfo ge, RoomInfo[] rooms, StringsChunkBuilder strings)
        {
            var ret = new SectionGeneral
            {
                Debug = ge.IsDebug,
                FilenameOffset = strings.GetOffset(ge.FileName),
                ConfigOffset = strings.GetOffset(ge.Configuration),
                NameOffset = strings.GetOffset(ge.Name),
                DisplayNameOffset = strings.GetOffset(ge.DisplayName),
                GameID = ge.GameID,
                WindowSize = ge.WindowSize,
                BytecodeVersion = (Int24)ge.BytecodeVersion,
                Major = ge.Version.Major,
                Minor = ge.Version.Minor,
                Release = ge.Version.Build,
                Build = ge.Version.Revision,

                Info = ge.InfoFlags,
                ActiveTargets = ge.ActiveTargets,
                AppID = ge.SteamAppID,

                LastObj = 0,
                LastTile = 0
            };
            var stringOffsetOffsets = new int[]
            {
                (int)Marshal.OffsetOf(typeof(SectionGeneral), "FilenameOffset") - 3,
                (int)Marshal.OffsetOf(typeof(SectionGeneral), "ConfigOffset") - 3,
                (int)Marshal.OffsetOf(typeof(SectionGeneral), "NameOffset") - 3,
                (int)Marshal.OffsetOf(typeof(SectionGeneral), "DisplayNameOffset") - 3
            };

            foreach (var room in rooms)
            {
                foreach (var obj in room.Objects)
                {
                    if (obj.InstanceID > ret.LastObj)
                    {
                        ret.LastObj = obj.InstanceID;
                    }
                }
                foreach (var tile in room.Tiles)
                {
                    if (tile.InstanceID > ret.LastTile)
                    {
                        ret.LastTile = tile.InstanceID;
                    }
                }
            }
            ret.LastObj++;
            ret.LastTile++;

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

            // TODO for 2.0: some lengthy checksum/hash at the end?
            // exits after launch if GEN8 is modified at all,
            // doesn't launch if the extra stuff is missing
            //for (int i = 0; i < 16; i++)
            //    data.Buffer.Write(0x3F3F3F3F);

            return stringOffsetOffsets;
        }

        public static int[] WriteOptions(BBData data, OptionInfo opt, StringsChunkBuilder strings)
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

            var tmp = new BinBuffer();
            tmp.Write(ret);
            data.Buffer.Write(tmp, 0, tmp.Size - 12, 8);

            if (opt.Constants != null)
            {
                foreach (var kvp in opt.Constants)
                {
                    stringOffsetOffsets.Add(data.Buffer.Position + 8);
                    data.Buffer.Write(strings.GetOffset(kvp.Key));
                    stringOffsetOffsets.Add(data.Buffer.Position + 8);
                    data.Buffer.Write(strings.GetOffset(kvp.Value));
                }
            }

            return stringOffsetOffsets.ToArray();
        }

        public static void Pad(BBData data, int amount, int offset)
        {
            var pad = (amount - 1) - ((data.Buffer.Position + offset - 1) % amount);
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
                //texturedata.Buffer.Write(1); // TextureEntry._pad for 2.0
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
                data.Buffer.Position = offsets[i] + 4; // 8 on 2.0
                secondaryOffsets[i] = data.Buffer.Position;
                data.Buffer.Write(p);
                data.Buffer.Position = p;
                data.Buffer.Write(textures[i].PngData);
            }
            data.OffsetOffsets = data.OffsetOffsets.Concat(secondaryOffsets).ToArray();

            Pad(data, 8, 0);
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

        public static void WriteFont(BBData data, FontInfo fi, StringsChunkBuilder strings, int[] texPagOffsets)
        {
            var fe = new FontEntry
            {
                CodeName = fi.CodeName == null ? 0 : strings.GetOffset(fi.CodeName),
                SystemName = strings.GetOffset(fi.SystemName),
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
            StringsChunkBuilder strings,
            int[] texPagOffsets,
            out int[] stringOffsetOffsets,
            out int[] texpOffsetOffsets)
        {
            int[] offsets = WriteList(data, fonts, WriteFont, strings, texPagOffsets);

            stringOffsetOffsets = new int[fonts.Length * 2];
            texpOffsetOffsets = new int[fonts.Length];

            for (int i = 0; i < fonts.Length; i++)
            {
                stringOffsetOffsets[i * 2] = offsets[i] + (int)Marshal.OffsetOf(typeof(FontEntry), "CodeName") + 8;
                stringOffsetOffsets[i * 2 + 1] = offsets[i] + (int)Marshal.OffsetOf(typeof(FontEntry), "SystemName") + 8;
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
                if (i != waves.Length - 1) Pad(audiodata, 0x4, i == 0 ? 8 + offset : 0);
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

        private static void WriteObject(BBData data, ObjectInfo oi, StringsChunkBuilder strings)
        {
            var oe = new ObjectEntry
            {
                Name = strings.GetOffset(oi.Name),
                SpriteIndex = oi.SpriteIndex,
                Visible = oi.IsVisible ? DwordBool.True : DwordBool.False,
                Solid = oi.IsSolid ? DwordBool.True : DwordBool.False,
                Depth = oi.Depth,
                Persistent = oi.IsPersistent ? DwordBool.True : DwordBool.False,

                ParentId = oi.ParentId == null ? -100 : (int)oi.ParentId,
                MaskId = oi.TexMaskId == null ? -1 : (int)oi.TexMaskId,

                HasPhysics = oi.Physics != null ? DwordBool.True : DwordBool.False,
                IsSensor = oi.IsSensor ? DwordBool.True : DwordBool.False,
                CollisionShape = oi.CollisionShape
            };

            oe.Physics = oi.Physics ?? new ObjectPhysics
            {
                Density = 0.5f,
                Restitution = 0.1f,
                Group = 0,
                LinearDamping = 0.1f,
                AngularDamping = 0.1f,
                Unknown0 = 0,
                Friction = 0.2f,
                Unknown1 = 1,
                Kinematic = 0
            };

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
                BinBuffer tmp = new BinBuffer();
                tmp.Write(oe);
                data.Buffer.Write(tmp.AsByteArray(), 0, (int)Marshal.OffsetOf(typeof(ObjectEntry), "Rest") + (int)Marshal.OffsetOf(typeof(ObjectRest), "ShapePoints_IfMoreFloats"));
            }
            else
            {
                BinBuffer tmp = new BinBuffer();
                tmp.Write(oe);
                data.Buffer.Write(tmp.AsByteArray(), 0, (int)Marshal.OffsetOf(typeof(ObjectEntry), "Rest") + (int)Marshal.OffsetOf(typeof(ObjectRest), "ShapePoints"));
            }
            WriteList(data, oi.ShapePoints, (shapePointData, shapePoint) =>
                WriteList(shapePointData, shapePoint, (pointPointData, pointPoint) =>
                {
                    pointPointData.Buffer.Write(pointPoint[0]);
                    pointPointData.Buffer.Write(pointPoint[1]); // probably count
                    pointPointData.OffsetOffsets = new int[] { pointPointData.Buffer.Position };
                    pointPointData.Buffer.Write(12); // probably offset
                    for (int i = 3; i < pointPoint.Length; i++)
                    {
                        pointPointData.Buffer.Write(pointPoint[i]);
                    }
                })
            );
        }

        public static int[] WriteObjects(BBData data, ObjectInfo[] objects, StringsChunkBuilder strings)
        {
            int[] offsets = WriteList(data, objects, WriteObject, strings);
            var stringOffsetOffsets = new int[objects.Length];

            for (int i = 0; i < objects.Length; i++)
            {
                stringOffsetOffsets[i] = offsets[i] + (int)Marshal.OffsetOf(typeof(ObjectEntry), "Name") + 8;
            }
            return stringOffsetOffsets;
        }

        private static void WriteSound(BBData data, SoundInfo si, StringsChunkBuilder strings, string[] audioGroups)
        {
            var se = new SoundEntry
            {
                NameOffset = si.Name == null ? 0 : strings.GetOffset(si.Name),
                TypeOffset = si.Type == null ? 0 : strings.GetOffset(si.Type),
                FileOffset = si.File == null ? 0 : strings.GetOffset(si.File),

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

        public static int[] WriteSounds(BBData data, SoundInfo[] sounds, StringsChunkBuilder strings, string[] audioGroups)
        {
            int[] offsets = WriteList(data, sounds, (sounddata, sound) => WriteSound(sounddata, sound, strings, audioGroups));
            var stringOffsetOffsets = new List<int>();

            for (int i = 0; i < sounds.Length; i++)
            {
                if (sounds[i].Name != null) stringOffsetOffsets.Add(offsets[i] + (int)Marshal.OffsetOf(typeof(SoundEntry), "NameOffset") + 8);
                if (sounds[i].Type != null) stringOffsetOffsets.Add(offsets[i] + (int)Marshal.OffsetOf(typeof(SoundEntry), "TypeOffset") + 8);
                if (sounds[i].File != null) stringOffsetOffsets.Add(offsets[i] + (int)Marshal.OffsetOf(typeof(SoundEntry), "FileOffset") + 8);
            }
            return stringOffsetOffsets.ToArray();
        }

        public static int[] WriteAudioGroups(BBData data, string[] audioGroups, StringsChunkBuilder strings)
        {
            int[] offsets = WriteList(data, audioGroups, (groupdata, group) => groupdata.Buffer.Write(strings.GetOffset(group)));
            var stringOffsetOffsets = new int[audioGroups.Length];

            for (int i = 0; i < audioGroups.Length; i++)
            {
                stringOffsetOffsets[i] = offsets[i] + 8;
            }
            return stringOffsetOffsets.ToArray();
        }

        private static void WriteSprite(BBData data, SpriteInfo si, StringsChunkBuilder strings, int[] texPagOffsets)
        {
            var se = new SpriteEntry
            {
                Name = strings.GetOffset(si.Name),
                Size = si.Size,
                Bounding = si.Bounding,
                BBoxMode = si.BBoxMode,
                Origin = si.Origin,

                SeparateColMasks = si.SeparateColMasks ? DwordBool.True : DwordBool.False
            };

            var tmp = new BinBuffer();
            tmp.Write(se);
            data.Buffer.Write(tmp, 0, tmp.Size - 8, 0);

            if (si.Version >= 2)
            {
                var se2 = new SpriteEntry2();
                unsafe
                {
                    se2._pad2[0] = -1;
                    se2._pad2[1] = 1;
                    se2._pad2[2] = 0;
                }
                se2.funk = si.UnknownFloat;
                tmp = new BinBuffer();
                tmp.Write(se2);
                data.Buffer.Write(tmp, 0, 0x14, 0x38);
            }

            if (si.TextureIndices == null)
            {
                data.Buffer.Write(0xFFFFFFFF);
            }
            else
            {
                data.Buffer.Write(si.TextureIndices.Length);
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
                    data.Buffer.Write((uint)si.CollisionMasks.Length);
                    foreach (var mask in si.CollisionMasks)
                    {
                        int w = mask.GetLength(0);
                        for (int y = 0; y < mask.GetLength(1); y++)
                        {
                            for (int x = 0; x < w; x += 8)
                            {
                                byte b = 0;
                                if (x < w && mask[x, y]) b |= 0b00000001;
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
            StringsChunkBuilder strings,
            int[] texPagOffsets,
            out int[] stringOffsetOffsets,
            out int[] texpOffsetOffsetsArray)
        {
            int[] offsets = WriteList(data, sprites, WriteSprite, strings, texPagOffsets);
            stringOffsetOffsets = new int[sprites.Length];
            var texpOffsetOffsetsList = new List<int>();
            for (int i = 0; i < sprites.Length; i++)
            {
                stringOffsetOffsets[i] = offsets[i] + (int)Marshal.OffsetOf(typeof(SpriteEntry), "Name") + 8;
                var si = sprites[i];
                if (si.TextureIndices != null)
                {
                    var texIdxOffs = offsets[i] + 12;
                    if (si.Version < 2)
                    {
                        texIdxOffs += (int)Marshal.OffsetOf(typeof(SpriteEntry), "Textures");
                    }
                    else
                    {
                        texIdxOffs += (int)Marshal.OffsetOf(typeof(SpriteEntry2), "Textures");
                    }
                    for (int j = 0; j < si.TextureIndices.Length; j++)
                    {
                        texpOffsetOffsetsList.Add(texIdxOffs + j * sizeof(int));
                    }
                }
            }
            texpOffsetOffsetsArray = texpOffsetOffsetsList.ToArray();
        }

        private static void WriteBackground(BBData data, BackgroundInfo bi, StringsChunkBuilder strings, int[] texPagOffsets)
        {
            data.Buffer.Write(new BgEntry
            {
                Name = strings.GetOffset(bi.Name),
                TextureOffset = bi.TexPageIndex.HasValue ? (uint)texPagOffsets[bi.TexPageIndex.Value] : 0
            });
        }

        public static void WriteBackgrounds(BBData data,
            BackgroundInfo[] backgrounds,
            StringsChunkBuilder strings,
            int[] texPagOffsets,
            out int[] stringOffsetOffsets,
            out int[] texpOffsetOffsetsArray)
        {
            int[] offsets = WriteList(data, backgrounds, WriteBackground, strings, texPagOffsets);
            stringOffsetOffsets = new int[backgrounds.Length];
            var texpOffsetOffsetsList = new List<int>();
            for (int i = 0; i < backgrounds.Length; i++)
            {
                stringOffsetOffsets[i] = offsets[i] + (int)Marshal.OffsetOf(typeof(BgEntry), "Name") + 8;
                if (backgrounds[i].TexPageIndex.HasValue) texpOffsetOffsetsList.Add(offsets[i] + (int)Marshal.OffsetOf(typeof(BgEntry), "TextureOffset") + 8);
            }
            texpOffsetOffsetsArray = texpOffsetOffsetsList.ToArray();
        }

        private static void WritePath(BBData data, PathInfo pi, StringsChunkBuilder strings)
        {
            var tmp = new BinBuffer();
            tmp.Write(new PathEntry
            {
                Name = strings.GetOffset(pi.Name),
                IsSmooth = pi.IsSmooth ? DwordBool.True : DwordBool.False,
                IsClosed = pi.IsClosed ? DwordBool.True : DwordBool.False,
                Precision = pi.Precision,

                PointCount = (uint)pi.Points.Length
            });
            data.Buffer.Write(tmp, 0, tmp.Size - 12, 0);

            foreach (var pt in pi.Points)
                data.Buffer.Write(pt);
        }

        public static int[] WritePaths(BBData data, PathInfo[] paths, StringsChunkBuilder strings)
        {
            int[] offsets = WriteList(data, paths, WritePath, strings);
            var stringOffsetOffsets = new int[paths.Length];

            for (int i = 0; i < paths.Length; i++)
            {
                stringOffsetOffsets[i] = offsets[i] + (int)Marshal.OffsetOf(typeof(PathEntry), "Name") + 8;
            }
            return stringOffsetOffsets;
        }

        private static void WriteScript(BBData data, ScriptInfo si, StringsChunkBuilder strings)
        {
            data.Buffer.Write(new ScriptEntry
            {
                Name = strings.GetOffset(si.Name),
                CodeId = si.CodeId
            });
        }

        public static int[] WriteScripts(BBData data, ScriptInfo[] scripts, StringsChunkBuilder strings)
        {
            int[] offsets = WriteList(data, scripts, WriteScript, strings);
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

        private static void WriteRoomObjInst(BBData data, RoomObjInst roi, StringsChunkBuilder strings)
        {
            data.Buffer.Write(new RoomObjInstEntry
            {
                Index = roi.Index,
                Unk1  = roi.Unk1 ,
                Depth = roi.Depth,
                Unk3  = roi.Unk3 ,
                InstCount = (uint)roi.Instances.Length,
                Name = strings.GetOffset(roi.Name)
            });
            data.Buffer.Position -= 4;
            foreach (var id in roi.Instances)
            {
                data.Buffer.Write(id);
            }
        }

        private static void WriteRoom(BBData data, RoomInfo ri, StringsChunkBuilder strings)
        {
            var re = new RoomEntry
            {
                Name = strings.GetOffset(ri.Name),
                Caption = ri.Caption == null ? 0 : strings.GetOffset(ri.Caption),
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
            if (ri.EnableViews) re.Flags |= RoomEntryFlags.EnableViews;
            if (ri.ShowColour) re.Flags |= RoomEntryFlags.ShowColour;
            if (ri.ClearDisplayBuffer) re.Flags |= RoomEntryFlags.ClearDisplayBuffer;
            if (ri.UnknownFlag) re.Flags |= RoomEntryFlags.Unknown;

            var bgOffsetOffset = (int)Marshal.OffsetOf(typeof(RoomEntry), "BgOffset");
            var viewOffsetOffset = (int)Marshal.OffsetOf(typeof(RoomEntry), "ViewOffset");
            var objOffsetOffset = (int)Marshal.OffsetOf(typeof(RoomEntry), "ObjOffset");
            var tileOffsetOffset = (int)Marshal.OffsetOf(typeof(RoomEntry), "TileOffset");
            int objInstOffsetOffset = (int)Marshal.OffsetOf(typeof(RoomEntry), "MetresPerPixel") + 4;

            data.Buffer.Write(re);
            data.OffsetOffsets = new int[] { bgOffsetOffset, viewOffsetOffset, objOffsetOffset, tileOffsetOffset };

            if (ri.ObjInst != null)
            {
                data.Buffer.Write(0);
                data.OffsetOffsets = data.OffsetOffsets.Concat(new int[] { objInstOffsetOffset }).ToArray();
            }

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

            if (ri.ObjInst != null)
            {
                data.Buffer.Position = objInstOffsetOffset;
                data.Buffer.Write(data.Buffer.Size);
                data.Buffer.Position = data.Buffer.Size;
                WriteList(data, ri.ObjInst, WriteRoomObjInst, strings);
            }
            
            // Unknown stuff for 2.0
            //for (int i = 0; i < 8; i++)
            //    data.Buffer.Write(0x3F3F3F3F);
        }

        public static int[] WriteRooms(BBData data, RoomInfo[] rooms, StringsChunkBuilder strings)
        {
            int[] offsets = WriteList(data, rooms, WriteRoom, strings);
            var stringOffsetOffsets = new List<int>();
            for (int i = 0; i < rooms.Length; i++)
            {
                stringOffsetOffsets.Add(offsets[i] + (int)Marshal.OffsetOf(typeof(RoomEntry), "Name") + 8);
                if (rooms[i].Caption != null) stringOffsetOffsets.Add(offsets[i] + (int)Marshal.OffsetOf(typeof(RoomEntry), "Caption") + 8);
                if (rooms[i].ObjInst != null)
                {
                    data.Buffer.Position = offsets[i];
                    data.Buffer.Position += (int)Marshal.OffsetOf(typeof(RoomEntry), "MetresPerPixel") + 4;
                    int listoff = data.Buffer.ReadInt32();
                    data.Buffer.Position = listoff;
                    listoff += 4;
                    int count = data.Buffer.ReadInt32();
                    for (int j = 0; j < count; j++)
                    {
                        data.Buffer.Position = listoff + j * 4;
                        int off = data.Buffer.ReadInt32();
                        stringOffsetOffsets.Add(off + (int)Marshal.OffsetOf(typeof(RoomObjInstEntry), "Name") + 8);
                    }
                }
            }
            return stringOffsetOffsets.ToArray();
        }

        private static void WriteShader(BBData data, ShaderInfo si, StringsChunkBuilder strings)
        {
            var se = new ShaderEntry
            {
                Name = strings.GetOffset(si.Name),
                VertexAttributeCount = (uint)si.VertexAttributes.Length,
                UnknownFlags = 0x80000001
            };
            unsafe
            {
                for (int i = 0; i < si.Sources.Length; i++)
                {
                    se.Sources[i] = strings.GetOffset(si.Sources[i]);
                }
            }
            var tmp = new BinBuffer();
            tmp.Write(se);
            data.Buffer.Write(tmp, 0, tmp.Size - 4, 0);
            foreach (var attr in si.VertexAttributes)
            {
                data.Buffer.Write(strings.GetOffset(attr));
            }
            data.Buffer.Write(2); // Unknown
            for (int i = 0; i < 12; i++)
            {
                data.Buffer.Write(0);
            }
        }

        public static int[] WriteShaders(BBData data, ShaderInfo[] shaders, StringsChunkBuilder strings)
        {
            int[] offsets = WriteList(data, shaders, WriteShader, strings);
            var stringOffsetOffsets = new List<int>(shaders.Length);
            for (int i = 0; i < shaders.Length; i++)
            {
                stringOffsetOffsets.Add(offsets[i] + (int)Marshal.OffsetOf(typeof(ShaderEntry), "Name") + 8);
                for (int j = 0; j < shaders[i].Sources.Length; j++)
                {
                    stringOffsetOffsets.Add(offsets[i] + (int)Marshal.OffsetOf(typeof(ShaderEntry), "Sources") + 8 + j * sizeof(uint));
                }
                for (int j = 0; j < shaders[i].VertexAttributes.Length; j++)
                {
                    stringOffsetOffsets.Add(offsets[i] + (int)Marshal.OffsetOf(typeof(ShaderEntry), "VertexAttribute") + 8 + j * sizeof(uint));
                }
            }
            return stringOffsetOffsets.ToArray();
        }

        private static void WriteRefDef(BBData data, ReferenceDef rd, StringsChunkBuilder strings)
        {
            data.Buffer.Write(new RefDefEntry
            {
                NameOffset = strings.GetOffset(rd.Name),
                Occurrences = rd.Occurrences,
                FirstAddress = rd.FirstOffset
            });
        }

        private static void WriteRefDefWithOthers(BBData data, ReferenceDef rd, StringsChunkBuilder strings)
        {
            data.Buffer.Write(new RefDefEntryWithOthers
            {
                NameOffset = strings.GetOffset(rd.Name),
                InstanceType = (int)rd.InstanceType,
                _pad1 = rd.unknown2,
                Occurrences = rd.Occurrences,
                FirstAddress = rd.FirstOffset
            });
        }

        public static void WriteRefDefs(BBData data, ReferenceDef[] variables,
            StringsChunkBuilder strings,
            bool IsOldBCVersion, bool isFunction,
            out int[] stringOffsetOffsets, out int[] codeOffsetOffsets)
        {
            if (!IsOldBCVersion && isFunction)
            {
                data.Buffer.Write(variables.Length);
            }
            stringOffsetOffsets = new int[variables.Length];
            var codeOffsetOffsetsList = new List<int>(variables.Length);
            for (int i = 0; i < variables.Length; i++)
            {
                if (IsOldBCVersion || isFunction)
                {
                    stringOffsetOffsets[i] = data.Buffer.Position + (int)Marshal.OffsetOf(typeof(RefDefEntry), "NameOffset") + 8;
                    if (variables[i].FirstOffset != 0xFFFFFFFF)
                        codeOffsetOffsetsList.Add(data.Buffer.Position + (int)Marshal.OffsetOf(typeof(RefDefEntry), "FirstAddress") + 8);
                    WriteRefDef(data, variables[i], strings);
                }
                else
                {
                    stringOffsetOffsets[i] = data.Buffer.Position + (int)Marshal.OffsetOf(typeof(RefDefEntryWithOthers), "NameOffset") + 8;
                    if (variables[i].FirstOffset != 0xFFFFFFFF)
                        codeOffsetOffsetsList.Add(data.Buffer.Position + (int)Marshal.OffsetOf(typeof(RefDefEntryWithOthers), "FirstAddress") + 8);
                    WriteRefDefWithOthers(data, variables[i], strings);
                }
            }
            codeOffsetOffsets = codeOffsetOffsetsList.ToArray();
        }

        public static int[] WriteFunctionLocals(BBData data, FunctionLocalsInfo[] functions, StringsChunkBuilder strings)
        {
            var stringOffsetOffsets = new List<int>();

            data.Buffer.Write(functions.Length);

            foreach (var func in functions)
            {
                data.Buffer.Write((uint)func.LocalNames.Length);
                stringOffsetOffsets.Add(data.Buffer.Position + 8);
                data.Buffer.Write(strings.GetOffset(func.FunctionName));
                for (uint i = 0; i < func.LocalNames.Length; i++)
                {
                    stringOffsetOffsets.Add(data.Buffer.Position + 12);
                    data.Buffer.Write(new FunctionLocalEntry
                    {
                        Index = i,
                        Name = strings.GetOffset(func.LocalNames[i])
                    });
                }
            }

            return stringOffsetOffsets.ToArray();
        }
    }
}
