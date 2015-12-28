using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace Altar.NET
{
    /*TODO: Most important things atm:
     *
     * * ObjectEntry unknowns (code?)
     * * SpriteEntry unknowns
     * * weird TPAG unknowns (offsets?)
     * * RoomEntry unknowns
     * * RoomBgEntry unknowns
     * * RoomViewentry unknowns
     * * RoomObjEntry unknowns
     * * FontEntry/FontCharEntry unknowns
     * * PathEntry unknowns
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
        public uint Name;
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

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct SectionGeneral
    {
        [Flags]
        public enum GraphicsFlags : byte
        {
            Fullscreen  = 0x01,
            SyncVertex1 = 0x02,
            SyncVertex2 = 0x04,
            Interpolate = 0x08,
            Unknown     = 0x10,
            ShowCursor  = 0x20,
            Sizeable    = 0x40,
            ScreenKey   = 0x80
        }
        [Flags]
        public enum InfoFlags : byte
        {
            SyncVertex3       = 0x01,
            StudioVersionB1   = 0x02,
            StudioVersionB2   = 0x04,
            StudioVersionB3   = 0x08,
            StudioVersionMask = StudioVersionB1 | StudioVersionB2 | StudioVersionB3,
            SteamEnabled      = 0x10,
            LocalDataEnabled  = 0x20
        }

        public SectionHeader Header;

        public DwordBool Debug;
        public uint FilenameOffset;
        public uint ConfigOffset;
        public uint LastObj;
        public uint LastTile;
        public uint GameId;
        fixed uint _padding[4];
        public uint NameOffset;
        public uint Major;
        public uint Minor;
        public uint Release;
        public uint Build;
        public uint LargestVpw;
        public uint LargestVph;
        public GraphicsFlags Graphics;
        public InfoFlags Info;
        public ushort InfoMaskPadding;
        public uint LicenseKeyCrc32;
        public uint LicenseMD5Offset;
        public ulong Timestamp;
        public uint DisplayOffset;
        public ulong ActiveTargets;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SectionOptions
    {
        public SectionHeader Header;

        public DwordBool Fullscreen;
        public uint Interpolate;
        public DwordBool UseNewAudio;
        public DwordBool Borderless;
        public DwordBool ShowCursor;
        public uint Scale;
        public DwordBool Sizeable;
        public DwordBool StayOnTop;
        public uint WindowColor;
        public uint ChangeResolution;
        public uint ColorDepth;
        public uint Resolution;
        public uint Frequency;
        public DwordBool Buttonless;
        public DwordBool SyncVertex;
        public uint FullscreenKey;
        public uint HelpKey;
        public uint QuitKey;
        public uint SaveKey;
        public uint ScreenshotKey;
        public uint SecondaryQuitKey;
        public int ProcessPriority;
        public uint FreezeLoseFoocus;
        public uint ShadowLoadProgress;
        public uint MBESplashBGOffset;
        public uint MBESplashFGOffset;
        public uint MBESplashLDOffset;
        public DwordBool LoadTransparency;
        public DwordBool LoadAlpha;
        public DwordBool ScaleToProgress;
        public DwordBool DisplayErrors;
        public DwordBool WriteErrors;
        public DwordBool AbortErrors;
        public DwordBool TreatUninitZero;
        public DwordBool CreationEventOrder;
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
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct SectionRefDefs
    {
        public SectionHeader Header;

        /// <summary>
        /// Take address, cast back to RefDefEntry*, use as array.
        /// </summary>
        public RefDefEntry* Entries;
    }

    // ---

    [Flags]
    public enum SoundEntryFlags : uint
    {
        Embedded = 0x01,
        Normal   = 0x04 | 0x20 | 0x40 // all seem to have these flags -> unimportant?
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct SoundEntry
    {
        public uint NameOffset;
        public SoundEntryFlags Flags;
        public uint TypeOffset;
        public uint FileOffset;
        uint _pad0; // 0
        public float Volume;
        public float Pitch ;
        uint _pad1; // 0
        public int AudioId;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct SpriteEntry
    {
        public uint Name;
        public Point Size;
        fixed uint _pad[11]; // mostly zeroes, but some other values, too...
        public uint TextureCount;
        public uint TextureAddresses;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct BgEntry
    {
        public uint Name;
        fixed uint _pad[3]; // 0 0 0
        public uint TextureOffset;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct ScriptEntry
    {
        public uint Name;
        public uint CodeId;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct ObjectEntry
    {
        public uint Name;
        public uint SpriteIndex;

        uint
            _pad0, // unknown
            _pad1, // 0
            _pad3, // unknkown
            _pad4, // 0
            _pad5; // unknown
        fixed uint  _pad6[4]; // -1 0 0 0
        fixed float _pad7[7]; // unknown
        fixed float _pad8[2]; // 1 0

        public CountOffsetsPair AList;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct RoomEntry
    {
        public uint Name, Name2;
        public Point Size;
        fixed uint _pad0[2]; // unknown
        public Colour Colour;
        fixed uint _pad1[3]; // unknown
        public uint BgOffset, ViewOffset, ObjOffset, TileOffset;
        fixed uint _pad2[3]; // unknown
        Point _pad_size; // 1024 * 768
        uint _pad3; // unknown
        fixed float _pad_flt[2]; // 10, 0.1
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct TexPageEntry
    {
        public Point16 Position, Size, RenderOffset;
        fixed uint _pad[2]; // two uints that look like offsets pointing to somewhere in TXTR, but not sure what exactly
        public ushort SpritesheetId;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct CodeEntry
    {
        public uint Name;
        public int Length;
        public byte* Bytecode;
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
        uint _pad; // 1
        public uint Offset;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct AudioEntry
    {
        public uint Length;
        public byte* Data;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct FontEntry
    {
        public uint CodeName, SystemName;
        uint _pad0; // unknown
        fixed uint _pad1[4]; // 0x00000000 0x00000000 0x00010020 0x0000007F
        public uint TPagOffset;

        public PointF Scale;

        public CountOffsetsPair Chars;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct PathEntry
    {
        public uint Name;
        fixed uint _pad0[4]; // unknown
        public float Data;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct RoomBgEntry
    {
        public const int DataLength = 12;

        public DwordBool IsEnabled;
        uint _pad;
        public uint DefIndex;
        public Point Position;
        public DwordBool TileX, TileY;
        public fixed byte Data[DataLength];
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct RoomViewEntry
    {
        public const int DataLength = 20;

        public DwordBool IsEnabled;
        public Rectangle View, Port;
        public fixed byte Data[DataLength];
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct RoomObjEntry
    {
        public Point Position;
        public uint DefIndex;
        fixed uint _pad0[2]; // some value (offset? to SOND??); -1
        public PointF Scale;
        uint _pad1; // -1
        public float Tint;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct RoomTileEntry
    {
        public Point Position;
        public uint DefIndex;
        public Point SourcePos;
        public Point Size;
        Point _pad;
        public PointF Scale;
        public float Tint;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct FontCharEntry
    {
        public char Character; // wchar_t
        public Point16 RelativePos; // relative to TPAG
        public Point16 Size;
        ushort _pad0; // unknown
        uint   _pad1; // unknown
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PngChunck
    {
        public const uint ChunckEnd = 0x444E4549; // IEND

        public uint Length, Type;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PngIhdr
    {
        public PngChunck Header;
        public uint Width, Height;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PngHeader
    {
        ulong _pad; // <0x89>PNG <uint length?>
        public PngIhdr IHDR;
    }
}
