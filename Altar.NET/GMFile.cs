using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Altar.Decomp;
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

        // Extensions, AudioGroups, Shaders, Timelines, DataFiles: empty

        public SoundInfo      [] Sound
        {
            get;
        }
        public SpriteInfo     [] Sprites
        {
            get;
        }
        public BackgroundInfo [] Backgrounds
        {
            get;
        }
        public PathInfo       [] Paths
        {
            get;
        }
        public ScriptInfo     [] Scripts
        {
            get;
        }
        public FontInfo       [] Fonts
        {
            get;
        }
        public ObjectInfo     [] Objects
        {
            get;
        }
        public RoomInfo       [] Rooms
        {
            get;
        }
        public TexturePageInfo[] TexturePages
        {
            get;
        }
        public CodeInfo       [] Code
        {
            get;
        }
        public string         [] Strings
        {
            get;
        }
        public TextureInfo    [] Textures
        {
            get;
        }
        public AudioInfo      [] Audio
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
        }
        internal GMFile(GMFileContent f)
        {
            Content = f;

            General = SectionReader.GetGeneralInfo(f);
            Options = SectionReader.GetOptionInfo (f);

            Sound        = Utils.UintRange(0, f.Sounds      ->Count).Select(i => SectionReader.GetSoundInfo   (f, i)).ToArray();
            Sprites      = Utils.UintRange(0, f.Sprites     ->Count).Select(i => SectionReader.GetSpriteInfo  (f, i)).ToArray();
            Backgrounds  = Utils.UintRange(0, f.Backgrounds ->Count).Select(i => SectionReader.GetBgInfo      (f, i)).ToArray();
            Paths        = Utils.UintRange(0, f.Paths       ->Count).Select(i => SectionReader.GetPathInfo    (f, i)).ToArray();
            Scripts      = Utils.UintRange(0, f.Scripts     ->Count).Select(i => SectionReader.GetScriptInfo  (f, i)).ToArray();
            Fonts        = Utils.UintRange(0, f.Fonts       ->Count).Select(i => SectionReader.GetFontInfo    (f, i)).ToArray();
            Objects      = Utils.UintRange(0, f.Objects     ->Count).Select(i => SectionReader.GetObjectInfo  (f, i)).ToArray();
            Rooms        = Utils.UintRange(0, f.Rooms       ->Count).Select(i => SectionReader.GetRoomInfo    (f, i)).ToArray();
            TexturePages = Utils.UintRange(0, f.TexturePages->Count).Select(i => SectionReader.GetTexPageInfo (f, i)).ToArray();
            Code         = Utils.UintRange(0, f.Code        ->Count).Select(i => Disassembler .DisassembleCode(f, i)).ToArray();
            Strings      = Utils.UintRange(0, f.Strings     ->Count).Select(i => SectionReader.GetStringInfo  (f, i)).ToArray();
            Textures     = Utils.UintRange(0, f.Textures    ->Count).Select(i => SectionReader.GetTextureInfo (f, i)).ToArray();
            Audio        = Utils.UintRange(0, f.Audio       ->Count).Select(i => SectionReader.GetAudioInfo   (f, i)).ToArray();

            AudioSoundMap = new Dictionary<uint, uint>();
            for (uint i = 0; i < Sound.Length; i++)
            {
                var s = Sound[i];

                if ((s.IsEmbedded || s.IsCompressed) && s.AudioId != -1)
                    AudioSoundMap[(uint)s.AudioId] = i;
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

                        if (ret.Extensions->Header.Size > 4)
                            Console.WriteLine("Warning: EXTN chunk is not empty!");
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

                        if (ret.Extensions->Header.Size > 4)
                            Console.WriteLine("Warning: SHDR chunk is not empty!");
                        break;
                    case SectionHeaders.Fonts:
                        ret.Fonts = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.Timelines:
                        ret.Timelines = (SectionUnknown*)hdr;

                        if (ret.Extensions->Header.Size > 4)
                            Console.WriteLine("Warning: TMLN chunk is not empty!");
                        break;
                    case SectionHeaders.Objects:
                        ret.Objects = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.Rooms:
                        ret.Rooms = (SectionCountOffsets*)hdr;
                        break;
                    case SectionHeaders.DataFiles:
                        ret.DataFiles = (SectionUnknown*)hdr;

                        if (ret.Extensions->Header.Size > 4)
                            Console.WriteLine("Warning: DAFL chunk is not empty!");
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
                        ret.AudioGroup = (SectionUnknown*)hdr;

                        if (ret.Extensions->Header.Size > 4)
                            Console.WriteLine("Warning: AGRP chunk is not empty!");
                        break;
                    default:
                        if (hdr->Size > 4)
                            Console.WriteLine($"Warning: unexpected {hdr->Identity.ToChunkName()}, chunk is not empty!");

                        break;
                }

                ret.HeaderOffsets[headersMet++] = (byte*)hdr - (byte*)basePtr;
                hdr = unchecked((SectionHeader*)((IntPtr)hdr + (int)hdr->Size) + 1);
            }

            ret.RawData = hdr_bp;

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
        public static bool IsEmpty(SectionHeader* header) => header->Size <= 4;
    }
}
