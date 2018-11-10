using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Altar
{
    using static SR;

    // TODO: LANG DAFL | EMBI
    public enum SectionHeaders : uint
    {
        Form        = 0x4D524F46, // FORM
        General     = 0x384E4547, // GEN8
        Options     = 0x4E54504F, // OPTN
        Language    = 0x474E414C, // LANG
        Extensions  = 0x4E545845, // EXTN
        Sounds      = 0x444E4F53, // SOND
        AudioGroup  = 0x50524741, // AGRP
        Sprites     = 0x54525053, // SPRT
        Backgrounds = 0x444E4742, // BGND
        Paths       = 0x48544150, // PATH
        Scripts     = 0x54504353, // SCPT
        Globals     = 0x424F4C47, // GLOB
        Shaders     = 0x52444853, // SHDR
        Fonts       = 0x544E4F46, // FONT
        Timelines   = 0x4E4C4D54, // TMLN
        Objects     = 0x544A424F, // OBJT
        Rooms       = 0x4D4F4F52, // ROOM
        DataFiles   = 0x4C464144, // DAFL
        TexturePage = 0x47415054, // TPAG
        Code        = 0x45444F43, // CODE
        Variables   = 0x49524156, // VARI
        Functions   = 0x434E5546, // FUNC
        Strings     = 0x47525453, // STRG
        Textures    = 0x52545854, // TXTR
        Audio       = 0x4F445541, // AUDO
        EmbedImage  = 0x49424D45, // EMBI

        Count = 25
    }
    public static class SectionHeadersExtensions
    {
        readonly static StringBuilder sb = new StringBuilder(4);

        public static string ToChunkName(this SectionHeaders h)
        {
            sb.Clear();

            var u = (uint)h;

            var c3 = (char)((u & 0xFF000000) >> 24);
            var c2 = (char)((u & 0x00FF0000) >> 16);
            var c1 = (char)((u & 0x0000FF00) >>  8);
            var c0 = (char)((u & 0x000000FF)      );

            return sb
                .Append(c0).Append(c1).Append(c2).Append(c3)
                .ToString();
        }

        public static SectionHeaders FromChunkName(string s)
        {
            uint u;

            u  = ((uint)s[0]      ) & 0x000000FF;
            u |= ((uint)s[1] <<  8) & 0x0000FF00;
            u |= ((uint)s[2] << 16) & 0x00FF0000;
            u |= ((uint)s[3] << 24) & 0xFF000000;

            return (SectionHeaders)u;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe class UniquePtr : IDisposable
    {
        IntPtr stuff;

        public IntPtr IPtr => stuff;
        public void*  VPtr => (void*)stuff;
        public byte*  BPtr => (byte*)stuff;

        public int Size;
        public long LongSize;

        public UniquePtr(byte[] data)
        {
            Size     = data.Length;
            LongSize = data.LongLength;

            stuff = Marshal.AllocHGlobal(data.Length);

            ILHacks.Cpblk(data, stuff, 0, data.Length);
          //Marshal.Copy(data, 0, stuff, data.Length);
        }
        ~UniquePtr()
        {
            Disposing();
        }

        void Disposing()
        {
            if (stuff != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(stuff);
                stuff = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            Disposing();
            GC.SuppressFinalize(this);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe class GMFileContent : IDisposable
    {
        public UniquePtr RawData;

        public SectionHeader * Form   ;

        public SectionGeneral* General => (SectionGeneral*)GetChunk(SectionHeaders.General);
        public SectionOptions* Options => (SectionOptions*)GetChunk(SectionHeaders.Options);
        public SectionGlobals* Globals => (SectionGlobals*)GetChunk(SectionHeaders.Globals);

        public SectionCountOffsets* Sounds       => (SectionCountOffsets*)GetChunk(SectionHeaders.Sounds        );
        public SectionCountOffsets* Sprites      => (SectionCountOffsets*)GetChunk(SectionHeaders.Sprites       );
        public SectionCountOffsets* Backgrounds  => (SectionCountOffsets*)GetChunk(SectionHeaders.Backgrounds   );
        public SectionCountOffsets* Paths        => (SectionCountOffsets*)GetChunk(SectionHeaders.Paths         );
        public SectionCountOffsets* Scripts      => (SectionCountOffsets*)GetChunk(SectionHeaders.Scripts       );
        public SectionCountOffsets* Fonts        => (SectionCountOffsets*)GetChunk(SectionHeaders.Fonts         );
        public SectionCountOffsets* Objects      => (SectionCountOffsets*)GetChunk(SectionHeaders.Objects       );
        public SectionCountOffsets* Rooms        => (SectionCountOffsets*)GetChunk(SectionHeaders.Rooms         );
        public SectionCountOffsets* TexturePages => (SectionCountOffsets*)GetChunk(SectionHeaders.TexturePage   );
        public SectionCountOffsets* Code         => (SectionCountOffsets*)GetChunk(SectionHeaders.Code          );
        public SectionCountOffsets* Strings      => (SectionCountOffsets*)GetChunk(SectionHeaders.Strings       );
        public SectionCountOffsets* Textures     => (SectionCountOffsets*)GetChunk(SectionHeaders.Textures      );
        public SectionCountOffsets* Audio        => (SectionCountOffsets*)GetChunk(SectionHeaders.Audio         );
        public SectionCountOffsets* AudioGroup   => (SectionCountOffsets*)GetChunk(SectionHeaders.AudioGroup    );
        public SectionCountOffsets* Extensions   => (SectionCountOffsets*)GetChunk(SectionHeaders.Extensions    );
        public SectionCountOffsets* Shaders      => (SectionCountOffsets*)GetChunk(SectionHeaders.Shaders       );
        public SectionCountOffsets* Timelines    => (SectionCountOffsets*)GetChunk(SectionHeaders.Timelines     );
        public SectionCountOffsets* EmbedImage   => (SectionCountOffsets*)GetChunk(SectionHeaders.EmbedImage    );

        public SectionRefDefs* Functions => (SectionRefDefs*)GetChunk(SectionHeaders.Functions);
        public SectionRefDefs* Variables => (SectionRefDefs*)GetChunk(SectionHeaders.Variables);

        public Dictionary<SectionHeaders, IntPtr> Chunks = new Dictionary<SectionHeaders, IntPtr>();

        internal long[] HeaderOffsets = new long[(int)SectionHeaders.Count];

        public GMFileContent(byte[] data)
        {
            RawData = new UniquePtr(data);
            byte* hdr_b = RawData.BPtr;

            var basePtr = (SectionHeader*)hdr_b;

            Form = basePtr;

            if (Form->Identity != SectionHeaders.Form)
                throw new InvalidDataException(ERR_NO_FORM);

            SectionHeader*
                hdr = basePtr + 1,
                hdrEnd = (SectionHeader*)((IntPtr)basePtr + (int)Form->Size);

            int headersMet = 0;

            for (; hdr < hdrEnd; hdr = unchecked((SectionHeader*)((IntPtr)hdr + (int)hdr->Size) + 1), ++headersMet)
            {
                Chunks.Add(hdr->Identity, (IntPtr)hdr);

                for (int i = 0; i < HeaderOffsets.Length; i++)
                    if (((SectionHeader*)((byte*)basePtr + HeaderOffsets[i]))->Identity == hdr->Identity)
                        Console.Error.WriteLine($"WARNING: chunk {hdr->MagicString()} encountered (at least) twice! Only the last occurrence will be exported! (If you see this message, consider reversing manually.)");

                if (HeaderOffsets.Length >= headersMet)
                {
                    var ho = HeaderOffsets;
                    Array.Resize(ref ho, (headersMet == HeaderOffsets.Length) ? 1 : (headersMet + 2));
                    HeaderOffsets = ho;
                }
                HeaderOffsets[headersMet] = (byte*)hdr - (byte*)basePtr;
            }
        }

        public void DumpChunkOffs()
        {
            for (int i = 0; i < HeaderOffsets.Length; ++i)
            {
                var l = HeaderOffsets[i];
                var p = *(SectionHeader*)GMFile.PtrFromOffset(this, l);

                Console.Error.WriteLine(p.MagicString() + ": " + l.ToString("X8") + "-" + (l+p.Size).ToString("X8") + " (" + p.Size.ToString("X8") + ")");
            }
        }

        public SectionHeader* GetChunk(SectionHeaders ident)
        {
            if (Chunks.ContainsKey(ident))
                return (SectionHeader*)Chunks[ident];

            return null;
        }

        void Disposing()
        {
            if (RawData != null)
            {
                RawData.Dispose();
                RawData = null;
            }
        }

        public void Dispose()
        {
            Disposing();
            HeaderOffsets = null;
            GC.SuppressFinalize(this);
        }
        ~GMFileContent()
        {
            Disposing();
        }
    }
}

