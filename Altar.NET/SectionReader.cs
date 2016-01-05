using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Altar
{
    public unsafe static class SectionReader
    {
        // http://undertale.rawr.ws/unpacking
        // https://www.reddit.com/r/Underminers/comments/3teemm/wip_documenting_stringstxt/

        static uint PngLength(PngHeader* png)
        {
#pragma warning disable RECS0065
            if (png == null)
                return 0;
#pragma warning restore RECS0065

            var chunk = &png->IHDR.Header;

            while (chunk->Type != PngChunk.ChunkEnd)
            {
                if (chunk->Length == 0)
                    return 0;

                chunk = unchecked((PngChunk*)((IntPtr)chunk + (int)Utils.SwapEnd32(chunk->Length) + 0xC));
            }

            return unchecked((uint)((long)++chunk - (long)png));
        }

        internal static void   ReadString(byte* ptr, StringBuilder sb)
        {
            while (*ptr != 0)
            {
                sb.Append((char)*ptr);

                ptr++;
            }
        }
        internal static string ReadString(byte* ptr)
        {
            var sb = new StringBuilder();

            ReadString(ptr, sb);

            return sb.ToString();
        }
        internal static string StringFromOffset(GMFileContent content, uint off)
        {
            if (off == 0 || (off & 0xFFFFFF00) == 0xFFFFFF00)
                return String.Empty;

            return ReadString((byte*)GMFile.PtrFromOffset(content, off));
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

        static TexturePageInfo TPagFromOffset  (GMFileContent content, uint off)
        {
            var tpe = (TexPageEntry*)GMFile.PtrFromOffset(content, off);

            var ret = new TexturePageInfo();

            ret.Position      = tpe->Position;
            ret.RenderOffset  = tpe->RenderOffset;
            ret.Size          = tpe->Size;
            ret.BoundingBox   = tpe->BoundingBox;
            ret.SpritesheetId = tpe->SpritesheetId;

            return ret;
        }

        static RoomBackground ReadRoomBg  (GMFileContent content, IntPtr p)
        {
            var entry = (RoomBgEntry*)p;

            var b = new RoomBackground();

            b.IsEnabled     = entry->IsEnabled.IsTrue()   ;
            b.IsForeground  = entry->IsForeground.IsTrue();
            b.BgIndex       = entry->DefIndex             ;
            b.Position      = entry->Position             ;
            b.TileX         = entry->TileX.IsTrue()       ;
            b.TileY         = entry->TileY.IsTrue()       ;
            b.Speed         = entry->Speed                ;
            b.StretchSprite = entry->Stretch.IsTrue()     ;

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

            v.ObjectId = entry->ObjectId < 0 ? null : (uint?)entry->ObjectId;

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

            return t;
        }

        public static GeneralInfo GetGeneralInfo(GMFileContent content)
        {
            var ret = new GeneralInfo();

            var ge = content.General;

            ret.IsDebug         = ge->Debug;
            ret.FileName        = StringFromOffset(content, ge->FilenameOffset);
            ret.Configuration   = StringFromOffset(content, ge->ConfigOffset);
            ret.GameId          = ge->GameId;
            ret.Name            = StringFromOffset(content, ge->NameOffset);
            ret.Version         = new Version(ge->Major, ge->Minor, ge->Release, ge->Build);
            ret.WindowSize      = ge->WindowSize;
            ret.DisplayName     = StringFromOffset(content, ge->DisplayNameOffset);
            ret.BytecodeVersion = ge->BytecodeVersion;

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

            ret.Constants = new Dictionary<string, string>((int)oe->ConstMap.Count);
            for (uint i = 0; i < oe->ConstMap.Count; i++)
                ret.Constants.Add(StringFromOffset(content, (&oe->ConstMap.Offsets)[i * 2]), StringFromOffset(content, (&oe->ConstMap.Offsets)[i * 2 + 1]));

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

            ret.AudioId      =  se->AudioId;
            ret.IsEmbedded   = (se->Flags & SoundEntryFlags.Embedded  ) != 0;
            ret.IsCompressed = (se->Flags & SoundEntryFlags.Compressed) != 0;

            return ret;
        }
        public static SpriteInfo      GetSpriteInfo (GMFileContent content, uint id)
        {
            if (id >= content.Sprites->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var se = (SpriteEntry*)GMFile.PtrFromOffset(content, (&content.Sprites->Offsets)[id]);

            var ret = new SpriteInfo();

            ret.Name     = StringFromOffset(content, se->Name);
            ret.Size     = se->Size    ;
            ret.Bounding = se->Bounding;
            ret.BBoxMode = se->BBoxMode;
            ret.SepMasks = se->SepMasks;
            ret.Origin   = se->Origin  ;

            ret.TextureIndices = new uint[se->Textures.Count];

            for (uint i = 0; i < se->Textures.Count; i++)
                for (uint j = 0; j < content.TexturePages->Count; j++)
                    if ((&se->Textures.Offsets)[i] == (&content.TexturePages->Offsets)[j])
                    {
                        ret.TextureIndices[i] = j;
                        break;
                    }

            return ret;
        }
        public static BackgroundInfo  GetBgInfo     (GMFileContent content, uint id)
        {
            if (id >= content.Backgrounds->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var ret = new BackgroundInfo();

            var be = (BgEntry*)GMFile.PtrFromOffset(content, (&content.Backgrounds->Offsets)[id]);

            ret.Name         = StringFromOffset(content, be->Name);
            ret.TexPageIndex = be->TextureOffset;

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

            var tpag = TPagFromOffset(content, fe->TPagOffset);

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
                c.TexturePageFrame = entry->TexPagFrame;
                c.Shift            = entry->Shift      ;
                c.Offset           = entry->Offset     ;

                return c;
            });

            return ret;
        }
        public static ObjectInfo      GetObjectInfo (GMFileContent content, uint id)
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

            ret.Physics = oe->Physics;

            // floats messing things up - do not uncomment for now (see PackedStructs.cs, struct ObjectEntry)
            //ret.Data = new uint[oe->ShapePoints.Count];

            //for (uint i = 0; i < oe->ShapePoints.Count; i++)
            //    ret.Data[i] = *(uint*)GMFile.PtrFromOffset(content, (&oe->ShapePoints.Offsets)[i]);

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

            ret.EnableViews = (re->Flags & RoomEntryFlags.EnableViews) != 0;
            ret.ShowColour  = (re->Flags & RoomEntryFlags.ShowColour ) != 0;

            ret.World          = re->World         ;
            ret.Bounding       = re->Bounding      ;
            ret.Gravity        = re->Gravity       ;
            ret.MetresPerPixel = re->MetresPerPixel;

            ret.Backgrounds = ReadList(content, (CountOffsetsPair*)GMFile.PtrFromOffset(content, re->BgOffset  ), ReadRoomBg  );
            ret.Views       = ReadList(content, (CountOffsetsPair*)GMFile.PtrFromOffset(content, re->ViewOffset), ReadRoomView);
            ret.Objects     = ReadList(content, (CountOffsetsPair*)GMFile.PtrFromOffset(content, re->ObjOffset ), ReadRoomObj );
            ret.Tiles       = ReadList(content, (CountOffsetsPair*)GMFile.PtrFromOffset(content, re->TileOffset), ReadRoomTile);

            return ret;
        }
        public static TexturePageInfo GetTexPageInfo(GMFileContent content, uint id)
        {
            if (id >= content.TexturePages->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            return TPagFromOffset(content, (&content.TexturePages->Offsets)[id]);
        }
        public static TextureInfo     GetTextureInfo(GMFileContent content, uint id)
        {
            if (id >= content.Textures->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var te = (TextureEntry*)GMFile.PtrFromOffset(content, (&content.Textures->Offsets)[id]);

            var ret = new TextureInfo();

            var png = (PngHeader*)GMFile.PtrFromOffset(content, te->Offset);

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

            ret.Wave = new byte[au->Length + 4];

            Marshal.Copy((IntPtr)(&au->Data), ret.Wave, 0, ret.Wave.Length);

            return ret;
        }
        public static string          GetStringInfo (GMFileContent content, uint id)
        {
            if (id >= content.Strings->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var se = (StringEntry*)GMFile.PtrFromOffset(content, (&content.Strings->Offsets)[id]);

            return ReadString(&se->Data);
        }

        public static byte[][] ListToByteArrays(GMFileContent content, SectionCountOffsets* list, long elemLen = 0)
        {
            var ret = new byte[list->Count][];

            for (uint i = 0; i < list->Count; i++)
            {
                var  curOff = (&list->Offsets)[i];
                var nextOff = i == list->Count - 1 ? list->Header.Size - 4 : (&list->Offsets)[i + 1];

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
        static ReferenceDef[] GetRefDefsInternal(GMFileContent content, SectionRefDefs* section, uint elemOff, uint amount, uint rdeSize, Func<IntPtr, ReferenceDef> iter)
        {
            amount = amount == 0 ? section->Header.Size / rdeSize : amount;
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
            });
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

                return ret;
            });
        }
#pragma warning restore CSE0003
    }
}
