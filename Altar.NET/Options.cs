using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using CommandLine.Text;

namespace Altar
{
    class ExportOptions
    {
        [Option('g', HelpText = "Export manifest file.", MutuallyExclusiveSet = "EXPORT")]
        public bool General
        {
            get;
            set;
        }
        [Option('o', HelpText = "Export options.", MutuallyExclusiveSet = "EXPORT")]
        public bool Options
        {
            get;
            set;
        }
        [Option('n', HelpText = "Export sound definitions (NOTE: this does NOT export the audio files themselves, use -a instead).", MutuallyExclusiveSet = "EXPORT")]
        public bool Sound
        {
            get;
            set;
        }
        [Option('s', HelpText = "Export sprite definitions (NOTE: this does NOT export the texture files themselves, use -t instead).", MutuallyExclusiveSet = "EXPORT")]
        public bool Sprite
        {
            get;
            set;
        }
        [Option('b', HelpText = "Export background definitions.", MutuallyExclusiveSet = "EXPORT")]
        public bool Background
        {
            get;
            set;
        }
        [Option('p', HelpText = "Export path definitions.", MutuallyExclusiveSet = "EXPORT")]
        public bool Path
        {
            get;
            set;
        }
        [Option('i', HelpText = "Export script definitions (NOTE: this does NOT export the decompiled or decompiled code, use -c or -d instead).", MutuallyExclusiveSet = "EXPORT")]
        public bool Script
        {
            get;
            set;
        }
        [Option('f', HelpText = "Export font definitions.", MutuallyExclusiveSet = "EXPORT")]
        public bool Font
        {
            get;
            set;
        }
        [Option('j', HelpText = "Export object definitions.", MutuallyExclusiveSet = "EXPORT")]
        public bool Object
        {
            get;
            set;
        }
        [Option('r', HelpText = "Export room definitions.", MutuallyExclusiveSet = "EXPORT")]
        public bool Room
        {
            get;
            set;
        }
        [Option('m', HelpText = "Export texture maps (NOTE: does not contain the textures themselves, use -t instead).", MutuallyExclusiveSet = "EXPORT")]
        public bool TPag
        {
            get;
            set;
        }
        [Option('t', HelpText = "Export texture sheets.", MutuallyExclusiveSet = "EXPORT")]
        public bool Texture
        {
            get;
            set;
        }
        [Option('a', HelpText = "Export audio files.", MutuallyExclusiveSet = "EXPORT")]
        public bool Audio
        {
            get;
            set;
        }
        [Option('c', HelpText = "Decompile code.", MutuallyExclusiveSet = "EXPORT")]
        public bool Decompile
        {
            get;
            set;
        }
        [Option('d', HelpText = "Disassemble code (NOTE: displays GM:S VMASM, NOT high-level code, use -c instead).", MutuallyExclusiveSet = "EXPORT")]
        public bool Disassemble
        {
            get;
            set;
        }
        [Option('u', HelpText = "Dump strings.", MutuallyExclusiveSet = "EXPORT")]
        public bool String
        {
            get;
            set;
        }
        [Option('w', HelpText = "Dump variable names.", MutuallyExclusiveSet = "EXPORT")]
        public bool Variables
        {
            get;
            set;
        }
        [Option('h', HelpText = "Dump function names.", MutuallyExclusiveSet = "EXPORT")]
        public bool Functions
        {
            get;
            set;
        }
        [Option('*', "any", HelpText = "Export everything (except -d, --dumpempty, --dumpall).", MutuallyExclusiveSet = "EXPORT")]
        public bool Any
        {
            get;
            set;
        }

        [Option("absolute", HelpText = "Use absolute instead of relative offsets in code disassembly/decompilation. Ignored if -c or -d aren't provided.", MutuallyExclusiveSet = "EXPORT")]
        public bool AbsoluteAddresses
        {
            get;
            set;
        }

        [Option("project", HelpText = "Export everything (except -dwhk) to a project-like structure and emit a project file that can be rebuilt (in a later version).", MutuallyExclusiveSet = "PROJ")]
        public bool ExportToProject
        {
            get;
            set;
        }

        [Option("file", Required = true, DefaultValue = "data.win", HelpText = "Specifies the data.win file to export.")]
        public string File
        {
            get;
            set;
        }
        [Option("out", HelpText = "Specifies the output folder.")]
        public string OutputDirectory
        {
            get;
            set;
        }

        [Option('k', "dumpunk", HelpText = "Dumps the contents of unknown chunks. (NOTE: dumps them as binary files *with* chunk header. You'll need a hex editor of some sort to make something useful out of it)", MutuallyExclusiveSet = "EXPORT")]
        public bool DumpUnknownChunks
        {
            get;
            set;
        }
        [Option("dumpempty", HelpText = "Dumps the contents of unknown empty chunks, too.", MutuallyExclusiveSet = "EXPORT")]
        public bool DumpEmptyChunks
        {
            get;
            set;
        }
        [Option("dumpall", HelpText = "Dumps *ALL* chunks, does not imply --dumpempty. (NOTE: <see note of --dumpunk)", MutuallyExclusiveSet = "EXPORT")]
        public bool DumpAllChunks
        {
            get;
            set;
        }

        [Option('q', "quiet", HelpText = "Does not output any info on progress.")]
        public bool Quiet
        {
            get;
            set;
        }
        [Option("noprecprog", HelpText = "Doesn't output precise progress on large chunks.")]
        public bool NoPrecProg
        {
            get;
            set;
        }

        [HelpOption]
        public string GetUsage() => HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
    }
    class Options
    {
        [VerbOption("export", HelpText = "Export contents of a data.win file. See export --help for more info.")]
        public ExportOptions Export
        {
            get;
            set;
        }

        [HelpVerbOption]
        public string GetUsage(string v) => HelpText.AutoBuild(this, v);
    }
}
