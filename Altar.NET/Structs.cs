using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Altar.NET
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

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe class UniquePtr : IDisposable
    {
        IntPtr stuff;

        public IntPtr IPtr => stuff;
        public void*  VPtr => (void*)stuff;
        public byte*  BPtr => (byte*)stuff;

        public UniquePtr(byte[] data)
        {
            stuff = Marshal.AllocHGlobal(data.Length);

            Marshal.Copy(data, 0, stuff, data.Length);
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

        public SectionHeader * Base   ;
        public SectionGeneral* Gen    ;
        public SectionOptions* Options;

        public SectionUnknown* Extensions ;
        public SectionUnknown* Sounds     ;
        public SectionUnknown* AudioGroup ;
        public SectionUnknown* Paths      ;
        public SectionUnknown* Shaders    ;
        public SectionUnknown* Fonts      ;
        public SectionUnknown* Timelines  ;
        public SectionUnknown* DataFiles  ;

        public SectionCountOffset* Sprites     ;
        public SectionCountOffset* Backgrounds ;
        public SectionCountOffset* Scripts     ;
        public SectionCountOffset* Objects     ;
        public SectionCountOffset* Rooms       ;
        public SectionCountOffset* TexturePages;
        public SectionCountOffset* Code        ;
        public SectionCountOffset* Strings     ;
        public SectionCountOffset* Textures    ;
        public SectionCountOffset* Audio       ;

        public SectionRefDefs* Functions;
        public SectionRefDefs* Variables;

        internal uint[] HeaderOffsets = new uint[(int)SectionHeaders.Count];

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

    [StructLayout(LayoutKind.Sequential)]
    public struct ReferenceDef
    {
        public string Name;
        public uint Occurrences;
        public uint FirstOffset;
    }

    // ---

    [StructLayout(LayoutKind.Sequential)]
    public struct SpriteInfo
    {
        public string Name;
        public Point Size;
        public uint[] TextureIndices;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct BackgroundInfo
    {
        public string Name;
        public uint TexPageIndex;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct ScriptInfo
    {
        public string Name;
        public uint CodeId;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct ObjectInfo
    {
        public string Name;
        public uint SpriteIndex;
        public byte[] Data;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct RoomInfo
    {
        public string Name;
        public Point Size;
        public Colour Colour;

        public RoomBackground[] Backgrounds;
        public RoomView      [] Views      ;
        public RoomObject    [] Objects    ;
        public RoomTile      [] Tiles      ;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct TexturePageInfo
    {
        public Point16 Position, Size, RenderOffset;
        public uint SpritesheetId;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct CodeInfo
    {
        public string Name;
        public AnyInstruction*[] Instructions;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct TextureInfo
    {
        public uint Width, Height;
        public byte[] PngData;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct AudioInfo
    {
        public byte[] Wave;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RoomBackground
    {
        public bool IsEnabled;
        public uint BgIndex;
        public Point Position;
        public bool TileX, TileY;
        public byte[] Data;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct RoomView
    {
        public bool IsEnabled;
        public Rectangle View, Port;
        public byte[] Data;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct RoomObject
    {
        public Point Position;
        public uint DefIndex;
        public PointF Scale;
        public float Tint;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct RoomTile
    {
        public Point Position;
        public uint DefIndex;
        public Point SourcePosition;
        public Point Size;
        public Point Scale;
        public float Tint;
    }
}
