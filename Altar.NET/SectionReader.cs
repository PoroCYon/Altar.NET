using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Altar.NET
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

            var chunck = &png->IHDR.Header;

            while (chunck->Type != PngChunck.ChunckEnd)
            {
                if (chunck->Length == 0)
                    return 0;

                chunck = unchecked((PngChunck*)((IntPtr)chunck + (int)Utils.SwapEnd32(chunck->Length) + 0xC));
            }

            return unchecked((uint)((long)++chunck - (long)png));
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

        static long IndexOfUnsafe(uint* arr, uint arrlen, uint value)
        {
            for (uint i = 0; i < arrlen; i++)
                if (arr[i] == value)
                    return i;

            return -1L;
        }

        static T[] ReadList<T>(GMFileContent content, CountOffsetsPair* list, Func<IntPtr, T> readThing)
        {
            if (readThing == null)
                throw new ArgumentNullException(nameof(readThing));

            var len = list->Count;
            var ret = new T[len];

            var addresses = &list->Offsets;

            for (uint i = 0; i < len; i++)
                ret[i] = readThing((IntPtr)GMFile.PtrFromOffset(content, addresses[i]));

            return ret;
        }

        static RoomTile[] ReadRoomTileList(GMFileContent content, CountOffsetsPair* list)
        {
            while (list->Count == 0xFFFFFFFF)
                list = (CountOffsetsPair*)((byte*)list + sizeof(uint));

            return ReadList(content, list, p =>
            {
                var entry = (RoomTileEntry*)p;

                var t = new RoomTile();

                t.DefIndex       = entry->DefIndex;
                t.Position       = entry->Position;
                t.SourcePosition = entry->SourcePos;
                t.Size           = entry->Size;
                t.Scale          = entry->Scale;
                t.Tint           = entry->Tint;

                return t;
            });
        }

        static SpriteInfo      SpriteFromOffset(GMFileContent content, uint off)
        {
            var se = (SpriteEntry*)GMFile.PtrFromOffset(content, off);

            var ret = new SpriteInfo();

            ret.Name = ReadString((byte*)GMFile.PtrFromOffset(content, se->Name));
            ret.Size = se->Size;

            ret.TextureIndices = new uint[se->TextureCount];

            for (uint i = 0; i < se->TextureCount; i++)
                for (uint j = 0; j < content.TexturePages->Count; j++)
                    if ((&se->TextureAddresses)[i] == (&content.TexturePages->Offsets)[j])
                    {
                        ret.TextureIndices[i] = j;
                        break;
                    }

            return ret;
        }
        static TexturePageInfo TPagFromOffset  (GMFileContent content, uint off)
        {
            var tpe = (TexPageEntry*)GMFile.PtrFromOffset(content, off);

            var ret = new TexturePageInfo();

            ret.Position      = tpe->Position;
            ret.RenderOffset  = tpe->RenderOffset;
            ret.Size          = tpe->Size;
            ret.SpritesheetId = tpe->SpritesheetId;

            return ret;
        }

        static RoomBackground[] GetRoomBgs  (GMFileContent content, ref CountOffsetsPair* list)
        {
            try
            {
                return ReadList(content, list, p =>
                {
                    var entry = (RoomBgEntry*)p;

                    var b = new RoomBackground();

                    b.IsEnabled = entry->IsEnabled.IsTrue();
                    b.BgIndex   = entry->DefIndex;
                    b.Position  = entry->Position;
                    b.TileX     = entry->TileX.IsTrue();
                    b.TileY     = entry->TileY.IsTrue();

                    return b;
                });
            }
            finally
            {
                const int Padding = 0x24;

                list = (CountOffsetsPair*)((byte*)list + sizeof(RoomBgEntry) * list->Count + Padding);
            }
        }
        static RoomView      [] GetRoomViews(GMFileContent content, ref CountOffsetsPair* list)
        {
            try
            {
                return ReadList(content, list, p =>
                {
                    var entry = (RoomViewEntry*)p;

                    var v = new RoomView();

                    v.IsEnabled = entry->IsEnabled.IsTrue();
                    v.Port      = entry->Port;
                    v.View      = entry->View;

                    //v.Data = new byte[RoomViewEntry.DataLength];

                    //Marshal.Copy((IntPtr)entry->Data, v.Data, 0, RoomViewEntry.DataLength);

                    return v;
                });
            }
            finally
            {
                const int Padding = 0x24;

                list = (CountOffsetsPair*)((byte*)list + sizeof(RoomViewEntry) * list->Count + Padding);
            }
        }
        static RoomObject    [] GetRoomObjs (GMFileContent content, ref CountOffsetsPair* list)
        {
            try
            {
                return ReadList(content, list, p =>
                {
                    var entry = (RoomObjEntry*)p;

                    var o = new RoomObject();

                    o.DefIndex = entry->DefIndex;
                    o.Position = entry->Position;
                    o.Scale    = entry->Scale;
                    o.Tint     = entry->Tint;

                    return o;
                });
            }
            finally
            {
                const int Padding = 0x24;

                list = (CountOffsetsPair*)((byte*)list + sizeof(RoomObjEntry) * list->Count + (Padding + 0x10));
            }
        }
        //TODO: either data or description are drunk
        static RoomTile      [] GetRoomTiles(GMFileContent content, ref CountOffsetsPair* list)
        {
            //return ReadRoomTileList(content, list);

//#pragma warning disable 162
            var rtl = new List<RoomTile>();
//#pragma warning restore 162

            var len = list->Count + 1 /* wtf? */;
            var addresses = &list->Offsets;

            for (uint i = 0; i < len; i++)
                rtl.AddRange(ReadRoomTileList(content, (CountOffsetsPair*)GMFile.PtrFromOffset(content, addresses[i])));

            return rtl.ToArray();
        }

        public static SoundInfo       GetSoundInfo  (GMFileContent content, uint id)
        {
            if (id >= content.Sounds->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var se = (SoundEntry*)GMFile.PtrFromOffset(content, (&content.Sounds->Offsets)[id]);

            var ret = new SoundInfo();

            ret.Name = ReadString((byte*)GMFile.PtrFromOffset(content, se->NameOffset));
            ret.Type = ReadString((byte*)GMFile.PtrFromOffset(content, se->TypeOffset));
            ret.File = ReadString((byte*)GMFile.PtrFromOffset(content, se->FileOffset));

            ret.VolumeMod = se->Volume;
            ret.PitchMod  = se->Pitch ;

            ret.AudioId    =  se->AudioId;
            ret.IsEmbedded = (se->Flags & SoundEntryFlags.Embedded) != 0;

            return ret;
        }
        public static SpriteInfo      GetSpriteInfo (GMFileContent content, uint id)
        {
            if (id >= content.Sprites->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            return SpriteFromOffset(content, (&content.Sprites->Offsets)[id]);
        }
        public static BackgroundInfo  GetBgInfo     (GMFileContent content, uint id)
        {
            if (id >= content.Backgrounds->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var ret = new BackgroundInfo();

            var be = (BgEntry*)GMFile.PtrFromOffset(content, (&content.Backgrounds->Offsets)[id]);

            ret.Name          = ReadString((byte*)GMFile.PtrFromOffset(content, be->Name));

            ret.TexPageIndex = be->TextureOffset;

            for (uint i = 0; i < content.TexturePages->Count; i++)
                if (be->TextureOffset == (&content.TexturePages->Offsets)[i])
                {
                    ret.TexPageIndex = i;
                    break;
                }

            return ret;
        }
        public static ScriptInfo      GetScriptInfo (GMFileContent content, uint id)
        {
            if (id >= content.Scripts->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var se = (ScriptEntry*)GMFile.PtrFromOffset(content, (&content.Scripts->Offsets)[id]);

            var ret = new ScriptInfo();

            ret.Name = ReadString((byte*)GMFile.PtrFromOffset(content, se->Name));
            ret.CodeId = se->CodeId;

            return ret;
        }
        public static ObjectInfo      GetObjectInfo (GMFileContent content, uint id)
        {
            if (id >= content.Objects->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var ret = new ObjectInfo();

            var reOff   = (&content.Objects->Offsets)[id    ];
            var nextOff = (&content.Objects->Offsets)[id + 1];

            if (id == content.Objects->Count - 1)
                fixed (uint* ho = content.HeaderOffsets)
                {
                    var i = IndexOfUnsafe(ho, (uint)SectionHeaders.Count, (uint)((byte*)content.Objects - content.RawData.BPtr));

                    nextOff = i == (uint)SectionHeaders.Count - 1 ? content.Form->Size /*! untested */ : (ho[i + 1] - 12);
                }

            var re = (ObjectEntry*)GMFile.PtrFromOffset(content, reOff);

            var name = (byte*)GMFile.PtrFromOffset(content, re->Name);

            var len = (int)(nextOff - reOff) - 8; // name, spriteInd

            ret.Name = ReadString(name);
            ret.SpriteIndex = re->SpriteIndex;

            //ret.Data = new byte[len];

            //Marshal.Copy(new IntPtr(&re->Data), ret.Data, 0, len);

            return ret;
        }
        public static RoomInfo        GetRoomInfo   (GMFileContent content, uint id)
        {
            if (id >= content.Rooms->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var ret = new RoomInfo();

            var reOff   = (&content.Rooms->Offsets)[id    ];
            var nextOff = (&content.Rooms->Offsets)[id + 1];

            if (id == content.Rooms->Count - 1)
                fixed (uint* ho = content.HeaderOffsets)
                {
                    var i = IndexOfUnsafe(ho, (uint)SectionHeaders.Count, (uint)((byte*)content.Rooms - content.RawData.BPtr));

                    nextOff = i == (uint)SectionHeaders.Count - 1 ? content.Form->Size /*! untested */ : (ho[i + 1] - 12);
                }

            var re = (RoomEntry*)GMFile.PtrFromOffset(content, reOff);

            var name  = (byte*)GMFile.PtrFromOffset(content, re->Name );
            var name2 = (byte*)GMFile.PtrFromOffset(content, re->Name2);

            var sb = new StringBuilder();

            ReadString(name, sb);

            if (*name2 != 0)
            {
                sb.Append('/');

                ReadString(name2, sb);
            }

            ret.Name   = sb.ToString();
            ret.Size   = re->Size;
            ret.Colour = re->Colour;

            var stuff = &re->Backgrounds;

            ret.Backgrounds = GetRoomBgs  (content, ref stuff);
            ret.Views       = GetRoomViews(content, ref stuff);
            ret.Objects     = GetRoomObjs (content, ref stuff);
          //ret.Tiles       = GetRoomTiles(content, ref stuff);

            ret.Tiles = new RoomTile[0];

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

            Marshal.Copy((IntPtr)au->Data, ret.Wave, 0, ret.Wave.Length);

            return ret;
        }
        public static FontInfo        GetFontInfo   (GMFileContent content, uint id)
        {
            if (id >= content.Fonts->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var fe = (FontEntry*)GMFile.PtrFromOffset(content, (&content.Fonts->Offsets)[id]);

            var ret = new FontInfo();

            var tpag = TPagFromOffset(content, fe->TPagOffset);

            ret.CodeName   = ReadString((byte*)GMFile.PtrFromOffset(content, fe->CodeName  ));
            ret.SystemName = ReadString((byte*)GMFile.PtrFromOffset(content, fe->SystemName));

            ret.Scale = fe->Scale;

            for (uint i = 0; i < content.TexturePages->Count; i++)
                if (fe->TPagOffset == (&content.TexturePages->Offsets)[i])
                {
                    ret.TexPagId = i;
                    break;
                }

            ret.Characters = ReadList(content, &fe->Chars, p =>
            {
                var entry = (FontCharEntry*)p;

                var c = new FontCharacter();

                c.Character        = entry->Character  ;
                c.RelativePosition = entry->RelativePos;
                c.Size             = entry->Size       ;

                return c;
            });

            return ret;
        }
        public static PathInfo        GetPathInfo   (GMFileContent content, uint id)
        {
            if (id >= content.Paths->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var pe = (PathEntry*)GMFile.PtrFromOffset(content, (&content.Paths->Offsets)[id]);

            var ret = new PathInfo();

            ret.Name = ReadString((byte*)GMFile.PtrFromOffset(content, pe->Name));

            return ret;
        }
        public static string          GetStringInfo (GMFileContent content, uint id)
        {
            if (id >= content.Strings->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var stre = (StringEntry*)GMFile.PtrFromOffset(content, (&content.Strings->Offsets)[id]);

            return unchecked(new string((sbyte*)&stre->Data, 0, (int)stre->Length, Encoding.ASCII));
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

        public static ReferenceDef[] GetRefDefs(GMFileContent content, SectionRefDefs* section)
        {
            var r = new ReferenceDef[section->Header.Size / 12];

            uint i = 0;
            for (RefDefEntry* rde = (RefDefEntry*)&section->Entries; i < section->Header.Size / 12; rde++, i++)
            {
                var ret = new ReferenceDef();

                var name = (byte*)GMFile.PtrFromOffset(content, rde->Name);

                ret.Name = ReadString(name);
                ret.Occurrences  = rde->Occurrences ;
                ret.FirstOffset = rde->FirstAddress;

                r[i] = ret;
            }

            return r.ToArray();
        }
    }
}
