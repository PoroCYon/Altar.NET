using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace Altar
{
    /*TODO:
     *
     * *! SectionGeneral
     * *! SectionOption
     * *! TextureEntry
     * *! SpriteEntry
     * *! ObjectEntry
     * *  BgEntry?
     * *! RoomEntry
     * *  RoomObjEntry?
     * *  RoomTileEntry?
     * *  FontEntry?
     * *  CodeEntry?
     *
     */

    [StructLayout(LayoutKind.Sequential, Pack = 1), DebuggerDisplay("{DebugDisplay()}")]
    public unsafe struct CountOffsetsPair
    {
        public uint Count;
        public uint Offsets;

        internal string DebugDisplay() => "{Count=" + SR.HEX_PRE + Count.ToString(SR.HEX_FM8) + SR.C_BRACE;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct RefDefEntry
    {
        public uint NameOffset;
        public uint Occurrences;
        public uint FirstAddress;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct RefDefEntryWithOthers
    {
        public uint NameOffset;
        public int InstanceType;
        public int _pad1; // unknown
        public uint Occurrences;
        public uint FirstAddress;
    }

    // ---

    [StructLayout(LayoutKind.Sequential, Pack = 1), DebuggerDisplay("{DebugDisplay()}")]
    public struct SectionHeader
    {
        public SectionHeaders Identity;
        public uint Size;

        public string MagicString()
        {
            return Identity.ToChunkName();
        }

        internal string DebugDisplay() => Identity.ToString() + SR.SPACE_S + SR.O_PAREN + MagicString() + SR.C_PAREN + SR.SPACE_S + SR.COLON_S + SR.HEX_PRE + Size.ToString(SR.HEX_FM8);
    }

    public static class InfoFlagsExt
    {
        public static byte StudioVersion(this InfoFlags flags)
        {
            flags &= InfoFlags.StudioVersionMask;

            byte r = 0;

            if ((flags & InfoFlags.StudioVersionB1) != 0)
                r |= 1;
            if ((flags & InfoFlags.StudioVersionB2) != 0)
                r |= 2;
            if ((flags & InfoFlags.StudioVersionB3) != 0)
                r |= 4;

            return r;
        }
    }

    [Flags]
    public enum InfoFlags : uint
    {
        Fullscreen = 0x0001,
        SyncVertex1 = 0x0002,
        SyncVertex2 = 0x0004,
        Interpolate = 0x0008,
        Unknown = 0x0010, // seems to be 1 all the time...
        ShowCursor = 0x0020,
        Sizeable = 0x0040,
        ScreenKey = 0x0080,
        SyncVertex3 = 0x0100,
        StudioVersionB1 = 0x0200,
        StudioVersionB2 = 0x0400,
        StudioVersionB3 = 0x0800,
        StudioVersionMask = StudioVersionB1 | StudioVersionB2 | StudioVersionB3,
        SteamEnabled = 0x1000,
        LocalDataEnabled = 0x2000,
        BorderlessWindow = 0x4000

        // others...?
    }
    public enum GameTargets : uint
    {
        None = 0

        // ?
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct SectionGeneral
    {
        public SectionHeader Header;

        public bool Debug; // ?
        public Int24 BytecodeVersion; // probably
        public uint FilenameOffset;
        public uint ConfigOffset;
        public uint LastObj;
        public uint LastTile;
        public uint GameID;
        fixed uint _pad1[4]; // 0, 0, 0, 0
        public uint NameOffset;
        public int Major;
        public int Minor;
        public int Release;
        public int Build;
        public Point WindowSize;
        public InfoFlags Info;
        public fixed byte MD5[0x10];
        public uint CRC32;
        public ulong Timestamp; // UNIX time (64-bit, luckily)
        public uint DisplayNameOffset;
        public GameTargets ActiveTargets;
        public fixed uint _unknown[4]; // unknown, more flags?
        public uint AppID;
        public uint NumberCount;
        public uint Numbers;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct SectionOptions
    {
        public SectionHeader Header;

        public fixed uint _pad0[2]; // flags?
        public InfoFlags GEN8FlagsDup;
        public fixed uint _pad1[0xC];
        public CountOffsetsPair ConstMap;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct SectionUnknown
    {
        public SectionHeader Header;

        public uint Unknown;

        public bool IsEmpty() => Header.Size == 0 || (Header.Size == sizeof(uint) && Unknown == 0);
    }
    [StructLayout(LayoutKind.Explicit, Pack = 1), DebuggerDisplay("{DebugDisplay()}")]
    public unsafe struct SectionCountOffsets
    {
        [FieldOffset(0)]
        public SectionHeader Header;

        [FieldOffset(8)]
        public uint Count;
        [FieldOffset(12)]
        public uint Offsets;

        [FieldOffset(8)]
        public CountOffsetsPair List;

        internal string DebugDisplay() => Header.DebugDisplay() + " -> " + List.DebugDisplay();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1), DebuggerDisplay("{DebugDisplay()}")]
    public unsafe struct SectionRefDefs
    {
        public SectionHeader Header;

        public RefDefEntry Entries;

        internal string DebugDisplay() => Header.DebugDisplay();
    }

    // ---

    [Flags]
    public enum SoundEntryFlags : uint
    {
        Embedded = 0x01, // NotStreamed?
        Compressed = 0x02,
        Normal = 0x04 | 0x20 | 0x40 // all seem to have these flags -> unimportant?
    }
    [Flags]
    public enum RoomEntryFlags
    {
        EnableViews = 1,
        ShowColour = 2,
        ClearDisplayBuffer = 4, // clear display buffer with window colour
        Unknown = 0x20000 // probably signals the extra 32 bytes at the end

        // isometric?
        // clearViewBg?
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct SoundEntry
    {
        public uint NameOffset;
        public SoundEntryFlags Flags;
        public uint TypeOffset;
        public uint FileOffset;
        uint _pad; // effects?
        public float Volume;
        public float Pitch;
        public int GroupID;
        public int AudioID;
    }

    public struct SpriteCollisionMask
    {
        public uint MaskCount;
        public byte MaskData;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct SpriteEntry
    {
        public uint Name;
        public Point Size;

        public BoundingBox2 Bounding;

        fixed uint _pad[3]; // type? coltolerance? htile? vtile? for3D?
        public uint BBoxMode;
        public DwordBool SeparateColMasks;

        public Point Origin;

        public CountOffsetsPair Textures;

        // SpriteCollisionMask
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct SpriteEntry2
    {
        public uint Name;
        public Point Size;

        public BoundingBox2 Bounding;

        fixed uint _pad[3]; // type? coltolerance? htile? vtile? for3D?
        public uint BBoxMode;
        public DwordBool SeparateColMasks;

        public Point Origin;

        // unknown stuff
        public fixed int _pad2[3];
        public float funk;
        uint _pad3;

        public CountOffsetsPair Textures;

        // SpriteCollisionMask
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct BgEntry
    {
        public uint Name;
        fixed uint _pad[3]; // 0, 0, 0
        public uint TextureOffset;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct PathEntry
    {
        public uint Name;
        public DwordBool IsSmooth;
        public DwordBool IsClosed;
        public uint Precision;

        public uint PointCount;
        public PathPoint Points;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct ScriptEntry
    {
        public uint Name;
        public uint CodeId;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct FontEntry
    {
        public uint CodeName, SystemName;
        public uint EmSize;
        public DwordBool Bold;
        public DwordBool Italic;
        // renderhq? includettf? ttfname? WHERE?
        public ushort _ignore0; // ascii range start (probably, only one) -> use Chars
        public byte Charset;
        public FontAntiAliasing AntiAliasing;
        public uint _ignore1; // ascii range end (probably, only one) -> use Chars
        public uint TPagOffset;

        public PointF Scale;

        public CountOffsetsPair Chars;
    }

    public enum CollisionShape : uint
    {
        Circle = 0,
        Box = 1,
        Custom = 2
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct ObjectEntry
    {
        public uint Name;
        public uint SpriteIndex;

        public DwordBool Visible;
        public DwordBool Solid;
        public int Depth;
        public DwordBool Persistent;

        public int ParentId; // OBJT
        public int MaskId; // SPRT

        public DwordBool HasPhysics;
        public DwordBool IsSensor;
        public CollisionShape CollisionShape;

        public ObjectPhysics Physics;

        public ObjectRest Rest;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct RoomEntry
    {
        public uint Name;
        public uint Caption;
        public Point Size;
        public uint Speed;
        public DwordBool Persistent;
        public Colour Colour;

        public DwordBool DrawBackgroundColour;
        public uint _unknown; // unknown (can be -1 -> option?)

        public RoomEntryFlags Flags;

        public uint BgOffset, ViewOffset, ObjOffset, TileOffset;

        public uint World;
        public BoundingBox Bounding;
        public PointF Gravity;
        public float MetresPerPixel;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct RoomObjInstEntry
    {
        public uint Name;
        public uint Index;
        public uint Unk1;
        public uint Unk2;
        fixed uint _pad1[4];
        public uint Unk3;
        public uint InstCount;
        public uint Instances;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct TexPageEntry
    {
        public Rectangle16 Source, Dest;
        public Point16 Size;
        public ushort SpritesheetId;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct CodeEntryE
    {
        public uint Name;
        public int Length;
        public uint Bytecode;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CodeEntryF
    {
        public uint Name;
        public uint Length;
        public uint Probably1;
        public int BytecodeOffset;
        uint Probably0;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct StringEntry
    {
        public uint Length;
        public byte Data;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct TextureEntry
    {
        uint _pad; // unknown, a low int value
        public uint Offset;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct TextureEntry2
    {
        fixed uint _pad[2];
        public uint Offset;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct AudioEntry
    {
        public uint Length;
        public byte Data;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ObjectPhysics
    {
        public float
            Density,
            Restitution,
            Group,
            LinearDamping,
            AngularDamping,
            Unknown0,
            Friction;
        public int Unknown1;
        public float Kinematic;
    }
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public unsafe struct ObjectRest
    {
        // event data?

        [FieldOffset(0)]
        public fixed float MoreFloats[4];
        [FieldOffset(0)]
        public CountOffsetsPair ShapePoints;
        [FieldOffset(0x10)]
        public CountOffsetsPair ShapePoints_IfMoreFloats;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct RoomBgEntry
    {
        public DwordBool IsEnabled, IsForeground;
        public uint DefIndex;
        public Point Position;
        public DwordBool TileX, TileY;
        public Point Speed;
        public DwordBool Stretch;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct RoomViewEntry
    {
        public DwordBool IsEnabled;
        public Rectangle View, Port;
        public Point Border, Speed;

        public uint ObjectId; // can be -1
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct RoomObjEntry
    {
        public Point Position;
        public uint DefIndex;
        public uint InstanceID;
        public uint CreateCodeID; // gml_RoomCC_<name>_<CreateCodeID>_Create
        public PointF Scale;
        public Colour Colour;
        public float Rotation;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct RoomTileEntry
    {
        public Point Position;
        public uint DefIndex;
        public Point SourcePos;
        public Point Size;
        public uint TileDepth;
        public uint InstanceID;
        public PointF Scale;
        public Colour Colour;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct FontCharEntry
    {
        public char Character; // wchar_t
        public Rectangle16 TexPagFrame;
        public ushort Shift;
        public uint Offset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct PathPoint
    {
        public PointF Position;
        public float Speed;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PngChunk
    {
        public const uint ChunkEnd = 0x444E4549; // IEND

        public uint Length, Type;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PngIhdr
    {
        public PngChunk Header;
        public uint Width, Height;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PngHeader
    {
        ulong _pad; // <0x89>PNG <uint length?>
        public PngIhdr IHDR;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct FunctionLocalEntry
    {
        public uint Index;
        public uint Name;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct FunctionLocalsEntry
    {
        public uint LocalsCount;
        public uint FunctionName;
        public FunctionLocalEntry Locals;
    }
}
