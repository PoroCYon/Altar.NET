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
        public uint BytecodeVersion;
        public string FileName;
        public string Configuration;
        public uint GameId;
        public string Name;
        public Version Version;
        public Point WindowSize;
        public byte[] LicenseMD5Hash;
        public uint LicenceCRC32;
        public string DisplayName;
        public DateTime Timestamp;

        public uint[] WeirdNumbers;

        public bool CanDisassembleCode => BytecodeVersion <= 0x0E;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct OptionInfo
    {
        public Dictionary<string, string> Constants;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SoundInfo
    {
        public string Name        ;
        public bool   IsEmbedded  ;
        public bool   IsCompressed;
        public string Type        ;
        public string File        ;
        public float  VolumeMod   ;
        public float  PitchMod    ;
        public float  PanMod      ;
        /// <summary>
        /// -1 if unused? Only makes sense when embedded or compressed?
        /// </summary>
        public int    AudioId     ;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct SpriteInfo
    {
        public string Name;
        public Point Size;
        public BoundingBox2 Bounding;
        public uint BBoxMode;
        public uint SepMasks;
        public Point Origin;

        public uint[] TextureIndices;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct BackgroundInfo
    {
        public string Name;
        public uint TexPageIndex;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct PathInfo
    {
        public string Name;
        public bool IsSmooth;
        public bool IsClosed;
        public uint Precision;
        public PathPoint[] Points;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct ScriptInfo
    {
        public string Name;
        public uint CodeId;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct FontInfo
    {
        public string CodeName  ;
        public string SystemName;

        public uint EmSize;
        public bool IsBold, IsItalic;
        public FontAntiAliasing AntiAliasing;
        public byte Charset;
        public uint TexPagId;

        public PointF Scale;

        public FontCharacter[] Characters;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct ObjectInfo
    {
        public string Name;
        public uint SpriteIndex;

        public bool IsVisible;
        public bool IsSolid;
        public int Depth;
        public bool IsPersistent;

        public uint? ParentId ;
        public uint? TexMaskId;

        public ObjectPhysics Physics;
        public float[] OtherFloats;
        public Point[] ShapePoints;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct RoomInfo
    {
        public string Name;
        public string Caption;
        public Point Size;
        public uint Speed;
        public bool IsPersistent;
        public Colour Colour;

        public bool EnableViews;
        public bool ShowColour;

        public uint World;
        public BoundingBox Bounding;
        public PointF Gravity;
        public float MetresPerPixel;

        public RoomBackground[] Backgrounds;
        public RoomView      [] Views      ;
        public RoomObject    [] Objects    ;
        public RoomTile      [] Tiles      ;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct TexturePageInfo
    {
        public Point16 Position, Size, RenderOffset;
        public Rectangle16 BoundingBox;
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
        public bool IsEnabled, IsForeground;
        public uint BgIndex;
        public Point Position;
        public bool TileX, TileY;
        public Point Speed;
        public bool StretchSprite;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct RoomView
    {
        public bool IsEnabled;
        public Rectangle View, Port;
        public Point Border, Speed;
        public uint? ObjectId;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct RoomObject
    {
        public Point Position;
        public uint DefIndex;
        public PointF Scale;
        public Colour Colour;
        public float Rotation;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct RoomTile
    {
        public Point Position;
        public uint DefIndex;
        public Point SourcePosition;
        public Point Size;
        public PointF Scale;
        public Colour Colour;
    }

    public enum FontAntiAliasing : byte
    {
        Off
        // other values
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct FontCharacter
    {
        public char Character;
        public Rectangle16 TexturePageFrame;
        public ushort Shift;
        public uint Offset;
    }
}
