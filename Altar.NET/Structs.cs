using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Altar.NET
{
    public enum SectionHeaders : uint
    {
        Form        = 0x4D524F46,
        General     = 0x384E4547,
        Options     = 0x4E54504F,
        Extensions  = 0x4E545845,
        Sounds      = 0x444E4F53,
        Sprites     = 0x54525053,
        Backgrounds = 0x444E4742,
        Paths       = 0x48544150,
        Scripts     = 0x54504353,
        Shaders     = 0x52444853,
        Fonts       = 0x544E4F46,
        Timelines   = 0x4E4C4D54,
        Objects     = 0x544A424F,
        Rooms       = 0x4D4F4F52,
        DataFiles   = 0x4C464144,
        TexInfo     = 0x47415054,
        Code        = 0x45444F43,
        Variables   = 0x49524156,
        Functions   = 0x434E5546,
        Strings     = 0x47525453,
        Textures    = 0x52545854,
        Audio       = 0x4F445541,
        AudioGroup  = 0x50524741,

        Count = 23
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct Pointer : IDisposable
    {
        IntPtr stuff;
        bool disposedValue;

        public IntPtr IPtr => stuff;
        public void* VPtr => (void*)stuff;
        public byte* BPtr => (byte*)stuff;

        public Pointer(byte[] data)
        {
            disposedValue = false;
            stuff = Marshal.AllocHGlobal(data.Length);

            Marshal.Copy(data, 0, stuff, data.Length);
        }

        public void Dispose()
        {
            if (stuff != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(stuff);
                stuff = IntPtr.Zero;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct GMFileContent : IDisposable
    {
        public Pointer RawData;

        public SectionHeader * Base   ;
        public SectionGeneral* Gen    ;
        public SectionOptions* Options;

        public SectionUnknown* Extensions ;
        public SectionUnknown* Sounds     ;
        public SectionUnknown* Sprites    ;
        public SectionUnknown* Paths      ;
        public SectionUnknown* Scripts    ;
        public SectionUnknown* Shaders    ;
        public SectionUnknown* Fonts      ;
        public SectionUnknown* Timelines  ;
        public SectionUnknown* DataFiles  ;
        public SectionUnknown* TexInfo    ;
        public SectionUnknown* AudioGroup ;

        public SectionCountOffset* Objects    ;
        public SectionCountOffset* Rooms      ;
        public SectionCountOffset* Code       ;
        public SectionCountOffset* Strings    ;
        public SectionCountOffset* Textures   ;
        public SectionCountOffset* Audio      ;
        public SectionCountOffset* Backgrounds;

        public SectionRefDefs* Functions;
        public SectionRefDefs* Variables;

        internal fixed uint HeaderOffsets[(int)SectionHeaders.Count];

        public void Dispose()
        {
            RawData.Dispose();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PngInfo
    {
        public uint Width, Height;
        public byte[] DataInfo;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct AudioInfo
    {
        public byte[] RIFF;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ReferenceDef
    {
        public string Name;
        public uint Occurrences;
        public uint FirstAddress;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ObjectInfo
    {
        public string Name;
        public uint SpriteIndex;
        public byte[] Data;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BackgroundInfo
    {
        public string Name;
        public uint TextureAddress;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RoomBackground
    {
        public bool IsEnabled;
        public uint DefIndex;
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
        public PointF Scale;
        public float Tint;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RoomInfo
    {
        public string Name;
        public Point Size;
        public Colour Colour;

        //public RoomBackground[] Backgrounds;
        //public RoomView      [] Views      ;
        //public RoomObject    [] Objects    ;
        //public RoomTile      [] Tiles      ;

        public byte[] Data;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NameDataPair
    {
        public string Name;
        public byte[] Data;
    }
}
