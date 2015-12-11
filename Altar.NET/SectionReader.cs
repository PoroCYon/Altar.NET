using System;
using System.Collections.Generic;
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

        static void   ReadString(byte* ptr, StringBuilder sb)
        {
            while (*ptr != 0)
            {
                sb.Append((char)*ptr);

                ptr++;
            }
        }
        static string ReadString(byte* ptr)
        {
            var sb = new StringBuilder();

            ReadString(ptr, sb);

            return sb.ToString();
        }

        static int IndexOfUnsafe(uint* arr, uint arrlen, uint value)
        {
            for (uint i = 0; i < arrlen; i++)
                if (arr[i] == value)
                    return unchecked((int)i);

            return -1;
        }

        public static string GetStringFromOffset(GMFileContent content, uint offset)
        {
            var eptr = (byte*)GMFile.PtrFromOffset(content, offset);
            var stre = (StringEntry*)eptr;

            return unchecked(new string((sbyte*)&stre->Data, 0, (int)stre->Length, Encoding.ASCII));
        }
        public static string GetStringInfo      (GMFileContent content, uint id    )
        {
            if (id >= content.Strings->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            return GetStringFromOffset(content, (&content.Strings->Offset)[id]);
        }

        public static PngInfo GetPngFromOffset(GMFileContent content, uint offset)
        {
            var ret = new PngInfo();

            var png = (PngHeader*)GMFile.PtrFromOffset(content, offset);

            ret.Width  = Utils.SwapEnd32(png->IHDR.Width );
            ret.Height = Utils.SwapEnd32(png->IHDR.Height);

            ret.DataInfo = new byte[PngLength(png)];

            Marshal.Copy((IntPtr)png, ret.DataInfo, 0, ret.DataInfo.Length);

            return ret;
        }
        public static PngInfo GetTextureInfo  (GMFileContent content, uint id    )
        {
            if (id >= content.Textures->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var tex = (TextureEntry*)GMFile.PtrFromOffset(content, (&content.Textures->Offset)[id]);

            return GetPngFromOffset(content, tex->Offset);
        }

        public static AudioInfo GetAudioInfo(GMFileContent content, uint id)
        {
            if (id >= content.Audio->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var au = (AudioEntry*)GMFile.PtrFromOffset(content, (&content.Audio->Offset)[id]);

            var ret = new AudioInfo();

            ret.RIFF = new byte[au->Length + 4];

            Marshal.Copy((IntPtr)au + 4, ret.RIFF, 0, ret.RIFF.Length);

            return ret;
        }

        public static ObjectInfo     GetObjectInfo(GMFileContent content, uint id)
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
        public static NameDataPair   GetCodeInfo  (GMFileContent content, uint id)
        {
            if (id >= content.Code->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var ret = new NameDataPair();

            var re = (CodeEntry*)GMFile.PtrFromOffset(content, (&content.Code->Offset)[id]);

            var name = (byte*)GMFile.PtrFromOffset(content, re->Name);

            ret.Name = ReadString(name);

            ret.Data = new byte[re->Length];

            Marshal.Copy(new IntPtr(&re->Bytecode), ret.Data, 0, re->Length);

            return ret;
        }
        public static BackgroundInfo GetBgInfo    (GMFileContent content, uint id)
        {
            if (id >= content.Backgrounds->Count)
                throw new ArgumentOutOfRangeException(nameof(id));

            var ret = new BackgroundInfo();

            var be = (BgEntry*)GMFile.PtrFromOffset(content, (&content.Backgrounds->Offset)[id]);

            var name = (byte*)GMFile.PtrFromOffset(content, be->Name);

            ret.Name           = ReadString(name);
            ret.TextureAddress = be->TextureAddress;

            return ret;
        }
        public static RoomInfo       GetRoomInfo  (GMFileContent content, uint id)
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

            ret.Name = sb.ToString();
            ret.Size = re->Size;
            ret.Colour = re->Colour;

            //TODO: read the actual bgs, views, objs and tiles (-> offsets -> data length?)

            //var d = (byte*)&re->Data;

            //var bgLen = *(uint*)d; d += 4;
            //var bgOffs = (uint*)d;

            //for (uint i = 0; i < bgLen; i++)
            //{
            //    var bg = (RoomBgEntry*)GMFile.PtrFromOffset(ref content, (int)bgOffs[i]);

            //    Console.WriteLine(*bg);
            //}

            var len = (int)(nextOff - reOff) - 24; // name, name2, size, colour(, padding)

            ret.Data = new byte[len];

            Marshal.Copy(new IntPtr(&re->Data), ret.Data, 0, len);

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
                ret.FirstAddress = rde->FirstAddress;

                r[i] = ret;
            }

            return r.ToArray();
        }
    }
}
