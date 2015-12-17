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

            var se = (SpriteEntry*)GMFile.PtrFromOffset(content, (&content.Sprites->Offset)[id]);

            var ret = new SpriteInfo();

            ret.Name = ReadString((byte*)GMFile.PtrFromOffset(content, se->Name));
            ret.Size = se->Size;

            ret.TextureIndices = new uint[se->TextureCount];

            for (uint i = 0; i < se->TextureCount; i++)
                for (uint j = 0; j < content.TexturePages->Count; j++)
                    if ((&se->TextureAddresses)[i] == (&content.TexturePages->Offset)[j])
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

            var be = (BgEntry*)GMFile.PtrFromOffset(content, (&content.Backgrounds->Offset)[id]);

            ret.Name          = ReadString((byte*)GMFile.PtrFromOffset(content, be->Name));

            ret.TexPageIndex = be->TextureOffset;

            for (uint i = 0; i < content.TexturePages->Count; i++)
                if (be->TextureOffset == (&content.TexturePages->Offset)[i])
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

            var se = (ScriptEntry*)GMFile.PtrFromOffset(content, (&content.Scripts->Offset)[id]);

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

            var reOff   = (&content.Objects->Offset)[id    ];
            var nextOff = (&content.Objects->Offset)[id + 1];

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

            var reOff   = (&content.Rooms->Offset)[id    ];
            var nextOff = (&content.Rooms->Offset)[id + 1];

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

            //TODO: finish

            var subDataPos = &re->Data;

            // backgrounds
            {
                var len = *subDataPos;
                ret.Backgrounds = new RoomBackground[len];

                var addresses = subDataPos + 1;
                var data = addresses + len;

                for (uint i = 0; i < len; i++)
                {
                    var entry = (RoomBgEntry*)GMFile.PtrFromOffset(content, addresses[i]);

                    var b = new RoomBackground();

                    b.IsEnabled = entry->IsEnabled.IsTrue();
                    b.BgIndex   = entry->BgIndex;
                    b.Position  = entry->Position;
                    b.TileX     = entry->TileX.IsTrue();
                    b.TileY     = entry->TileY.IsTrue();

                    uint nextStart;
                    if (i == len - 1)
                        nextStart = 0;
                    else
                        nextStart = addresses[i + 1];

                    ret.Backgrounds[i] = b;
                }
            }
            // views
            {

            }
            // objects
            {

            }
            // tiles
            {

            }

            //var len = (int)(nextOff - reOff) - 24; // name, name2, size, colour(, padding)

            //ret.Data = new byte[len];

            //Marshal.Copy(new IntPtr(&re->Data), ret.Data, 0, len);

            return ret;
        }
        public static TexturePageInfo GetTexPageInfo(GMFileContent content, uint id)
        {
            if (id >= content.TexturePages->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var tpe = (TexPageEntry*)GMFile.PtrFromOffset(content, (&content.TexturePages->Offset)[id]);

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

            var te = (TextureEntry*)GMFile.PtrFromOffset(content, (&content.Textures->Offset)[id]);

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

            var au = (AudioEntry*)GMFile.PtrFromOffset(content, (&content.Audio->Offset)[id]);

            var ret = new AudioInfo();

            ret.Wave = new byte[au->Length + 4];

            Marshal.Copy((IntPtr)au + 4, ret.Wave, 0, ret.Wave.Length);

            return ret;
        }
        public static string          GetStringInfo (GMFileContent content, uint id)
        {
            if (id >= content.Strings->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var stre = (StringEntry*)GMFile.PtrFromOffset(content, (&content.Strings->Offset)[id]);

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
