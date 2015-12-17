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

            return -1;
        }

        public static SpriteInfo      GetSpriteInfo (GMFileContent content, uint id)
        {
            if (id >= content.Sprites->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var se = (SpriteEntry*)GMFile.PtrFromOffset(content, (&content.Sprites->Offsets)[id]);

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

                    nextOff = i == (uint)SectionHeaders.Count - 1 ? content.Base->Size /*! untested */ : (ho[i + 1] - 12);
                }

            var re = (ObjectEntry*)GMFile.PtrFromOffset(content, reOff);

            var name = (byte*)GMFile.PtrFromOffset(content, re->Name);

            var len = (int)(nextOff - reOff) - 8; // name, spriteInd

            ret.Name = ReadString(name);
            ret.SpriteIndex = re->SpriteIndex;
            ret.Data = new byte[len];

            Marshal.Copy(new IntPtr(&re->Data), ret.Data, 0, len);

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

                    nextOff = i == (uint)SectionHeaders.Count - 1 ? content.Base->Size /*! untested */ : (ho[i + 1] - 12);
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

            const int RoomElementPadding = 0x24;

            // backgrounds
            {
                var len = stuff->Count;
                ret.Backgrounds = new RoomBackground[len];

                var addresses = &stuff->Offsets;

                for (uint i = 0; i < len; i++)
                {
                    var entry = (RoomBgEntry*)GMFile.PtrFromOffset(content, addresses[i]);

                    var b = new RoomBackground();

                    b.IsEnabled = entry->IsEnabled.IsTrue();
                    b.BgIndex   = entry->DefIndex;
                    b.Position  = entry->Position;
                    b.TileX     = entry->TileX.IsTrue();
                    b.TileY     = entry->TileY.IsTrue();

                    b.Data = new byte[RoomBgEntry.DataLength];

                    Marshal.Copy((IntPtr)entry->Data, b.Data, 0, RoomBgEntry.DataLength);

                    ret.Backgrounds[i] = b;
                }

                stuff = (CountOffsetsPair*)((byte*)stuff + sizeof(RoomBgEntry) * len + RoomElementPadding);
            }
            // views
            {
                var len = stuff->Count;
                ret.Views = new RoomView[len];

                var addresses = &stuff->Offsets;

                for (uint i = 0; i < len; i++)
                {
                    var entry = (RoomViewEntry*)GMFile.PtrFromOffset(content, addresses[i]);

                    var v = new RoomView();

                    v.IsEnabled = entry->IsEnabled.IsTrue();
                    v.Port      = entry->Port;
                    v.View      = entry->View;

                    v.Data = new byte[RoomViewEntry.DataLength];

                    Marshal.Copy((IntPtr)entry->Data, v.Data, 0, RoomViewEntry.DataLength);

                    ret.Views[i] = v;
                }

                stuff = (CountOffsetsPair*)((byte*)stuff + sizeof(RoomViewEntry) * len + RoomElementPadding);
            }
            // objects
            {
                var len = stuff->Count;
                ret.Objects = new RoomObject[len];

                var addresses = &stuff->Offsets;

                for (uint i = 0; i < len; i++)
                {
                    var entry = (RoomObjEntry*)GMFile.PtrFromOffset(content, addresses[i]);

                    var o = new RoomObject();

                    o.DefIndex = entry->DefIndex;
                    o.Position = entry->Position;
                    o.Scale    = entry->Scale;
                    o.Tint     = entry->Tint;

                    ret.Objects[i] = o;
                }

                //stuff = (CountOffsetsPair*)((byte*)stuff + sizeof(RoomObjEntry) * len + RoomElementPadding + 0x10);
            }

            ret.Tiles = new RoomTile[0];
            // tiles
            //TODO: still not working
            //{
            //    var len = stuff->Count;

            //    ret.Tiles = new RoomTile[len];

            //    //var tiles = new List<RoomTile>();

            //    var addresses = &stuff->Offsets;

            //    for (uint i = 0; i < len; i++)
            //    {
            //        //var morestuff = (CountOffsetsPair*)GMFile.PtrFromOffset(content, addresses[i]);

            //        //var morelen = morestuff->Count;
            //        //var moreaddresses = &morestuff->Offsets;

            //        //for (uint j = 0; j < morelen; j++)
            //        //{
            //        //    var entry = (RoomTileEntry*)GMFile.PtrFromOffset(content, moreaddresses[j]);

            //        //    var t = new RoomTile();

            //        //    t.DefIndex       = entry->DefIndex;
            //        //    t.Position       = entry->Position;
            //        //    t.Scale          = entry->Scale;
            //        //    t.Size           = entry->Size;
            //        //    t.SourcePosition = entry->SourcePos;
            //        //    t.Tint           = entry->Tint;

            //        //    tiles.Add(t);
            //        //}

            //        var entry = (RoomTileEntry*)GMFile.PtrFromOffset(content, addresses[i]);

            //        var t = new RoomTile();

            //        t.DefIndex       = entry->DefIndex;
            //        t.Position       = entry->Position;
            //        t.Scale          = entry->Scale;
            //        t.Size           = entry->Size;
            //        t.SourcePosition = entry->SourcePos;
            //        t.Tint           = entry->Tint;

            //        ret.Tiles[i] = t;
            //    }

            //    //ret.Tiles = tiles.ToArray();

            //    // no need to increase 'stuff' here
            //}

            return ret;
        }
        public static TexturePageInfo GetTexPageInfo(GMFileContent content, uint id)
        {
            if (id >= content.TexturePages->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var tpe = (TexPageEntry*)GMFile.PtrFromOffset(content, (&content.TexturePages->Offsets)[id]);

            var ret = new TexturePageInfo();

            ret.Position      = tpe->Position     ;
            ret.RenderOffset  = tpe->RenderOffset ;
            ret.Size          = tpe->Size         ;
            ret.SpritesheetId = tpe->SpritesheetId;

            return ret;
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

            Marshal.Copy((IntPtr)au + 4, ret.Wave, 0, ret.Wave.Length);

            return ret;
        }
        public static string          GetStringInfo (GMFileContent content, uint id)
        {
            if (id >= content.Strings->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var stre = (StringEntry*)GMFile.PtrFromOffset(content, (&content.Strings->Offsets)[id]);

            return unchecked(new string((sbyte*)&stre->Data, 0, (int)stre->Length, Encoding.ASCII));
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
