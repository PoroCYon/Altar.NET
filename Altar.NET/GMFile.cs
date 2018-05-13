using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Altar.Decomp;
using Altar.Repack;
using Altar.Unpack;

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

        public FunctionLocalsInfo[] FunctionLocals;

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
        }
        internal GMFile(GMFileContent f)
        {
            Content = f;

            General = SectionReader.GetGeneralInfo(f);
            //Console.Error.WriteLine(General.BytecodeVersion);
            Options = SectionReader.GetOptionInfo (f);

            if (f.Sounds       != null && !f.Sounds->Header.IsEmpty())
                Sound        = Utils.UintRange(0, f.Sounds      ->Count).Select(i => SectionReader.GetSoundInfo   (f, i)).ToArray();
            var toil = SectionReader.BuildTPAGOffsetIndexLUT(f);
            if (f.Sprites      != null && !f.Sprites->Header.IsEmpty())
                Sprites      = Utils.UintRange(0, f.Sprites     ->Count).Select(i => SectionReader.GetSpriteInfo  (f, i, toil)).ToArray();
            if (f.Backgrounds  != null && !f.Backgrounds->Header.IsEmpty())
                Backgrounds  = Utils.UintRange(0, f.Backgrounds ->Count).Select(i => SectionReader.GetBgInfo      (f, i)).ToArray();
            if (f.Paths        != null && !f.Paths->Header.IsEmpty())
                Paths        = Utils.UintRange(0, f.Paths       ->Count).Select(i => SectionReader.GetPathInfo    (f, i)).ToArray();
            if (f.Scripts      != null && !f.Scripts->Header.IsEmpty())
                Scripts      = Utils.UintRange(0, f.Scripts     ->Count).Select(i => SectionReader.GetScriptInfo  (f, i)).ToArray();
            if (f.Fonts        != null && !f.Fonts->Header.IsEmpty())
                Fonts        = Utils.UintRange(0, f.Fonts       ->Count).Select(i => SectionReader.GetFontInfo    (f, i)).ToArray();
            if (f.Objects      != null && !f.Objects->Header.IsEmpty())
                Objects      = Utils.UintRange(0, f.Objects     ->Count).Select(i => SectionReader.GetObjectInfo  (f, i)).ToArray();
            if (f.Rooms        != null && !f.Rooms->Header.IsEmpty())
                Rooms        = Utils.UintRange(0, f.Rooms       ->Count).Select(i => SectionReader.GetRoomInfo    (f, i)).ToArray();
            if (f.TexturePages != null && !f.TexturePages->Header.IsEmpty())
                TexturePages = Utils.UintRange(0, f.TexturePages->Count).Select(i => SectionReader.GetTexPageInfo (f, i)).ToArray();
            if (f.Code         != null && !f.Code->Header.IsEmpty())
                Code         = Utils.UintRange(0, f.Code        ->Count).Select(i => Disassembler .DisassembleCode(f, i)).ToArray();
            if (f.Strings      != null && !f.Strings->Header.IsEmpty())
                Strings      = Utils.UintRange(0, f.Strings     ->Count).Select(i => SectionReader.GetStringInfo  (f, i)).ToArray();
            if (f.Textures     != null && !f.Textures->Header.IsEmpty())
                Textures     = Utils.UintRange(0, f.Textures    ->Count).Select(i => SectionReader.GetTextureInfo (f, i)).ToArray();
            if (f.Audio        != null && !f.Audio->Header.IsEmpty())
                Audio        = Utils.UintRange(0, f.Audio       ->Count).Select(i => SectionReader.GetAudioInfo   (f, i)).ToArray();
            if (f.AudioGroup   != null && !f.AudioGroup->Header.IsEmpty())
                AudioGroups  = Utils.UintRange(0, f.AudioGroup  ->Count).Select(i => SectionReader.GetAudioGroupInfo(f, i)).ToArray();

            AudioSoundMap = new Dictionary<uint, uint>();
            if (Sound != null)
                for (uint i = 0; i < Sound.Length; i++)
                {
                    var s = Sound[i];

                    if ((s.IsEmbedded || s.IsCompressed) && s.AudioID != -1)
                        AudioSoundMap[(uint)s.AudioID] = i;
                }

            var vars = General.IsOldBCVersion ? SectionReader.GetRefDefs(f, f.Variables) : SectionReader.GetRefDefsWithOthers(f, f.Variables);
            var fns  = General.IsOldBCVersion ? SectionReader.GetRefDefs(f, f.Functions) : SectionReader.GetRefDefsWithLength(f, f.Functions);

            RefData = new RefData
            {
                Variables = vars,
                Functions = fns ,

                 VarAccessors = Disassembler.GetReferenceTable(f, vars),
                FuncAccessors = Disassembler.GetReferenceTable(f, fns )
            };

            if (f.Functions->Entries.NameOffset * 12 < f.Functions->Header.Size)
            {
                FunctionLocals = SectionReader.GetFunctionLocals(f, f.Functions);
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
                        ret.General = (SectionGeneral*)hdr;
                        break;
                    case SectionHeaders.Options:
                        ret.Options = (SectionOptions*)hdr;
                        break;
                    case SectionHeaders.Extensions:
                        ret.Extensions = (SectionUnknown*)hdr;

                        if (!ret.Extensions->IsEmpty())
                            Console.WriteLine("Warning: EXTN chunk is not empty, its content will not be exported!");
                        break;
                    case SectionHeaders.Sounds:
                        ret.Sounds = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.Sprites:
                        ret.Sprites = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.Backgrounds:
                        ret.Backgrounds = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.Paths:
                        ret.Paths = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.Scripts:
                        ret.Scripts = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.Shaders:
                        ret.Shaders = (SectionUnknown*)hdr;

                        if (!ret.Shaders->IsEmpty())
                            Console.WriteLine("Warning: SHDR chunk is not empty, its content will not be exported!");
                        break;
                    case SectionHeaders.Fonts:
                        ret.Fonts = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.Timelines:
                        ret.Timelines = (SectionUnknown*)hdr;

                        if (!ret.Timelines->IsEmpty())
                            Console.WriteLine("Warning: TMLN chunk is not empty, its content will not be exported!");
                        break;
                    case SectionHeaders.Objects:
                        ret.Objects = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.Rooms:
                        ret.Rooms = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.DataFiles:
                        ret.DataFiles = (SectionUnknown*)hdr;

                        if (!ret.DataFiles->IsEmpty())
                            Console.WriteLine("Warning: DAFL chunk is not empty, its content will not be exported!");
                        break;
                    case SectionHeaders.TexturePage:
                        ret.TexturePages = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.Code:
                        ret.Code = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.Variables:
                        ret.Variables = (SectionRefDefs*)hdr;
                        break;
                    case SectionHeaders.Functions:
                        ret.Functions = (SectionRefDefs*)hdr;
                        break;
                    case SectionHeaders.Strings:
                        ret.Strings = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.Textures:
                        ret.Textures = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.Audio:
                        ret.Audio = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.AudioGroup:
                        ret.AudioGroup = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.Language:
                        ret.Language = (SectionUnknown*)hdr;

                        if (!ret.Language->IsEmpty())
                            Console.WriteLine("Warning: LANG chunk is not empty, its content will not be exported!");
                        break;
                    case SectionHeaders.GLOB_Unk:
                        ret.GLOB_Unk = (SectionUnknown*)hdr;

                        if (!ret.GLOB_Unk->IsEmpty())
                            Console.WriteLine("Warning: GLOB chunk is not empty, its content will not be exported!");
                        break;
                    default:
                        var unk = (SectionUnknown*)hdr;
                        if (!unk->IsEmpty())
                            Console.WriteLine($"Warning: unknown chunk {hdr->Identity.ToChunkName()}, chunk is not empty, its content will not be exported!");

                        ret.UnknownChunks.Add(hdr->Identity, (IntPtr)unk);
                        break;
                }

                for (int i = 0; i < ret.HeaderOffsets.Length; i++)
                    if (((SectionHeader*)((byte*)basePtr + ret.HeaderOffsets[i]))->Identity == hdr->Identity)
                        Console.WriteLine($"WARNING: chunk {hdr->MagicString()} encountered (at least) twice! Only the last occurrence will be exported! (If you see this message, consider reversing manually.)");

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
