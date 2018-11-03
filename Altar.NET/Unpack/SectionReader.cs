using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Altar.Unpack
{
    public unsafe static class SectionReader
    {
        // http://undertale.rawr.ws/unpacking
        // https://www.reddit.com/r/Underminers/comments/3teemm/wip_documenting_stringstxt/
        // https://gitlab.com/snippets/14944

        static float[] EmptyFloatArr = { };

        static long PngLength(PngHeader* png)
        {
#pragma warning disable RECS0065
            if (png == null)
                return 0L;
#pragma warning restore RECS0065

            var chunk = &png->IHDR.Header;

            while (chunk->Type != PngChunk.ChunkEnd)
            {
                if (chunk->Length == 0)
                    return 0L;

                chunk = (PngChunk*)((byte*)chunk + Utils.SwapEnd32(chunk->Length) + 0xC);
            }

            return (long)++chunk + 0x4 - (long)png;
        }

        internal static string ReadString(byte* ptr)
        {
            var length = *(UInt32*)ptr;
            byte[] bytes = new byte[length];
            for (int i = 0; i < length; i++)
            {
                bytes[i] = *(ptr + 4 + i);
            }
            return Encoding.UTF8.GetString(bytes);
        }
        internal static string StringFromOffset(GMFileContent content, long off)
        {
            if (off == 0 || (off & 0xFFFFFF00) == 0xFFFFFF00 /* avoid crashes due
                                                                to bogus offsets */)
                return null;

            return ReadString((byte*)GMFile.PtrFromOffset(content, off-4));
        }

        static long IndexOfUnsafe(uint* arr, uint arrlen, uint value)
        {
            for (uint i = 0; i < arrlen; i++)
                if (arr[i] == value)
                    return i;

            return -1L;
        }

        static T[] ReadList<T>(GMFileContent content, CountOffsetsPair* list, Func<GMFileContent, IntPtr, T> readThing)
        {
            if (readThing == null)
                throw new ArgumentNullException(nameof(readThing));

            var len = list->Count;
            var ret = new T[len];

            var addresses = &list->Offsets;

            for (uint i = 0; i < len; i++)
                ret[i] = readThing(content, (IntPtr)GMFile.PtrFromOffset(content, addresses[i]));

            return ret;
        }

        static RoomBackground ReadRoomBg  (GMFileContent content, IntPtr p)
        {
            var entry = (RoomBgEntry*)p;

            var b = new RoomBackground();

            b.IsEnabled     = entry->IsEnabled.IsTrue()   ;
            b.IsForeground  = entry->IsForeground.IsTrue();
            b.Position      = entry->Position             ;
            b.TileX         = entry->TileX.IsTrue()       ;
            b.TileY         = entry->TileY.IsTrue()       ;
            b.Speed         = entry->Speed                ;
            b.StretchSprite = entry->Stretch.IsTrue()     ;

            b.BgIndex = entry->DefIndex == 0xFFFFFFFF ? null : (uint?)entry->DefIndex;

            return b;
        }
        static RoomView       ReadRoomView(GMFileContent content, IntPtr p)
        {
            var entry = (RoomViewEntry*)p;

            var v = new RoomView();

            v.IsEnabled = entry->IsEnabled.IsTrue();
            v.Port      = entry->Port  ;
            v.View      = entry->View  ;
            v.Border    = entry->Border;
            v.Speed     = entry->Speed ;

            v.ObjectId = entry->ObjectId == 0xFFFFFFFF ? null : (uint?)entry->ObjectId;

            return v;
        }
        static RoomObject     ReadRoomObj (GMFileContent content, IntPtr p)
        {
            var entry = (RoomObjEntry*)p;

            var o = new RoomObject();

            o.DefIndex = entry->DefIndex;
            o.Position = entry->Position;
            o.Scale    = entry->Scale   ;
            o.Colour   = entry->Colour  ;
            o.Rotation = entry->Rotation;

            o.InstanceID   = entry->InstanceID  ;
            o.CreateCodeID = entry->CreateCodeID;

            return o;
        }
        static RoomTile       ReadRoomTile(GMFileContent content, IntPtr p)
        {
            var entry = (RoomTileEntry*)p;

            var t = new RoomTile();

            t.DefIndex       = entry->DefIndex;
            t.Position       = entry->Position;
            t.SourcePosition = entry->SourcePos;
            t.Size           = entry->Size;
            t.Scale          = entry->Scale;
            t.Colour         = entry->Colour;

            t.Depth      = entry->TileDepth ;
            t.InstanceID = entry->InstanceID;

            return t;
        }
        static RoomObjInst ReadRoomObjInst(GMFileContent content, IntPtr p)
        {
            var entry = (RoomObjInstEntry*)p;

            var oi = new RoomObjInst();

            oi.Index   = entry->Index;
            oi.Name    = StringFromOffset(content, entry->Name);
            oi.Unk1    = entry->Unk1;
            oi.Unk2    = entry->Unk2;
            oi.Unk3    = entry->Unk3;

            oi.Instances = new uint[entry->InstCount];
            for (uint i = 0; i < oi.Instances.Length; i++)
            {
                oi.Instances[i] = (&entry->Instances)[i];
            }

            return oi;
        }

        public static GeneralInfo GetGeneralInfo(GMFileContent content)
        {
            var ret = new GeneralInfo();

            var ge = content.General;

            ret.IsDebug         = ge->Debug;
            ret.FileName        = StringFromOffset(content, ge->FilenameOffset   );
            ret.Configuration   = StringFromOffset(content, ge->ConfigOffset     );
            ret.Name            = StringFromOffset(content, ge->NameOffset       );
            ret.DisplayName     = StringFromOffset(content, ge->DisplayNameOffset);
            ret.GameID          = ge->GameID;
            ret.WindowSize      = ge->WindowSize;
            ret.BytecodeVersion = ge->BytecodeVersion;
            ret.Version         = new Version(ge->Major, ge->Minor, ge->Release, ge->Build);

            ret.InfoFlags     = ge->Info         ;
            ret.ActiveTargets = ge->ActiveTargets;
            ret.SteamAppID    = ge->AppID        ;

            ret.unknown = new uint[4];
            for (uint i = 0; i < 4; i++)
                ret.unknown[i] = ge->_unknown[i];

            ret.LicenseMD5Hash = new byte[0x10];
            Marshal.Copy((IntPtr)ge->MD5, ret.LicenseMD5Hash, 0, 0x10);
            ret.LicenceCRC32 = ge->CRC32;

            ret.Timestamp = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(ge->Timestamp);

            ret.WeirdNumbers = new uint[ge->NumberCount];
            for (uint i = 0; i < ge->NumberCount; i++)
                ret.WeirdNumbers[i] = (&ge->Numbers)[i];

            return ret;
        }
        public static OptionInfo  GetOptionInfo (GMFileContent content)
        {
            var ret = new OptionInfo();

            var oe = content.Options;

            ret.InfoFlags = oe->GEN8FlagsDup;

            ret.Constants    = new Dictionary<string, string>((int)oe->ConstMap.Count);
            for (uint i = 0; i < oe->ConstMap.Count; i++)
            {
                uint inoff = (&oe->ConstMap.Offsets)[ i << 1   ];
                uint ouoff = (&oe->ConstMap.Offsets)[(i << 1)|1];
                //Console.Error.Write(inoff.ToString("X") + " -> " + ouoff.ToString("X"));
                string instr = StringFromOffset(content, inoff);
                string oustr = StringFromOffset(content, ouoff);
                //Console.Error.WriteLine(": " + instr + " -> " + oustr);
                ret.Constants   [instr] = oustr;
            }

            ret._pad0 = new uint[2] { oe->_pad0[0], oe->_pad0[1] };
            ret._pad1 = new uint[12] { oe->_pad1[0], oe->_pad1[1], oe->_pad1[2], oe->_pad1[3],
                oe->_pad1[4], oe->_pad1[5], oe->_pad1[6], oe->_pad1[7],
                oe->_pad1[8], oe->_pad1[9], oe->_pad1[10], oe->_pad1[11] };

            return ret;
        }

        public static SoundInfo       GetSoundInfo  (GMFileContent content, uint id)
        {
            if (id >= content.Sounds->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var se = (SoundEntry*)GMFile.PtrFromOffset(content, (&content.Sounds->Offsets)[id]);

            var ret = new SoundInfo();

            ret.Name = StringFromOffset(content, se->NameOffset);
            ret.Type = StringFromOffset(content, se->TypeOffset);
            ret.File = StringFromOffset(content, se->FileOffset);

            ret.VolumeMod = se->Volume;
            ret.PitchMod  = se->Pitch ;

            ret.Group =
                ~se->GroupID < 0
                    ? String.Empty
                    : GetAudioGroupInfo(content, (uint)se->GroupID);

            ret.GroupID      =  se->GroupID;
            ret.AudioID      =  se->AudioID;
            ret.IsEmbedded   = (se->Flags & SoundEntryFlags.Embedded  ) != 0;
            ret.IsCompressed = (se->Flags & SoundEntryFlags.Compressed) != 0;

            return ret;
        }
        public static Dictionary<uint, uint> BuildTPAGOffsetIndexLUT(GMFileContent content)
        {
            var r = new Dictionary<uint, uint>((int)content.TexturePages->Count);

            for (uint i = 0; i < content.TexturePages->Count; ++i)
                r.Add((&content.TexturePages->Offsets)[i], i);

            return r;
        }
        public static SpriteInfo      GetSpriteInfo (GMFileContent content, uint id, Dictionary<uint, uint> toil = null)
        {
            if (id >= content.Sprites->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var se = (SpriteEntry*)GMFile.PtrFromOffset(content, (&content.Sprites->Offsets)[id]);

            var ret = new SpriteInfo();

            ret.Name     = StringFromOffset(content, se->Name);
            ret.Size     = se->Size    ;
            ret.Bounding = se->Bounding;
            ret.BBoxMode = se->BBoxMode;
            ret.Origin   = se->Origin  ;
            ret.Version  = 1;

            ret.SeparateColMasks = se->SeparateColMasks.IsTrue();

            var tex = &se->Textures;
            if (tex->Count == ~(uint)0)
            {
                var se2 = (SpriteEntry2*)GMFile.PtrFromOffset(content, (&content.Sprites->Offsets)[id]);
                tex = &se2->Textures;
                ret.Version = 2;
                ret.UnknownFloat = se2->funk;
            }
            if (tex->Count == ~(uint)0)
            {
                Console.Error.WriteLine($"Warning: texture count of sprite {id} ({((ulong)se-(ulong)content.RawData.BPtr).ToString(SR.HEX_FM8)}) is -1.");
                return ret;
            }

            ret.TextureIndices = new uint[tex->Count];

            for (uint i = 0; i < tex->Count; i++)
                if (toil == null)
                {
                    for (uint j = 0; j < content.TexturePages->Count; j++)
                        if ((&tex->Offsets)[i] == (&content.TexturePages->Offsets)[j])
                        {
                            ret.TextureIndices[i] = j;
                            break;
                        }
                }
                else ret.TextureIndices[i] = toil[(&tex->Offsets)[i]];

            SpriteCollisionMask* masks =
                (SpriteCollisionMask*)((ulong)&tex->Offsets + sizeof(uint) * tex->Count);

            uint amt = ret.SeparateColMasks ? masks->MaskCount : 1;
            //Console.WriteLine("amt="+amt.ToString(SR.HEX_FM8) + " at " + ((ulong)&masks->MaskCount - (ulong)content.RawData.BPtr).ToString(SR.HEX_FM8));

            if (amt < 0x100) // guesstimate
            {
                ret.CollisionMasks = new bool[amt][,];
                byte* maskData = &masks->MaskData;

                uint w = (uint)(ret.Size.X & 0x7FFFFFFF);
                uint h = (uint)(ret.Size.Y & 0x7FFFFFFF);

                uint wPad = (((w & 7) == 0) ? w : (w - (w & 7) + 8))/8;

                for (uint i = 0; i < amt; i++)
                {
                    bool[,] stuff = new bool[wPad*8, h];

                    for (uint y = 0; y < h; y++)
                    {
                        for (uint x = 0; x < wPad*8; x++)
                        {
                            uint rown = y * wPad;

                            uint byten = x >> 3;
                            byte bitn = (byte)(x & 7);

                            byte* curptr = maskData + rown + byten;
                            byte curbyte = *curptr;
                            byte curbit = (byte)(curbyte & (byte)(1 << bitn));

                            stuff[x, y] = curbit != 0;
                        }
                    }

                    ret.CollisionMasks[i] = stuff;
                    maskData += wPad * h;
                }
            }
            else
                Console.Error.WriteLine($"Warning: collision mask of sprite {id} ({((ulong)se - (ulong)content.RawData.BPtr).ToString(SR.HEX_FM8)}) is bogus ({amt.ToString(SR.HEX_FM8)}), ignoring...");

            return ret;
        }
        public static BackgroundInfo  GetBgInfo     (GMFileContent content, uint id)
        {
            if (id >= content.Backgrounds->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var ret = new BackgroundInfo();

            var be = (BgEntry*)GMFile.PtrFromOffset(content, (&content.Backgrounds->Offsets)[id]);

            ret.Name         = StringFromOffset(content, be->Name);

            for (uint i = 0; i < content.TexturePages->Count; i++)
                if (be->TextureOffset == (&content.TexturePages->Offsets)[i])
                {
                    ret.TexPageIndex = i;
                    break;
                }

            return ret;
        }
        public static PathInfo        GetPathInfo   (GMFileContent content, uint id)
        {
            if (id >= content.Paths->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var curOff = (&content.Paths->Offsets)[id];
            var pe = (PathEntry*)GMFile.PtrFromOffset(content, curOff);

            var ret = new PathInfo();

            ret.Name      = StringFromOffset(content, pe->Name);
            ret.IsSmooth  = pe->IsSmooth.IsTrue();
            ret.IsClosed  = pe->IsClosed.IsTrue();
            ret.Precision = pe->Precision;

            ret.Points = new PathPoint[pe->PointCount];
            for (uint i = 0; i < pe->PointCount; i++)
                ret.Points[i] = (&pe->Points)[i];

            return ret;
        }
        public static ScriptInfo      GetScriptInfo (GMFileContent content, uint id)
        {
            if (id >= content.Scripts->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var se = (ScriptEntry*)GMFile.PtrFromOffset(content, (&content.Scripts->Offsets)[id]);

            var ret = new ScriptInfo();

            ret.Name   = StringFromOffset(content, se->Name);
            ret.CodeId = se->CodeId;

            return ret;
        }
        public static FontInfo        GetFontInfo   (GMFileContent content, uint id)
        {
            if (id >= content.Fonts->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var fe = (FontEntry*)GMFile.PtrFromOffset(content, (&content.Fonts->Offsets)[id]);

            var ret = new FontInfo();

            ret.CodeName     = StringFromOffset(content, fe->CodeName  );
            ret.SystemName   = StringFromOffset(content, fe->SystemName);
            ret.EmSize       = fe->EmSize;
            ret.IsBold       = fe->Bold  .IsTrue();
            ret.IsItalic     = fe->Italic.IsTrue();
            ret.Charset      = fe->Charset;
            ret.AntiAliasing = fe->AntiAliasing;
            ret.Scale        = fe->Scale;

            for (uint i = 0; i < content.TexturePages->Count; i++)
                if (fe->TPagOffset == (&content.TexturePages->Offsets)[i])
                {
                    ret.TexPagId = i;
                    break;
                }

            ret.Characters = ReadList(content, &fe->Chars, (_, p) =>
            {
                var entry = (FontCharEntry*)p;

                var c = new FontCharacter();

                c.Character        = entry->Character  ;
                c.TPagFrame        = entry->TexPagFrame;
                c.Shift            = entry->Shift      ;
                c.Offset           = entry->Offset     ;

                return c;
            });

            return ret;
        }
        public static ObjectInfo      GetObjectInfo (GMFileContent content, uint id, bool nameonly = false)
        {
            if (id >= content.Objects->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var ret = new ObjectInfo();

            var oe = (ObjectEntry*)GMFile.PtrFromOffset(content, (&content.Objects->Offsets)[id]);

            ret.Name         = StringFromOffset(content, oe->Name);
            ret.SpriteIndex  = oe->SpriteIndex;
            ret.IsVisible    = oe->Visible.IsTrue();
            ret.IsSolid      = oe->Solid  .IsTrue();
            ret.Depth        = oe->Depth;
            ret.IsPersistent = oe->Persistent.IsTrue();

            ret.ParentId  = oe->ParentId < 0 ? null : (uint?)oe->ParentId;
            ret.TexMaskId = oe->MaskId   < 0 ? null : (uint?)oe->MaskId  ;

            ret.Physics        = oe->HasPhysics.IsTrue() ? (ObjectPhysics?)oe->Physics : null;
            ret.IsSensor       = oe->IsSensor.IsTrue();
            ret.CollisionShape = oe->CollisionShape;

            var hasMore  = oe->Rest.ShapePoints.Count > 0x00FFFFFF; // good enough for now
            var shapeCop = hasMore ? &oe->Rest.ShapePoints_IfMoreFloats : &oe->Rest.ShapePoints;

            if (nameonly)
                return ret;

            if (hasMore)
            {
                ret.OtherFloats = new float[4];

                Marshal.Copy((IntPtr)(oe->Rest.MoreFloats), ret.OtherFloats, 0, 4);
            }
            else
                ret.OtherFloats = EmptyFloatArr;

            if ((shapeCop->Count & 0xFFFFF000) != 0)
            {
                Console.Error.WriteLine($"Warning: shape point coords of object {id} are bogus, ignoring...");

                ret.ShapePoints = null;
            }
            else
            {
                ret.ShapePoints = new int[shapeCop->Count][][];

                for (uint i = 0; i < (shapeCop->Count); i++)
                {
                    uint shapePointOff = (&shapeCop->Offsets)[i];

                    uint* shapePointPtr = (uint*)GMFile.PtrFromOffset(content, shapePointOff);

                  //Console.WriteLine(((IntPtr)xoff).ToString(SR.HEX_FM8) + SR.SPACE_S + ((IntPtr)yoff).ToString(SR.HEX_FM8));
                    if ((shapePointOff & 0xFF000000) != 0 || shapePointPtr == null)
                    {
                        Console.Error.WriteLine($"Warning: shape point coord {i} of object {id} is bogus, ignoring...");

                        continue;
                    }

                    uint shapePointPointCount = *shapePointPtr;
                    uint* shapePointPointOffsets = shapePointPtr+1;

                    ret.ShapePoints[i] = new int[shapePointPointCount][];

                    for (uint j = 0; j < shapePointPointCount; j++)
                    {
                        uint shapePointPointOff = shapePointPointOffsets[j];
                        int* shapePointPointPtr = (int*)GMFile.PtrFromOffset(content, shapePointPointOff);
                        // TODO: fixed size, but mostly the same across
                        // entries. Probably structure of some sort.
                        // Index 1/2 look like count/offset, but count is
                        // always 1 and offset is always +4.
                        uint pointpointlen = 17;
                        ret.ShapePoints[i][j] = new int[pointpointlen];
                        for (uint k = 0; k < ret.ShapePoints[i][j].Length; k++)
                        {
                            ret.ShapePoints[i][j][k] = shapePointPointPtr[k];
                        }
                    }
                }
            }

            return ret;
        }
        public static RoomInfo        GetRoomInfo   (GMFileContent content, uint id)
        {
            if (id >= content.Rooms->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var ret = new RoomInfo();

            var re = (RoomEntry*)GMFile.PtrFromOffset(content, (&content.Rooms->Offsets)[id]);

            ret.Name         = StringFromOffset(content, re->Name   );
            ret.Caption      = StringFromOffset(content, re->Caption);
            ret.Size         = re->Size          ;
            ret.Speed        = re->Speed         ;
            ret.IsPersistent = re->Persistent.IsTrue();
            ret.Colour       = re->Colour        ;

            ret.DrawBackgroundColour = re->DrawBackgroundColour.IsTrue();
            ret._unknown = re->_unknown;

            ret.EnableViews        = (re->Flags & RoomEntryFlags.EnableViews       ) != 0;
            ret.ShowColour         = (re->Flags & RoomEntryFlags.ShowColour        ) != 0;
            ret.ClearDisplayBuffer = (re->Flags & RoomEntryFlags.ClearDisplayBuffer) != 0;
            ret.UnknownFlag        = (re->Flags & RoomEntryFlags.Unknown           ) != 0;

            ret.World          = re->World         ;
            ret.Bounding       = re->Bounding      ;
            ret.Gravity        = re->Gravity       ;
            ret.MetresPerPixel = re->MetresPerPixel;

            ret.Backgrounds = ReadList(content, (CountOffsetsPair*)GMFile.PtrFromOffset(content, re->BgOffset  ), ReadRoomBg  );
            ret.Views       = ReadList(content, (CountOffsetsPair*)GMFile.PtrFromOffset(content, re->ViewOffset), ReadRoomView);
            ret.Objects     = ReadList(content, (CountOffsetsPair*)GMFile.PtrFromOffset(content, re->ObjOffset ), ReadRoomObj );
            ret.Tiles       = ReadList(content, (CountOffsetsPair*)GMFile.PtrFromOffset(content, re->TileOffset), ReadRoomTile);

            if (re->BgOffset != (&content.Rooms->Offsets)[id]+sizeof(RoomEntry))
            {
                // heuristic to find instance data
                uint instanceListOffset = *(uint*)GMFile.PtrFromOffset(content, (&content.Rooms->Offsets)[id]+sizeof(RoomEntry));
                if (instanceListOffset > (&content.Rooms->Offsets)[id] && (instanceListOffset&0xFF000000) != 0xFF000000)
                {
                    ret.ObjInst = ReadList(content, (CountOffsetsPair*)GMFile.PtrFromOffset(content, instanceListOffset), ReadRoomObjInst);
                }
            }

            // TODO: There's an extra 32 bytes at the end of each room entry in
            // JetBoy, and they are important! I got a different background
            // color and some motion blur effects when I set them to dummy
            // values.
            // background color is 16 bytes in

            return ret;
        }
        public static TexturePageInfo GetTexPageInfo(GMFileContent content, uint id)
        {
            if (id >= content.TexturePages->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var tpe = (TexPageEntry*)GMFile.PtrFromOffset(content, (&content.TexturePages->Offsets)[id]);

            var ret = new TexturePageInfo();

            ret.Source        = tpe->Source       ;
            ret.Destination   = tpe->Dest         ;
            ret.Size          = tpe->Size         ;
            ret.SpritesheetId = tpe->SpritesheetId;

            return ret;
        }
        public static TextureInfo     GetTextureInfo(GMFileContent content, uint id)
        {
            if (id >= content.Textures->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var teOffs = (&content.Textures->Offsets)[id];
            var te = (TextureEntry*)GMFile.PtrFromOffset(content, teOffs);
            uint off = te->Offset;
            if (off == 0)
            {
                var te2 = (TextureEntry2*)GMFile.PtrFromOffset(content,
                        teOffs);
                off = te2->Offset;
            }
            if (off == 0)
            {
                Console.Error.WriteLine($"Warning: texture {id} has no PNG data.");
                return default(TextureInfo);
            }

            var ret = new TextureInfo();

            var png = (PngHeader*)GMFile.PtrFromOffset(content, off);

            ret.Width  = Utils.SwapEnd32(png->IHDR.Width );
            ret.Height = Utils.SwapEnd32(png->IHDR.Height);

            ret.PngData = new byte[PngLength(png)];

            Marshal.Copy((IntPtr)png, ret.PngData, 0, ret.PngData.Length);

            return ret;
        }
        public static AudioInfo       GetAudioInfo  (GMFileContent content, uint id)
        {
            if (id >= content.Audio->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var au = (AudioEntry*)GMFile.PtrFromOffset(content, (&content.Audio->Offsets)[id]);

            var ret = new AudioInfo();

            ret.Wave = new byte[au->Length];

            Marshal.Copy((IntPtr)(&au->Data), ret.Wave, 0, ret.Wave.Length);

            return ret;
        }
        public static string          GetStringInfo (GMFileContent content, uint id)
        {
            if (id >= content.Strings->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var se = (StringEntry*)GMFile.PtrFromOffset(content, (&content.Strings->Offsets)[id]-4);

            return ReadString(&se->Data);
        }
        public static string GetAudioGroupInfo(GMFileContent content, uint id)
        {
            if (id >= content.AudioGroup->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var ag = GMFile.PtrFromOffset(content, (&content.AudioGroup->Offsets)[id]);

            return StringFromOffset(content, *(uint*)ag); // it's just a name
        }
        public static byte[] GetAgrpAudo(AGRPFileContent c,uint id){
            if (id >= c.Audo->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var au=(AudioEntry*)AGRPFile.PtrFromOffset(c,(&c.Audo->Offsets)[id]);

            byte[] ret = new byte[au->Length];

            Marshal.Copy((IntPtr)(&au->Data), ret, 0, ret.Length);

            return ret;
        }

        public static byte[][] ListToByteArrays(GMFileContent content, SectionCountOffsets* list, long elemLen = 0)
        {
            var ret = new byte[list->Count][];

            for (uint i = 0; i < list->Count; i++)
            {
                var  curOff = (&list->Offsets)[i];
                var nextOff = i == list->Count - 1L ? list->Header.Size - 4L : (&list->Offsets)[i + 1];

                var curPtr = (byte*)GMFile.PtrFromOffset(content,  curOff);
                var len    = elemLen <= 0L ? ((byte*)GMFile.PtrFromOffset(content, nextOff) - curPtr) : elemLen;
                if (len < 0L && elemLen < 0L)
                    len = -elemLen;

                var data = new byte[len];

                Marshal.Copy((IntPtr)curPtr, data, 0, (int)len);

                ret[i] = data;
            }

            return ret;
        }

        public static byte[] ToByteArrayData    (SectionHeader* section)
        {
            var ret = new byte[section->Size];

            Marshal.Copy((IntPtr)(section + 2), ret, 0, ret.Length);

            return ret;
        }
        public static byte[] ToByteArrayComplete(SectionHeader* section)
        {
            var ret = new byte[section->Size];

            Marshal.Copy((IntPtr)section, ret, 0, ret.Length);

            return ret;
        }

        // C# doesn't like pointers of generic types...
        static ReferenceDef[] GetRefDefsInternal(GMFileContent content, SectionRefDefs* section, long elemOff, uint amount, uint rdeSize, Func<IntPtr, ReferenceDef> iter, bool correct = true)
        {
            if (section->Header.Size <= 4)
                return new ReferenceDef[0];

            amount = correct && amount == 0 ? section->Header.Size / rdeSize : amount;
            var r = new ReferenceDef[amount];

            uint i = 0;
            for (var rde = (byte*)&section->Entries + elemOff * sizeof(uint); i < amount; rde += rdeSize, i++)
                r[i] = iter((IntPtr)rde);

            return r;
        }

        // FFS YoYo Games, WHY?
#pragma warning disable CSE0003 // "Use expression-bodied members": too ugly with those lambdas
        public static ReferenceDef[] GetRefDefs          (GMFileContent content, SectionRefDefs* section)
        {
            return GetRefDefsInternal(content, section, 0, 0, 12 /* sizeof RefDefEntry */, p =>
            {
                var rde = (RefDefEntry*)p;
                var ret = new ReferenceDef();

                ret.Name        = StringFromOffset(content, rde->NameOffset);
                ret.Occurrences = rde->Occurrences ;
                ret.FirstOffset = rde->FirstAddress;

                return ret;
            });
        }

        public static ReferenceDef[] GetRefDefsWithLength(GMFileContent content, SectionRefDefs* section)
        {
            return GetRefDefsInternal(content, section, 1, section->Entries.NameOffset /* actually length, because reasons */, 12, p =>
            {
                var rde = (RefDefEntry*)p;
                var ret = new ReferenceDef();

                ret.Name        = StringFromOffset(content, rde->NameOffset);
                ret.Occurrences = rde->Occurrences ;
                ret.FirstOffset = rde->FirstAddress;

                return ret;
            }, false);
        }
        public static ReferenceDef[] GetRefDefsWithOthers(GMFileContent content, SectionRefDefs* section)
        {
            return GetRefDefsInternal(content, section, 3, 0, 20 /* sizeof RefDefEntryWithOthers */, p =>
            {
                var rde = (RefDefEntryWithOthers*)p; // they really are doing a Redigit here (well, this AND the DwordBools (instead of 1-byte bools or bit flags))
                var ret = new ReferenceDef();

                ret.Name = StringFromOffset(content, rde->NameOffset);
                ret.Occurrences = rde->Occurrences;
                ret.FirstOffset = rde->FirstAddress;
                ret.HasExtra = true;
                ret.InstanceType = (Decomp.InstanceType)rde->InstanceType;
                ret.unknown2 = rde->_pad1;
                ret.VariableType = Decomp.VariableType.Normal;

                return ret;
            });
        }
#pragma warning restore CSE0003

        public static FunctionLocalsInfo[] GetFunctionLocals(GMFileContent content, SectionRefDefs* section)
        {
            uint extraOffset = section->Entries.NameOffset * (uint)sizeof(RefDefEntry);
            IntPtr extraStart = (IntPtr)((byte*)&section->Entries.Occurrences + extraOffset);

            uint count = *(uint*)extraStart;
            var r = new FunctionLocalsInfo[count];

            uint i = 0;
            for (var entryPtr = (byte*)extraStart + 4; i < count; i++)
            {
                var extraEntry = (FunctionLocalsEntry*)entryPtr;
                var info = new FunctionLocalsInfo();

                info.FunctionName = StringFromOffset(content, extraEntry->FunctionName);
                info.LocalNames = new string[extraEntry->LocalsCount];

                entryPtr += 8;

                for (uint j = 0; j < extraEntry->LocalsCount; j++)
                {
                    var local = (FunctionLocalEntry*)entryPtr;
                    info.LocalNames[local->Index] = StringFromOffset(content, local->Name);
                    entryPtr += sizeof(FunctionLocalEntry);
                }

                r[i] = info;
            }

            return r;
        }
    }
}
