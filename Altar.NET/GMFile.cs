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
        public GMFileContent Content { get; private set; }

        public GeneralInfo General { get; internal set; }
        public OptionInfo  Options { get; internal set; }
        public GlobalInfo  Globals { get; internal set; }

        public LazyArray<SoundInfo      > Sound         { get; internal set; }
        public LazyArray<SpriteInfo     > Sprites       { get; internal set; }
        public LazyArray<BackgroundInfo > Backgrounds   { get; internal set; }
        public LazyArray<PathInfo       > Paths         { get; internal set; }
        public LazyArray<ScriptInfo     > Scripts       { get; internal set; }
        public LazyArray<FontInfo       > Fonts         { get; internal set; }
        public LazyArray<ObjectInfo     > Objects       { get; internal set; }
        public LazyArray<RoomInfo       > Rooms         { get; internal set; }
        public LazyArray<TexturePageInfo> TexturePages  { get; internal set; }
        public LazyArray<CodeInfo       > Code          { get; internal set; }
        public LazyArray<string         > Strings       { get; internal set; }
        public LazyArray<TextureInfo    > Textures      { get; internal set; }
        public LazyArray<AudioInfo      > Audio         { get; internal set; }
        public LazyArray<string         > AudioGroups   { get; internal set; }
        public LazyArray<ExtensionInfo  > Extensions    { get; internal set; }
        public LazyArray<ShaderInfo     > Shaders       { get; internal set; }
        public LazyArray<TimelineInfo   > Timelines     { get; internal set; }

        public IDictionary<uint, uint> AudioSoundMap { get; }

        public RefData RefData { get; internal set; }

        public FunctionLocalsInfo[] FunctionLocals { get; internal set; }

        public uint[] VariableExtra { get; internal set; }

        public SectionHeaders[] ChunkOrder { get; internal set; }

        internal GMFile()
        {
            AudioSoundMap = new Dictionary<uint, uint>();

            ChunkOrder    = new SectionHeaders[0];
        }

        internal static LazyArray<T> MkLazyArr<T>(SectionCountOffsets* hdr, Func<uint, T> readOne)
        {
            if (hdr == null) return new LazyArray<T>(null, 0);
            bool skip = false;
            return new LazyArray<T>(ind =>
            {
                var fail = new KeyValuePair<bool, T>(false, default(T));
                try
                {
                    if (skip) return fail;
                    return new KeyValuePair<bool, T>(true, readOne(ind));
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Error reading " +
                            hdr->Header.MagicString() + " #" + ind +
                            " - skipping others.");
                    Console.Error.WriteLine(e);
                    skip = true;
                    return fail;
                }
            }, hdr->Count);
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

            if (f.General != null) General = SectionReader.GetGeneralInfo(f);
            if (f.Options != null) Options = SectionReader.GetOptionInfo (f);
            if (f.Globals != null) Globals = SectionReader.GetGlobalInfo (f);

            Sound = MkLazyArr(f.Sounds, i => SectionReader.GetSoundInfo(f, i));

            if (f.TexturePages != null)
            {
                var toil = SectionReader.BuildTPAGOffsetIndexLUT(f);
                Sprites = MkLazyArr(f.Sprites, i => SectionReader.GetSpriteInfo(f, i, toil));
            }

            Backgrounds  = MkLazyArr(f.Backgrounds , i => SectionReader.GetBgInfo        (f, i));
            Paths        = MkLazyArr(f.Paths       , i => SectionReader.GetPathInfo      (f, i));
            Scripts      = MkLazyArr(f.Scripts     , i => SectionReader.GetScriptInfo    (f, i));
            Fonts        = MkLazyArr(f.Fonts       , i => SectionReader.GetFontInfo      (f, i));
            Objects      = MkLazyArr(f.Objects     , i => SectionReader.GetObjectInfo    (f, i));
            Rooms        = MkLazyArr(f.Rooms       , i => SectionReader.GetRoomInfo      (f, i));
            TexturePages = MkLazyArr(f.TexturePages, i => SectionReader.GetTexPageInfo   (f, i));
            Code         = MkLazyArr(f.Code        , i => Disassembler .DisassembleCode  (f, i));
            Strings      = MkLazyArr(f.Strings     , i => SectionReader.GetStringInfo    (f, i));
            Textures     = MkLazyArr(f.Textures    , i => SectionReader.GetTextureInfo   (f, i));
            Audio        = MkLazyArr(f.Audio       , i => SectionReader.GetAudioInfo     (f, i));
            AudioGroups  = MkLazyArr(f.AudioGroup  , i => SectionReader.GetAudioGroupInfo(f, i));
            Extensions   = MkLazyArr(f.Extensions  , i => SectionReader.GetExtensionInfo (f, i));
            Shaders      = MkLazyArr(f.Shaders     , i => SectionReader.GetShaderInfo    (f, i));
            Timelines    = MkLazyArr(f.Timelines   , i => SectionReader.GetTimelineInfo  (f, i));

            AudioSoundMap = new Dictionary<uint, uint>();
            if (f.Sounds != null)
                for (uint i = 0; i < Sound.Length; i++)
                {
                    var s = Sound[i];

                    if ((s.IsEmbedded || s.IsCompressed) && s.AudioID != -1 && s.GroupID == 0)
                        AudioSoundMap[(uint)s.AudioID] = i;
                }

            if (f.General == null)
                return;

            try
            {
                // TODO: do this in a better way
                var vars = General.IsOldBCVersion
                    ? SectionReader.GetRefDefs(f, f.Variables)
                    : SectionReader.GetRefDefsWithOthers(f, f.Variables);
                var fns  = General.IsOldBCVersion
                    ? SectionReader.GetRefDefs(f, f.Functions)
                    : SectionReader.GetRefDefsWithLength(f, f.Functions);

                var varacc = Disassembler.GetReferenceTable(f, vars);
                var fnacc  = Disassembler.GetReferenceTable(f, fns );

                RefData = new RefData
                {
                    Variables     = vars,
                    Functions     = fns,
                    VarAccessors  = varacc,
                    FuncAccessors = fnacc
                };

                if (f.Functions->Entries.NameOffset * 12 < f.Functions->Header.Size)
                    FunctionLocals = SectionReader.GetFunctionLocals(f, f.Functions);
                if (f.Variables != null && !General.IsOldBCVersion)
                    VariableExtra = new uint[]
                    {
                        ((uint*)&f.Variables->Entries)[0],
                        ((uint*)&f.Variables->Entries)[1],
                        ((uint*)&f.Variables->Entries)[2]
                    };
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

        public static GMFile GetFile(byte[] data) => new GMFile(new GMFileContent(data));
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

