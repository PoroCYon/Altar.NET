using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Altar.Decomp;

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
        public uint GameID;
        public string Name;
        public Version Version;
        public Point WindowSize;
        public byte[] LicenseMD5Hash;
        public uint LicenceCRC32;
        public string DisplayName;
        public DateTime Timestamp;

        public GameTargets ActiveTargets;
        public uint SteamAppID;
        public uint[] unknown;
        public InfoFlags InfoFlags;

        public uint[] WeirdNumbers;

        public bool IsOldBCVersion => BytecodeVersion <= 0x0E;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct OptionInfo
    {
        public IDictionary<string, string> Constants;

        public InfoFlags InfoFlags;

        public uint[] _pad0;
        public uint[] _pad1;
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
        public int    GroupID     ;
        /// <summary>
        /// -1 if unused? Only makes sense when embedded or compressed?
        /// </summary>
        public int    AudioID     ;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct SpriteInfo
    {
        public string Name;
        public Point Size;
        public BoundingBox2 Bounding;
        public uint BBoxMode;
        public bool SeparateColMasks;
        public Point Origin;

        public uint[] TextureIndices;
        public bool[][,] CollisionMasks;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct BackgroundInfo
    {
        public string Name;
        public uint? TexPageIndex;
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
        public bool IsBold;
        public bool IsItalic;
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

        public bool IsSensor;
        public CollisionShape CollisionShape;

        public ObjectPhysics? Physics;
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

        public bool DrawBackgroundColour;
        public uint _unknown;

        public bool EnableViews;
        public bool ShowColour;
        public bool ClearDisplayBuffer;

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
        public Point16 Position;
        public Point16 Size;
        public Point16 RenderOffset;
        public Rectangle16 BoundingBox;
        public uint SpritesheetId;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct CodeInfo
    {
        public string Name;
        public AnyInstruction*[] Instructions;
        public int Size;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct TextureInfo
    {
        public uint Width;
        public uint Height;
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
        public bool IsForeground;
        public uint? BgIndex;
        public Point Position;
        public bool TileX;
        public bool TileY;
        public Point Speed;
        public bool StretchSprite;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct RoomView
    {
        public bool IsEnabled;
        public Rectangle View;
        public Rectangle Port;
        public Point Border;
        public Point Speed;
        public uint? ObjectId;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct RoomObject
    {
        public Point Position;
        public uint DefIndex;
        public uint InstanceID;
        public uint CreateCodeID; // gml_RoomCC_<name>_<CreateCodeID>_Create
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
        public uint Depth;
        public uint InstanceID;
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
        public Rectangle16 TPagFrame;
        public ushort Shift;
        public uint Offset;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FunctionLocalsInfo
    {
        public string FunctionName;
        public string[] LocalNames;
    }
}
