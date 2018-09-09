using Altar.Decomp;
using Altar.Unpack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static Altar.SR;

namespace Altar
{
    public unsafe class GMFile : IDisposable
    {
        public GMFileContent Content
        {
            get;
            private set;
        }

        public GeneralInfo General
        {
            get;
            internal set;
        }
        public OptionInfo Options
        {
            get;
            internal set;
        }

        // Extensions, Shaders, Timelines, DataFiles, Languages: empty

        public SoundInfo      [] Sound
        {
            get;
            internal set;
        }
        public SpriteInfo     [] Sprites
        {
            get;
            internal set;
        }
        public BackgroundInfo [] Backgrounds
        {
            get;
            internal set;
        }
        public PathInfo       [] Paths
        {
            get;
            internal set;
        }
        public ScriptInfo     [] Scripts
        {
            get;
            internal set;
        }
        public FontInfo       [] Fonts
        {
            get;
            internal set;
        }
        public ObjectInfo     [] Objects
        {
            get;
            internal set;
        }
        public RoomInfo       [] Rooms
        {
            get;
            internal set;
        }
        public TexturePageInfo[] TexturePages
        {
            get;
            internal set;
        }
        public CodeInfo       [] Code
        {
            get;
            internal set;
        }
        public string         [] Strings
        {
            get;
            internal set;
        }
        public TextureInfo    [] Textures
        {
            get;
            internal set;
        }
        public AudioInfo      [] Audio
        {
            get;
            internal set;
        }
        public string         [] AudioGroups
        {
            get;
            internal set;
        }
        public ShaderInfo     [] Shaders
        {
            get;
            internal set;
        }

        public IDictionary<uint, uint> AudioSoundMap
        {
            get;
        }

        public RefData RefData
        {
            get;
            internal set;
        }

        public FunctionLocalsInfo[] FunctionLocals
        {
            get;
            internal set;
        }

        public uint[] VariableExtra
        {
            get;
            internal set;
        }

        public SectionHeaders[] ChunkOrder
        {
            get;
            internal set;
        }

        internal GMFile()
        {
            Options = new OptionInfo
            {
                Constants = new Dictionary<string, string>()
            };

            AudioSoundMap = new Dictionary<uint, uint>();

            RefData = new RefData
            {
                Functions = new ReferenceDef[0],
                Variables = new ReferenceDef[0],

                 VarAccessors = new Dictionary<IntPtr, int>(),
                FuncAccessors = new Dictionary<IntPtr, int>()
            };

            Sound        = new SoundInfo      [0];
            Sprites      = new SpriteInfo     [0];
            Backgrounds  = new BackgroundInfo [0];
            Paths        = new PathInfo       [0];
            Scripts      = new ScriptInfo     [0];
            Fonts        = new FontInfo       [0];
            Objects      = new ObjectInfo     [0];
            Rooms        = new RoomInfo       [0];
            TexturePages = new TexturePageInfo[0];
            Code         = new CodeInfo       [0];
            Strings      = new string         [0];
            Textures     = new TextureInfo    [0];
            Audio        = new AudioInfo      [0];
            AudioGroups  = new string         [0];
            Shaders      = new ShaderInfo     [0];

            FunctionLocals = new FunctionLocalsInfo[0];
            VariableExtra  = new uint              [0];
            ChunkOrder     = new SectionHeaders    [0];
        }
        static T[] TryReadMany<T>(SectionCountOffsets* hdr, Func<uint, T> readOne)
        {
            if (hdr == null || hdr->Header.IsEmpty()) return ArrayExt<T>.Empty;

            uint cnt = hdr->Count;
            List<T> l = new List<T>((int)cnt);

            for (uint i = 0; i < cnt; ++i)
            {
                try
                {
                    l.Add(readOne(i));
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Error reading " +
                            hdr->Header.MagicString() + " #" + i +
                            " - skipping others.");
                    Console.Error.WriteLine(e);

                    break;
                }
            }

            return l.ToArray();
        }
        internal GMFile(GMFileContent f)
        {
            Content = f;

            var orderList = new List<SectionHeaders>(f.HeaderOffsets.Length);
            foreach (long headerOffset in f.HeaderOffsets)
            {
                SectionHeaders tag = ((SectionHeader*)((byte*)f.Form + headerOffset))->Identity;
                if (tag != SectionHeaders.Form)
                    orderList.Add(tag);
            }
            ChunkOrder = orderList.ToArray();

            General = SectionReader.GetGeneralInfo(f);
            //Console.Error.WriteLine(General.BytecodeVersion);
            Options = SectionReader.GetOptionInfo (f);

            Sound = TryReadMany(f.Sounds, i => SectionReader.GetSoundInfo(f, i));

            try
            {
                var toil = SectionReader.BuildTPAGOffsetIndexLUT(f);
                Sprites = TryReadMany(f.Sprites, i => SectionReader.GetSpriteInfo(f, i, toil));
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error building TPAG Offset/Index LUT, can't read sprites.");
                Console.Error.WriteLine(e);
            }

            Backgrounds  = TryReadMany(f.Backgrounds , i => SectionReader.GetBgInfo        (f, i));
            Paths        = TryReadMany(f.Paths       , i => SectionReader.GetPathInfo      (f, i));
            Scripts      = TryReadMany(f.Scripts     , i => SectionReader.GetScriptInfo    (f, i));
            Fonts        = TryReadMany(f.Fonts       , i => SectionReader.GetFontInfo      (f, i));
            Objects      = TryReadMany(f.Objects     , i => SectionReader.GetObjectInfo    (f, i));
            Rooms        = TryReadMany(f.Rooms       , i => SectionReader.GetRoomInfo      (f, i));
            TexturePages = TryReadMany(f.TexturePages, i => SectionReader.GetTexPageInfo   (f, i));
            Code         = TryReadMany(f.Code        , i => Disassembler .DisassembleCode  (f, i));
            Strings      = TryReadMany(f.Strings     , i => SectionReader.GetStringInfo    (f, i));
            Textures     = TryReadMany(f.Textures    , i => SectionReader.GetTextureInfo   (f, i));
            Audio        = TryReadMany(f.Audio       , i => SectionReader.GetAudioInfo     (f, i));
            AudioGroups  = TryReadMany(f.AudioGroup  , i => SectionReader.GetAudioGroupInfo(f, i));
            Shaders      = TryReadMany(f.Shaders     , i => SectionReader.GetShaderInfo    (f, i));

            AudioSoundMap = new Dictionary<uint, uint>();
            if (Sound != null)
                for (uint i = 0; i < Sound.Length; i++)
                {
                    var s = Sound[i];

                    if ((s.IsEmbedded || s.IsCompressed) && s.AudioID != -1)
                        AudioSoundMap[(uint)s.AudioID] = i;
                }

            try
            {
                var vars = General.IsOldBCVersion ? SectionReader.GetRefDefs(f, f.Variables) : SectionReader.GetRefDefsWithOthers(f, f.Variables);
                var fns  = General.IsOldBCVersion ? SectionReader.GetRefDefs(f, f.Functions) : SectionReader.GetRefDefsWithLength(f, f.Functions);

                var varacc = Disassembler.GetReferenceTable(f, vars);
                var fnacc = Disassembler.GetReferenceTable(f, fns);

                RefData = new RefData
                {
                    Variables = vars,
                    Functions = fns,
                    VarAccessors = varacc,
                    FuncAccessors = fnacc
                };

                if (f.Functions->Entries.NameOffset * 12 < f.Functions->Header.Size)
                {
                    FunctionLocals = SectionReader.GetFunctionLocals(f, f.Functions);
                }
                if (f.Variables != null && !General.IsOldBCVersion)
                {
                    VariableExtra = new uint[] {
                        ((uint*)&f.Variables->Entries)[0],
                        ((uint*)&f.Variables->Entries)[1],
                        ((uint*)&f.Variables->Entries)[2]
                    };
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Warning: Can't figure out RefDef pairs. Exception:");
                Console.Error.WriteLine(e);
            }
        }

        public void Dispose()
        {
            if (Content != null)
            {
                Content.Dispose();
                Content = null;
            }
        }

        public static GMFile GetFile(byte[] data)
        {
            var ret = new GMFileContent();

            var hdr_bp = new UniquePtr(data);
            byte* hdr_b = hdr_bp.BPtr;

            var basePtr = (SectionHeader*)hdr_b;

            ret.Form = basePtr;

            if (ret.Form->Identity != SectionHeaders.Form)
                throw new InvalidDataException(ERR_NO_FORM);

            SectionHeader*
                hdr = basePtr + 1,
                hdrEnd = (SectionHeader*)((IntPtr)basePtr + (int)ret.Form->Size);

            int headersMet = 0;

            while (hdr < hdrEnd)
            {
                /*Console.WriteLine(
                        "O=" + ((IntPtr)((byte*)hdr-(byte*)basePtr)).ToString("X")
                    + "\tN=" + hdr->Identity.ToChunkName()
                    + "\tE=" + ((SectionUnknown*)hdr)->IsEmpty()
                );*/
                switch (hdr->Identity)
                {
                    case SectionHeaders.General:
                        ret.General      = (SectionGeneral*)hdr;
                        break;
                    case SectionHeaders.Options:
                        ret.Options      = (SectionOptions*)hdr;
                        break;
                    case SectionHeaders.Sounds:
                        ret.Sounds       = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.Sprites:
                        ret.Sprites      = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.Backgrounds:
                        ret.Backgrounds  = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.Paths:
                        ret.Paths        = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.Scripts:
                        ret.Scripts      = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.Fonts:
                        ret.Fonts        = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.Objects:
                        ret.Objects      = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.Rooms:
                        ret.Rooms        = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.TexturePage:
                        ret.TexturePages = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.Code:
                        ret.Code         = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.Variables:
                        ret.Variables    = (SectionRefDefs*)hdr;
                        break;
                    case SectionHeaders.Functions:
                        ret.Functions    = (SectionRefDefs*)hdr;
                        break;
                    case SectionHeaders.Strings:
                        ret.Strings      = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.Textures:
                        ret.Textures     = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.Audio:
                        ret.Audio        = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.AudioGroup:
                        ret.AudioGroup   = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.Shaders:
                        ret.Shaders      = (SectionCountOffsets*)hdr;
                        break;
                    default:
                        var unk = (SectionUnknown*)hdr;
                        if (!unk->IsEmpty())
                            Console.Error.WriteLine($"Warning: unknown chunk {hdr->Identity.ToChunkName()} is not empty, its content will not be exported!");

                        ret.UnknownChunks.Add(hdr->Identity, (IntPtr)unk);
                        break;
                }

                for (int i = 0; i < ret.HeaderOffsets.Length; i++)
                    if (((SectionHeader*)((byte*)basePtr + ret.HeaderOffsets[i]))->Identity == hdr->Identity)
                        Console.Error.WriteLine($"WARNING: chunk {hdr->MagicString()} encountered (at least) twice! Only the last occurrence will be exported! (If you see this message, consider reversing manually.)");

                if (ret.HeaderOffsets.Length >= headersMet)
                {
                    var ho = ret.HeaderOffsets;

                    Array.Resize(ref ho, (headersMet == ret.HeaderOffsets.Length) ? 1 : (headersMet + 2));

                    ret.HeaderOffsets = ho;
                }

                ret.HeaderOffsets[headersMet++] = (byte*)hdr - (byte*)basePtr;
                hdr = unchecked((SectionHeader*)((IntPtr)hdr + (int)hdr->Size) + 1);
            }

            ret.RawData = hdr_bp;

            //ret.DumpChunkOffs();

            return new GMFile(ret);
        }
        public static GMFile GetFile(string path) => GetFile(File.ReadAllBytes(path));

        [DebuggerStepThrough]
        public static void* PtrFromOffset(GMFileContent file, long offset) => file.RawData.BPtr + offset;
        [DebuggerStepThrough]
        public static SectionHeaders ChunkOf(GMFileContent file, long offset)
        {
            var sorted = file.HeaderOffsets.OrderBy(i => i).ToArray();

            if (sorted.Length == 1)
                return *(SectionHeaders*)PtrFromOffset(file, sorted[0]);

            for (int i = 0; i < sorted.Length - 1 && sorted[i + 1] != 0; i++)
                if (offset == sorted[i] || offset > sorted[i] && (offset < sorted[i + 1] || sorted[i + 1] == 0))
                    return *(SectionHeaders*)PtrFromOffset(file, sorted[i]);

            return SectionHeaders.Form;
        }

        [DebuggerStepThrough]
        public static bool IsEmpty(     SectionHeader* header) => header->Size <= 4;
        [DebuggerStepThrough]
        public static bool IsEmpty(     SectionHeader  header) => header. Size <= 4;
    }
    public static class GMFileExt
    {
        [DebuggerStepThrough]
        public static bool IsEmpty(this SectionHeader  header) => header. Size <= 4;
    }
}
