Altar.NET
======================

GameMaker: Studio `data.win` unpacker and decompiler (non-YYC, specifically made for Undertale), based on [libaltar](https://github.com/kvanberendonck/libaltar) and [Mirrawrs' data site](https://pcy.ulyssis.be/undertale/) (followed by my corrections/completions [here](https://gitlab.com/snippets/14944) and [here](https://gitlab.com/snippets/14943)).

Contains a lot of pointer-littered spaghetti code, because it's basically a continuation of libaltar, but in C#.

I'm not sure if this counts as 'redistribution of modified [libaltar] source', but including their notice just in case.

## Building

You can build it from within Visual Studio (or MonoDevelop, or SharpDevelop, ...), or from the command-line:

```
[ms|x]build /m Altar.NET.sln /p:Configuration=[Debug|Release]
```

(NOTE: use `msbuild` on Windows, `xbuild` otherwise)    
(NOTE: using the `Debug` configuration emits debug code, use `Release` for an optimized binary.)    
(NOTE: the binary can be found at `<repo-dir>/bin/<config>/altar.exe`, it has all its dependencies merged into it. For a binary with separate DLLs for the dependencies, use the one in `<repo-dir>/Altar.NET/bin/<config>/altar.exe`.)

## Usage

```
altar <verb>? [--help|-h]
altar [--version|-v]
altar <verb> <options...>
```

(NOTE: use `./altar` if it is not added to your `%PATH%` yet, but resides in the current dir. Not applicable to `CMD`(but you shouldn't be using that).)    
(NOTE: use `mono altar.exe <args...>` on mono (should be obvious).)

Verbs:
* `export`: export parts from a `data.win` file. Options: `-[gonsbpifjrmtacduvhekq]* --any --absolute --project --file --out --dumpunk --dumpempty --dumpall --quiet --noprecprog --detachedagrp --help`
  * `file`: Path to the `data.win` or `audiogroup<n>.dat` file to export.
  * `out`: The output directory.
  * `project`: Emit a project file that can be recompiled by Altar.NET
  * `gonsbpifjrmtacduvhekq`: Select which parts of the `data.win` should be exported. Run `altar export --help` for more info.
  * `absolute`: Display absolute instruction offsets in decompiled/disassembled code, instead of relative to the first instruction.
  * `dumpunk`: Dump the raw contents of all chunks with unknown function.
  * `dumpempty`: Dump empty unknown chunks, too.
  * `dumpall`: Dump *all* (nonempty) raw chunks.
  * `quiet`: be completely silent (unless an error happens).
  * `noprecprog`: Don't report any exact details on the progress, but do output something. Makes things not break completely when cursor position isn't settable.
  * `detachedagrp`: Also dump the contents of an `audiogroup<n>.dat` file residing in the same directory as the `data.win` file.
  * `help`: ...
* `import`: Recompile an Altar.NET project file to a `data.win`.
  * `file`: Path to the to project file.
  * `out`: The output `data.win` file.

## API

To read a `data.win` file:
```csharp
using Altar;
using Altar.Decomp; // for the Disassembler and Decompiler classes

// [...]

using (var f = GMFile.GetFile(path_to_file)) // NOTE: the file's content (as a byte array) can be passed instead
{
    // using 'f' should be straightforward enough with IntelliSense/...

    // disassemble code:
    Disassembler.DisplayInstructions(f, 0); // disassembles the code with ID=0
    // decompile code:
    Decompiler.DecompileCode(f, 0);
}
```
