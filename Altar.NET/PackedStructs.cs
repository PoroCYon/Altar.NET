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
        uint _pad0; // unknown, some flags?
        uint _pad1; // unknown
        public uint Occurrences;
        public uint FirstAddress;
    }

    // ---

    [StructLayout(LayoutKind.Sequential, Pack = 1), DebuggerDisplay("{DebugDisplay()}")]
    public struct SectionHeader
    {
        public SectionHeaders Identity;
        public uint Size;

        internal string DebugDisplay() => Identity.ToString() + SR.COLON_S + SR.HEX_PRE + Size.ToString(SR.HEX_FM8);
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
        Fullscreen        = 0x001,
        SyncVertex1       = 0x002,
        SyncVertex2       = 0x004,
        Interpolate       = 0x008,
        Unknown           = 0x010, // seems to be 1 all the time...
        ShowCursor        = 0x020,
        Sizeable          = 0x040,
        ScreenKey         = 0x080,
        SyncVertex3       = 0x100,
        StudioVersionB1   = 0x200,
        StudioVersionB2   = 0x400,
        StudioVersionB3   = 0x800,
        StudioVersionMask = StudioVersionB1 | StudioVersionB2 | StudioVersionB3,
        SteamEnabled      = 0x1000,
        LocalDataEnabled  = 0x2000

        // others...?
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct SectionGeneral
    {
        public SectionHeader Header;

        public bool Debug; // ?
        public Int24 BytecodeVersion; // probably (could also be build, but it only works with HIGHER builds): // >0x00000E -> DisassembleCode breaks
        public uint FilenameOffset;
        public uint ConfigOffset;
        public uint LastObj;
        public uint LastTile;
        public uint GameId;
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
        public uint ActiveTargets;
        fixed uint _pad2[5]; // unknown (0, *, 0, *, *) -> more flags?
        public uint NumberCount;
        public uint Numbers;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct SectionOptions
    {
        public SectionHeader Header;

        /*
            uint color_depth;
            uint resolution;
            uint frequency;
            uint buttonless;?
            uint sync_vertex;
            bool fullscreen_key; (1 bit)
            bool help_key;
            bool quit_key;
            bool save_key;
            bool screenshot_key;
            bool close_secondary;
            uint process_priority;
            bool freeze_lose_focus;
            bool? show_load_progress;
            uint mbe_splash_bg_offset;?
            uint mbe_splash_fg_offset;?
            uint mbe_splash_ld_offset;?
            bool load_transparency;
            byte? load_alpha;
            bool scale_load_progress;
            bool display_errors;
            bool write_errors;
            bool abort_errors;
            uint treat_uninit_zero;?
            uint creation_event_order;?
        */

        fixed uint _pad[0xF]; // unknown: 0x80000000, 2, 0x00CC7A1#, 0, *, 0, 0, 0, 0, *, 0, 0, 0, 0, 0x000000FF
        public CountOffsetsPair ConstMap;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct SectionUnknown
    {
        public SectionHeader Header;

        public uint Unknown;
    }
    [StructLayout(LayoutKind.Explicit  , Pack = 1), DebuggerDisplay("{DebugDisplay()}")]
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
        Embedded   = 0x01, // NotStreamed?
        Compressed = 0x02,
        Normal     = 0x04 | 0x20 | 0x40 // all seem to have these flags -> unimportant?
    }
    [Flags]
    public enum RoomEntryFlags
    {
        EnableViews = 1,
        ShowColour  = 2

        // isometric?
        // clearViewBg?
        // clearDisplayBuf?
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct SoundEntry
    {
        public uint NameOffset;
        public SoundEntryFlags Flags;
        public uint TypeOffset;
        public uint FileOffset;
        public uint _pad; // effects?
        public float Volume;
        public float Pitch ;
        public float Pan   ; // probably
        public int AudioId;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct SpriteEntry
    {
        public uint Name;
        public Point Size;

        public BoundingBox2 Bounding;

        fixed uint _pad[3]; // type? coltolerance? htile? vtile? for3D?
        public uint BBoxMode;
        public uint SepMasks;

        public Point Origin;

        public CountOffsetsPair Textures;
        // colkind?
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
        ushort _ignore0; // ascii range start (probably, only one) -> use Chars
        public byte Charset;
        public FontAntiAliasing AntiAliasing;
        uint _ignore1; // ascii range end (probably, only one) -> use Chars
        public uint TPagOffset;

        public PointF Scale;

        public CountOffsetsPair Chars;
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
        public int MaskId  ; // SPRT

        fixed uint _pad[3]; // 0, 0, 0

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

        // isometric? [hv]snap? clearViewBackground? clearDisplayBuffer?
        fixed uint _pad[2]; // unknown: 1, * (can be -1 -> option?)

        public RoomEntryFlags Flags;

        public uint BgOffset, ViewOffset, ObjOffset, TileOffset;

        public uint World;
        public BoundingBox Bounding;
        public PointF Gravity;
        public float MetresPerPixel;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct TexPageEntry
    {
        public Point16 Position, Size, RenderOffset;
        public Rectangle16 BoundingBox;
        public ushort SpritesheetId;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct CodeEntry
    {
        public uint Name;
        public int Length;
        public uint Bytecode;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct StringEntry
    {
        public uint Length;
        public byte Data  ;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct TextureEntry
    {
        uint _pad; // unknown, a low int value
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
            Density       ,
            Restitution   ,
            Group         ,
            LinearDamping ,
            AngularDamping,
            Unknown0      ,
            Friction      ,
            Unknown1      ,
            Kinematic     ;
    }
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public unsafe struct ObjectRest
    {
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

        public int ObjectId; // can be -1
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct RoomObjEntry
    {
        public Point Position;
        public uint DefIndex;
        uint _count; // 100000 (0x186A0), keeps increasing by  per element
        uint _pad; // -1 (locked?)
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
        uint _magicnum; // somewhere near 1000000 (log), usually
        uint _count; // 10000000, keeps increasing by 1 per element
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
    public struct CodeInfo_VersionF
    {
        public uint Name;
        public uint Length;
        uint Probably1;
        public int BytecodeOffset;
        uint Probably0;
    }
}
