using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Altar
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ReferenceDef
    {
        public string Name;
        public uint Occurrences;
        public uint FirstOffset;
    }

    // ---

    [StructLayout(LayoutKind.Sequential)]
    public struct GeneralInfo
    {
        public bool IsDebug;
        public string FileName;
        public string Configuration;
        public uint GameId;
        public string Name;
        public Version Version;
        public Point WindowSize;

        public uint[] WeirdNumbers;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct OptionInfo
    {
        public Dictionary<string, string> Constants;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SoundInfo
    {
        public string Name      ;
        public bool   IsEmbedded;
        public string Type      ;
        public string File      ;
        public float  VolumeMod ;
        public float  PitchMod  ;
        /// <summary>
        /// -1 if unused? Only makes sense when embedded?
        /// </summary>
        public int    AudioId   ;
    }
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
        public uint[] Data;
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
    public struct FontInfo
    {
        public string CodeName  ;
        public string SystemName;

        public uint TexPagId;

        public PointF Scale;

        public FontCharacter[] Characters;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct PathInfo
    {
        public string Name;
        public float[] Data;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RoomBackground
    {
        public bool IsEnabled;
        public uint BgIndex;
        public Point Position;
        public bool TileX, TileY;
        //public byte[] Data;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct RoomView
    {
        public bool IsEnabled;
        public Rectangle View, Port;
        //public byte[] Data;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct RoomObject
    {
        public Point Position;
        public uint DefIndex;
        public PointF Scale;
        public float Tint;
        //public byte[] Data;
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
    public struct FontCharacter
    {
        public char Character;
        public Point16 RelativePosition, Size;
    }
}
