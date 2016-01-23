Altar.NET
======================

GameMaker: Studio `data.win` unpacker and decompiler (non-YYC, specifically made for Undertale), based on [libaltar](https://github.com/kvanberendonck/libaltar) and [Mirrawrs' data site](http://undertale.rawr.ws/) (followed by my corrections/completions [here](https://gist.github.com/PoroCYon/4045acfcad7728b87a0d) and [here](https://gist.github.com/PoroCYon/45f947d576f715de3a4d)).

Contains a lot of pointer-littered spaghetti code, because it's basically a continuation of libaltar, but in C#.

I'm not sure if this counts as 'redistribution of modified [libaltar] source', but including their notice just in case.

## Usage

```
altar <verb>? [--help|-h]
altar [--version|-v]
altar <verb> <options...>
```

(NOTE: use `mono altar <args...>` on mono (should be obvious).)

Verbs:
* `export`: export parts from a `data.win` file. Options: `-[gonsbpiformtacduwh]* --absolute --project --file --out`
  * `file`: Path to the `data.win` file to export.
  * `out`: The output directory.
  * `project`: Emit a project file that can be recompiled by Altar.NET (*In a later version, this is currently not implemented or anything.*).
  * `gonsbpiformtacduwh`: Select which parts of the `data.win` should be exported. Run `altar export --help` for more info.
  * `absolute`: Display absolute instruction offsets in decompiled/disassembled code, instead of relative to the first instruction.

## API

To read a `data.win` file:
```csharp
using Altar;

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
