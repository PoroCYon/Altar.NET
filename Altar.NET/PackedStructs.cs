﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace Altar
{
    /*TODO:
     *
     * * SectionGeneral unknowns
     * * SectionOption unknowns
     * * ObjectEntry unknowns (code?)
     * * SpriteEntry unknowns
     * * weird TPAG unknowns (offsets?)
     * * RoomEntry unknowns
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
        public enum InfoFlags : uint
        {
            Fullscreen        = 0x001,
            SyncVertex1       = 0x002,
            SyncVertex2       = 0x004,
            Interpolate       = 0x008,
            Unknown           = 0x010, // debug?
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
        }

        public SectionHeader Header;

        public bool Debug; // ?
        Int24 _pad0; // unknown (0x00000E)
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
        fixed uint _pad2[5]; // unknown (0, 0x00000016, 0, 0xFFFA068C 0x00001966)
        public uint NumberCount;
        public uint Numbers;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct SectionOptions
    {
        public SectionHeader Header;

        fixed uint _pad0[2]; // both unknown (0x80000000 (flags?), 2)
        public uint SomeOffset; // TXTR?? (0x00CC7A14)
        fixed uint _pad1[0xB]; // 0, -1, 0, 0, 0, 0, 1, 0, 0, 0, 0
        uint _pad2; // unknown (0x000000FF)
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
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct SectionRefDefs
    {
        public SectionHeader Header;

        public RefDefEntry Entries;
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
        fixed uint _pad0[4]; // unknown
        fixed uint _pad1[3]; // 0, 0, 0
        fixed uint _pad2[4]; // unknown
        public uint TextureCount;
        public uint TextureAddresses;
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
        fixed uint _pad0[4]; // unknown
        public float Data;
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
        uint _pad0; // unknown
        fixed uint _pad1[4]; // 0x00000000 0x00000000 0x00010020 0x0000007F
        public uint TPagOffset;

        public PointF Scale;

        public CountOffsetsPair Chars;
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
        fixed uint  _pad6[4]; // -1, 0, 0, 0
        fixed float _pad7[7]; // unknown
        fixed float _pad8[2]; // 1, 0

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
        public Point16 AnotherSize, YetAnotherSize; // unknown function...
        public ushort SpritesheetId;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct CodeEntry
    {
        public uint Name;
        public int Length;
        public byte Bytecode;
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
        public byte Data;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct RoomBgEntry
    {
        public DwordBool IsEnabled;
        uint _pad0; // 0
        public uint DefIndex;
        public Point Position;
        public DwordBool TileX, TileY;
        fixed uint _pad1[3]; // 0, 0, 0
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct RoomViewEntry
    {
        public DwordBool IsEnabled;
        public Rectangle View, Port;
        fixed uint _pad[5]; // 32, 32, -1, -1, -1
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct RoomObjEntry
    {
        public Point Position;
        public uint DefIndex;
        uint _pad0; // a number increasing by one everytime (probably not an offset)
        uint _pad1; // -1
        public PointF Scale;
        uint _pad2; // -1
        public float Tint;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct RoomTileEntry
    {
        public Point Position;
        public uint DefIndex;
        public Point SourcePos;
        public Point Size;
        Point _pad; // a really high value, {X=1000000, Y=10000000}, Y keeps increasing by 1 per element
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
