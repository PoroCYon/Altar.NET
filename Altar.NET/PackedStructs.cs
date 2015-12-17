using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Altar.NET
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SectionHeader
    {
        public SectionHeaders Identity;
        public uint Size;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct SectionGeneral
    {
        [Flags]
        public enum Flags0 : byte
        {
            Fullscreen = 0x1,
            SyncVertex1 = 0x2,
            SyncVertex2 = 0x4,
            Interpolate = 0x8,
            Unknown = 0x10,
            ShowCursor = 0x20,
            Sizeable = 0x40,
            ScreenKey = 0x80
        }
        [Flags]
        public enum Info : byte
        {
            SyncVertex3 = 0x1,
            StudioVersionB1 = 0x2,
            StudioVersionB2 = 0x4,
            StudioVersionB3 = 0x8,
            StudioVersionMask = 0x2 | 0x4 | 0x8,
            SteamEnabled = 0x10,
            LocalDataEnabled = 0x20
        }

        public SectionHeader Header;
        public DwordBool Debug;
        public uint FilenameOffset;
        public uint ConfigOffset;
        public uint LastObj;
        public uint LastTile;
        public uint GameId;
        fixed int _padding[4];
        public uint NameOffset;
        public uint Major;
        public uint Minor;
        public uint Release;
        public uint Build;
        public uint LargestVpw;
        public uint LargestVph;
        public Flags0 flags0;
        public Info info;
        public ushort infoMaskPadding;
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
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct SectionCountOffsets
    {
        public SectionHeader Header;

        public uint Count;
        public uint Offsets;
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

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct RefDefEntry
    {
        public uint Name;
        public uint Occurrences;
        public uint FirstAddress;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct CountOffsetsPair
    {
        public uint Count;
        public uint Offsets;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct SpriteEntry
    {
        public uint Name;
        public Point Size;
        fixed byte _pad[44];
        public uint TextureCount;
        public uint TextureAddresses;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct BgEntry
    {
        public uint Name;
        fixed uint _padding[3];
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
        public byte* Data;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct RoomEntry
    {
        public uint Name, Name2;
        public Point Size;
        fixed uint _pad0[2];
        public Colour Colour;
        fixed byte _pad1[60];

        public CountOffsetsPair Backgrounds;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct TexPageEntry
    {
        public Point16 Position, Size, RenderOffset;
        fixed ushort _pad[4];
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
        public byte* Data;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct TextureEntry
    {
        uint _pad;
        public uint Offset;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct AudioEntry
    {
        public uint Length;
        public byte* Data;
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
        fixed uint _pad0[2];
        public PointF Scale;
        public float Tint;
        uint _pad1;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct RoomTileEntry
    {
        public Point Position;
        public uint DefIndex;
        public Point SourcePos;
        public Point Size;
        fixed uint _pad[2];
        public PointF Scale;
        public float Tint;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PngChunck
    {
        public const uint ChunckEnd = 0x444E4549;

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
        ulong _pad;
        public PngIhdr IHDR;
    }
}
