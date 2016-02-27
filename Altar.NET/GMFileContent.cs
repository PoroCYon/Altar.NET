using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Altar
{
    public enum SectionHeaders : uint
    {
        Form        = 0x4D524F46, // FORM
        General     = 0x384E4547, // GEN8
        Options     = 0x4E54504F, // OPTN
        Extensions  = 0x4E545845, // EXTN
        Sounds      = 0x444E4F53, // SOND
        AudioGroup  = 0x50524741, // AGRP
        Sprites     = 0x54525053, // SPRT
        Backgrounds = 0x444E4742, // BGND
        Paths       = 0x48544150, // PATH
        Scripts     = 0x54504353, // SCPT
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

        Count = 23
    }
    public static class SectionHeadersExtensions
    {
        readonly static StringBuilder sb = new StringBuilder(4);

        public static string ToChunkName(this SectionHeaders h)
        {
            sb.Clear();

            var u = (uint)h;

            var c0 = (char)(u & 0xFF000000);
            var c1 = (char)(u & 0x00FF0000);
            var c2 = (char)(u & 0x0000FF00);
            var c3 = (char)(u & 0X000000FF);

            return sb
                .Append(c0).Append(c1).Append(c2).Append(c3)
                .ToString();
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

        public SectionGeneral* General;
        public SectionOptions* Options;

        public SectionUnknown* Extensions; // empty
        public SectionUnknown* AudioGroup; // empty
        public SectionUnknown* Shaders   ; // empty
        public SectionUnknown* Timelines ; // empty
        public SectionUnknown* DataFiles ; // empty

        public SectionCountOffsets* Sounds      ;
        public SectionCountOffsets* Sprites     ;
        public SectionCountOffsets* Backgrounds ;
        public SectionCountOffsets* Paths       ;
        public SectionCountOffsets* Scripts     ;
        public SectionCountOffsets* Fonts       ;
        public SectionCountOffsets* Objects     ;
        public SectionCountOffsets* Rooms       ;
        public SectionCountOffsets* TexturePages;
        public SectionCountOffsets* Code        ;
        public SectionCountOffsets* Strings     ;
        public SectionCountOffsets* Textures    ;
        public SectionCountOffsets* Audio       ;

        public SectionRefDefs* Functions;
        public SectionRefDefs* Variables;

        internal long[] HeaderOffsets = new long[(int)SectionHeaders.Count];

        void Disposing()
        {
            RawData.Dispose();
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
