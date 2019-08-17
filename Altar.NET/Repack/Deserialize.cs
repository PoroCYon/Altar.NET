using Altar.Decomp;
using Altar.Recomp;
using LitJson;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using static Altar.SR;

namespace Altar.Repack
{
    public static class Deserialize
    {
        static T[] DeserializeArray<T>(JsonData j, Func<JsonData, T> converter)
        {
            if (j == null)
                throw new ArgumentNullException(nameof(j));
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));

            var r = new T[j.Count];

            for (int i = 0; i < j.Count; i++)
                r[i] = converter(j[i]);

            return r;
        }

        static Colour ParseColour(JsonData j)
        {
            switch (j.JsonType)
            {
                case JsonType.Int:
                    return new Colour((uint)j);
                case JsonType.Long:
                    return new Colour((uint)(long)j);
                case JsonType.String:
                    {
                        var s = (string)j;

                        if (s.Length != 9 && s.Length != 7 /* (AA)RRGGBB + a '#' */ && (s.Length > 0 && !s.StartsWith(SR.HASH, StringComparison.Ordinal)))
                            throw new FormatException("Invalid colour format: string must be of format '#(AA)RRGGBB'.");

                        var i = UInt32.Parse(s.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

                        if (s.Length == 7)
                            i |= 0xFF000000; // make it opaque

                        return new Colour(i);
                    }
                case JsonType.Array:
                    if (j.Count != 4 && j.Count != 3)
                        throw new FormatException($"Invalid colour format: array lenght must be 3 (rgb) or 4 (argb).");
                    {
                        var c = DeserializeArray(j, jd => (byte)jd);

                        if (c.Length == 3)
                            return new Colour(0xFF, c[0], c[1], c[2]);
                    }
                    break;
                case JsonType.Object:
                    return new Colour(j.Has("a") ? (byte)j["a"] : (byte)0xFF,
                        (byte)j["r"], (byte)j["g"], (byte)j["b"]);
            }

            throw new FormatException($"Invalid colour format: invalid type {j.JsonType}");
        }

        static Point        DeserializePoint  (JsonData j) => new Point  ((int)   j["x"], (int)   j["y"]);
        static PointF       DeserializePointF (JsonData j) => new PointF ((float) j["x"], (float) j["y"]);
        static Point16      DeserializePoint16(JsonData j) => new Point16((ushort)j["x"], (ushort)j["y"]);
        static Point        DeserializeSize   (JsonData j) => new Point  ((int)   j["width"], (int)   j["height"]);
        static PointF       DeserializeSizeF  (JsonData j) => new PointF ((float) j["width"], (float) j["height"]);
        static Point16      DeserializeSize16 (JsonData j) => new Point16((ushort)j["width"], (ushort)j["height"]);
        static Rectangle    DeserializeRect   (JsonData j) => new Rectangle   ((int)   j["x"], (int)   j["y"], (int)   j["width"], (int)   j["height"]);
        static Rectangle16  DeserializeRect16 (JsonData j) => new Rectangle16 ((ushort)j["x"], (ushort)j["y"], (ushort)j["width"], (ushort)j["height"]);
        static BoundingBox  DeserializeBBox   (JsonData j) => new BoundingBox ((uint)j["top"], (uint)j["left"], (uint)j["right"], (uint)j["bottom"]);
        static BoundingBox2 DeserializeBBox2  (JsonData j) => new BoundingBox2((uint)j["left"], (uint)j["right"], (uint)j["bottom"], (uint)j["top"]);
        static PathPoint    DeserializePathPt (JsonData j) => new PathPoint { Position = DeserializePointF(j["pos"]), Speed = (float)j["speed"] };

        #region static FontCharacter DeserializeFontChar  (JsonData j)
        static FontCharacter DeserializeFontChar(JsonData j) => new FontCharacter
        {
            Character = (char)j["char"],
            TPagFrame = DeserializeRect16(j["frame"]),
            Shift     = (ushort)j["shift"],
            Offset    = (uint)j["offset"]
        };
        #endregion
        #region static ObjectPhysics DeserializeObjPhysics(JsonData j)
        static ObjectPhysics DeserializeObjPhysics(JsonData j) => new ObjectPhysics
        {
            Density        = (float)j["density"],
            Restitution    = (float)j["restitution"],
            Group          = (float)j["group"],
            LinearDamping  = (float)j["lineardamp"],
            AngularDamping = (float)j["angulardamp"],
            Friction       = (float)j["friction"],
            Kinematic      = (float)j["kinematic"],
            Unknown0       = (int)j["unk0"],
            Unknown1       = (int)j["unk1"]
        };
        #endregion

        static RoomBackground DeserializeRoomBg(JsonData j, BackgroundInfo[] bgs)
        {
            var r = new RoomBackground
            {
                IsEnabled     = (bool)j["enabled"],
                IsForeground  = (bool)j["foreground"],
                TileX         = (bool)j["tilex"],
                TileY         = (bool)j["tiley"],
                StretchSprite = (bool)j["stretch"],

                Position = DeserializePoint(j["pos"]),
                Speed    = DeserializePoint(j["speed"])
            };

            if (j.Has("bg"))
            {
                var i = Array.FindIndex(bgs, b => b.Name == (string)j["bg"]);

                if (i > -1)
                    r.BgIndex = (uint)i;
                //TODO: emit warning instead
            }

            return r;
        }
        static RoomObjInst DeserializeRoomObjInst(JsonData j)
        {
            return new RoomObjInst
            {
                Index     = (uint)j["index"],
                Instances = DeserializeArray(j["instances"], i => (uint)i),
                Name      = (string)j["name"],
                Unk1      = (uint)j["unk1" ],
                Depth     = (uint)j["depth"],
                Unk3      = (uint)j["unk3" ]
            };
        }

        static RoomView DeserializeRoomView(JsonData j, ObjectInfo[] objs)
        {
            var r = new RoomView
            {
                IsEnabled = (bool)j["enabled"],
                View      = DeserializeRect(j["view"]),
                Port      = DeserializeRect(j["port"]),
                Border    = DeserializePoint(j["border"]),
                Speed     = DeserializePoint(j["speed"])
            };

            if (j.Has("obj"))
            {
                var i = Array.FindIndex(objs, oi => oi.Name == (string)j["obj"]);

                if (i > -1)
                    r.ObjectId = (uint)i;
                //TODO: emit warning instead
            }

            return r;
        }
        static RoomObject DeserializeRoomObj(JsonData j, ObjectInfo[] objs)
        {
            var r = new RoomObject
            {
                Position     = DeserializePoint(j["pos"]),
                Scale        = DeserializePointF(j["scale"]),
                Colour       = ParseColour(j["colour"]),
                Rotation     = (float)j["rotation"],
                InstanceID   = (uint)j["instanceid"],
                CreateCodeID = (uint)j["createcodeid"]
            };

            var i = Array.FindIndex(objs, o => o.Name == (string)j["obj"]);
            if (i > -1)
                r.DefIndex = (uint)i;
            //TODO: emit warning instead

            return r;
        }
        static RoomTile DeserializeRoomTile(JsonData j, BackgroundInfo[] bgs)
        {
            var r = new RoomTile
            {
                Position       = DeserializePoint(j["pos"]),
                SourcePosition = DeserializePoint(j["sourcepos"]),
                Size           = DeserializeSize(j["size"]),
                Scale          = DeserializePointF(j["scale"]),
                Colour         = ParseColour(j["colour"]),
                Depth          = (uint)j["tiledepth"],
                InstanceID     = (uint)j["instanceid"]
            };

            var i = Array.FindIndex(bgs, b => b.Name == (string)j["bg"]);
            if (i > -1)
                r.DefIndex = (uint)i;
            //TODO: emit warning instead

            return r;
        }

        #region public static GeneralInfo DeserializeGeneral(JsonData j)
        public static GeneralInfo DeserializeGeneral(JsonData j) => new GeneralInfo
        {
            IsDebug         = (bool)j["debug"],
            BytecodeVersion = (uint)j["bytecodeversion"],
            FileName        = (string)j["filename"],
            Configuration   = (string)j["config"],
            GameID          = (uint)j["gameid"],
            Name            = (string)j["name"],
            Version         = new Version((string)j["version"]),
            WindowSize      = DeserializeSize(j["windowsize"]),
            LicenseMD5Hash  = DeserializeArray(j["licensemd5"], jd => (byte)jd),
            LicenceCRC32    = (uint)j["licensecrc32"],
            DisplayName     = (string)j["displayname"],
            Timestamp       = DateTime.Parse((string)j["timestamp"], CultureInfo.InvariantCulture),

            ActiveTargets = (GameTargets)Enum.Parse(typeof(GameTargets), (string)j["targets"], true),
            unknown       = DeserializeArray(j["unknown"], jd => (uint)jd),
            SteamAppID    = (uint)j["appid"],
            InfoFlags     = (InfoFlags)Enum.Parse(typeof(InfoFlags), (string)j["flags"], true),

            WeirdNumbers = DeserializeArray(j["numbers"], jd => (uint)jd)
        };
        #endregion
        #region public static OptionInfo  DeserializeOptions(JsonData j)
        public static OptionInfo DeserializeOptions(JsonData j) => new OptionInfo
        {
            Constants = j["constants"].ToDictionary()
                .ToDictionary(kvp => kvp.Key, kvp => (string)kvp.Value),

            InfoFlags = (InfoFlags)Enum.Parse(typeof(InfoFlags), (string)j["flags"], true),

            _pad0 = DeserializeArray(j["pad0"], jd=>(uint)jd),
            _pad1 = DeserializeArray(j["pad1"], jd => (uint)jd)
        };
        #endregion

        public static bool[,] DeserializeColMask(JsonData j)
        {
            var w = (int)j["w"];
            var h = (int)j["h"];

            var a = j["data"];

            //TODO: check of h == a.Count

            bool[,] ret = new bool[w, h];

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    ret[x, y] = (bool)a[y][x];

            return ret;
        }

        #region public static SoundInfo      DeserializeSound (JsonData j)
        public static SoundInfo DeserializeSound(JsonData j) => new SoundInfo
        {
            IsEmbedded   = (bool)j["embedded"],
            IsCompressed = (bool)j["compressed"],
            Type         = (string)j["type"],
            File         = (string)j["file"],
            VolumeMod    = (float)j["volume"],
            PitchMod     = (float)j["pitch"],
            Group        = (string)j["group"],
            AudioID      = (int)j["audioid"]
        };
        #endregion
        #region public static SpriteInfo     DeserializeSprite(JsonData j)
        public static SpriteInfo DeserializeSprite(JsonData j) => new SpriteInfo
        {
            BBoxMode         = (uint)j["bboxmode"],
            SeparateColMasks = (bool)j["sepmasks"],
            Size             = DeserializeSize(j["size"]),
            Bounding         = DeserializeBBox2(j["bounding"]),
            Origin           = DeserializePoint(j["origin"]),
            TextureIndices   = j.Has("textures") ? DeserializeArray(j["textures"], jd => (uint)jd) : null,
            CollisionMasks   = j.Has("colmasks") ? DeserializeArray(j["colmasks"], DeserializeColMask) : null,
            Version          = j.Has("unknown1") ? 2 : 1,
            UnknownFloat     = j.Has("unknown1") ? (float)j["unknown1"] : 0.0f
        };
        #endregion
        #region public static BackgroundInfo DeserializeBg    (JsonData j)
        public static BackgroundInfo DeserializeBg(JsonData j) => new BackgroundInfo
        {
            TexPageIndex = j.Has("texture") ? (uint?)j["texture"] : null
        };
        #endregion
        #region public static PathInfo       DeserializePath  (JsonData j)
        public static PathInfo DeserializePath(JsonData j) => new PathInfo
        {
            IsSmooth  = (bool)j["smooth"],
            IsClosed  = (bool)j["closed"],
            Precision = (uint)j["precision"],
            Points    = DeserializeArray(j["points"], (Func<dynamic, PathPoint>)(d => DeserializePathPt(d)))
        };
        #endregion
        public static ScriptInfo DeserializeScript(JsonData j, CodeInfo[] code)
        {
            var i = String.IsNullOrEmpty((string)j["code"]) ? -1 : Array.FindIndex(code, c => c.Name == (string)j["code"]);

            return new ScriptInfo { CodeId = i < 0 ? UInt32.MaxValue : (uint)i };
        }
        #region public static FontInfo       DeserializeFont  (JsonData j)
        public static FontInfo DeserializeFont(JsonData j) => new FontInfo
        {
            SystemName   = (string)j["sysname"],
            EmSize       = (uint)j["emsize"],
            IsBold       = (bool)j["bold"],
            IsItalic     = (bool)j["italic"],
            AntiAliasing = (FontAntiAliasing)Enum.Parse(typeof(FontAntiAliasing), (string)j["antialias"], true),
            Charset      = (byte)j["charset"],
            TexPagId     = (uint)j["texture"],
            Scale        = DeserializePointF(j["scale"]),
            Characters   = DeserializeArray(j["chars"], (Func<dynamic, FontCharacter>)(d => DeserializeFontChar(d)))
        };
        #endregion

        public static ObjectInfo DeserializeObj(JsonData j, SpriteInfo[] sprites, Func<string, uint> objNameToId)
        {
            var spr = String.IsNullOrEmpty((string)j["sprite"]) ? -1 : Array.FindIndex(sprites, s => s.Name == (string)j["sprite"]);

            return new ObjectInfo
            {
                SpriteIndex  = spr < 0 ? UInt32.MaxValue : (uint)spr,
                IsVisible    = (bool)j["visible"],
                IsSolid      = (bool)j["solid"],
                Depth        = (int)j["depth"],
                IsPersistent = (bool)j["persist"],

                ParentId  = j.Has("parent") ? (uint?)objNameToId((string)j["parent"]) : null,
                TexMaskId = j.Has("texmask") ? (uint?)j["texmask"] : null,

                Physics = j.Has("physics") ? (ObjectPhysics?)DeserializeObjPhysics(j["physics"]) : null,

                IsSensor       = (bool)j["sensor"],
                CollisionShape = (CollisionShape)Enum.Parse(typeof(CollisionShape), (string)j["colshape"], true),

                OtherFloats = DeserializeArray(j["data"], d => (float)d),
                ShapePoints = DeserializeArray(j["points"],
                    (Func<dynamic, int[][]>)(d => DeserializeArray(d,
                        (Func<dynamic, int[]>)(e => DeserializeArray(e,
                            (Func<dynamic, int>)(f => (int)f))))))
            };
        }
        #region public static RoomInfo        DeserializeRoom(JsonData j, BackgroundInfo[] bgs, ObjectInfo[] objs)
        public static RoomInfo DeserializeRoom(JsonData j, BackgroundInfo[] bgs, ObjectInfo[] objs) => new RoomInfo
        {
            Caption              = (string)j["caption"],
            Speed                = (uint)j["speed"],
            IsPersistent         = (bool)j["persist"],
            EnableViews          = (bool)j["enableviews"],
            ShowColour           = (bool)j["showcolour"],
            ClearDisplayBuffer   = (bool)j["clearbuf"],
            UnknownFlag          = (bool)j["flag"],
            World                = (uint)j["world"],
            MetresPerPixel       = (float)j["metresperpx"],
            DrawBackgroundColour = (bool)j["drawbgcol"],
            _unknown             = (uint)j["unknown"],

            Size     = DeserializeSize(j["size"]),
            Colour   = ParseColour(j["colour"]),
            Bounding = DeserializeBBox(j["bounding"]),
            Gravity  = DeserializePointF(j["gravity"]),

            Backgrounds = DeserializeArray(j["bgs"], (Func<dynamic, RoomBackground>)(d => DeserializeRoomBg(d, bgs))),
            Views       = DeserializeArray(j["views"], (Func<dynamic, RoomView>)(d => DeserializeRoomView(d, objs))),
            Objects     = DeserializeArray(j["objs"], (Func<dynamic, RoomObject>)(d => DeserializeRoomObj(d, objs))),
            Tiles       = DeserializeArray(j["tiles"], (Func<dynamic, RoomTile>)(d => DeserializeRoomTile(d, bgs))),
            ObjInst     = j.Has("objinst") ? DeserializeArray(j["objinst"], DeserializeRoomObjInst) : null
        };
        #endregion
        #region public static TexturePageInfo DeserializeTPag(JsonData j)
        public static TexturePageInfo DeserializeTPag(JsonData j) => new TexturePageInfo
        {
            Source        = DeserializeRect16 (j["src"] ),
            Destination   = DeserializeRect16 (j["dest"]),
            Size          = DeserializeSize16 (j["size"]),
            SpritesheetId = (uint)j["sheetid"]
        };
        #endregion

        public static ShaderProgramSource DeserializeShaderProgramSource(JsonData j) => new ShaderProgramSource
        {
            VertexShader = (string)j["vertex"],
            FragmentShader = (string)j["fragment"]
        };

        public static ShaderCode DeserializeShaderCode(JsonData j) => new ShaderCode
        {
            GLSL_ES = DeserializeShaderProgramSource(j["glsles"]),
            GLSL    = DeserializeShaderProgramSource(j["glsl"  ]),
            HLSL9   = DeserializeShaderProgramSource(j["hlsl9" ])
                // TODO: HLSL11, PSSL, Cg, Cg_PS3
        };

        #region public static ShaderInfo DeserializeShader(JsonData j)
        public static ShaderInfo DeserializeShader(JsonData j) => new ShaderInfo
        {
            Type       = (ShaderType)Enum.Parse(typeof(ShaderType), (string)j["type"]),
            Code       = DeserializeShaderCode(j["code"]),
            Attributes = DeserializeArray(j["attributes"], d => (string)d)
        };
        #endregion

        public static FunctionLocalsInfo DeserializeFuncLocals(JsonData j) => new FunctionLocalsInfo
        {
            FunctionName = (string)j["name"],
            LocalNames   = DeserializeArray(j["locals"], jd => jd.ToString())
        };

        private static ReferenceDef DeserializeReferenceDef(JsonData j) =>
            j.IsString ? new ReferenceDef { Name = j.ToString(), FirstOffset = 0xFFFFFFFF } : new ReferenceDef
        {
            Name         = (string)j["name"],
            Occurrences  = j.Has("occurrences") ? (uint)j["occurrences"] : 0,
            FirstOffset  = j.Has("firstoffset") ? (uint)j["firstoffset"] : 0xFFFFFFFF,
            HasExtra     = j.Has("instancetype") || j.Has("unknown"),
            InstanceType = j.Has("instancetype") ? (InstanceType)(int)j["instancetype"] : InstanceType.StackTopOrGlobal,
            unknown2     = j.Has("unknown") ? (int)j["unknown"] : 0
        };

        // strings, vars and funcs are compiled using the other things

        private static JsonData LoadJson(string baseDir, string filename) => JsonMapper.ToObject(File.OpenText(Path.Combine(baseDir, filename)));

        public class StringsListBuilder
        {
            IDictionary<string, int> stringIndices;
            IList<string> stringList;

            public StringsListBuilder()
            {
                stringIndices = new Dictionary<string, int>();
                stringList = new List<string>();
            }

            public virtual int AddString(String s)
            {
                stringIndices[s] = stringList.Count;
                stringList.Add(s);
                return 0;
            }

            public void AddStrings(String[] strings)
            {
                foreach (var s in strings)
                {
                    AddString(s);
                }
            }

            public uint GetIndex(String s)
            {
                if (stringIndices.TryGetValue(s, out int idx))
                {
                    return (uint)idx;
                }
                else
                {
                    AddString(s);
                    return (uint)(stringList.Count - 1);
                }
            }

            public string[] GetStrings()
            {
                return stringList.ToArray();
            }
        }

        public static GMFile /* errors: different return type? */ ReadFile(string baseDir, JsonData projFile)
        {
            var f = new GMFile();
            // OBJT: depends on SPRT, obj<->id map
            // ROOM: depends on OBJT, BGND
            // SCPT: depends on CODE

            if (projFile.Has("chunkorder") && projFile["chunkorder"].IsArray)
            {
                f.ChunkOrder = DeserializeArray(projFile["chunkorder"], jd => SectionHeadersExtensions.FromChunkName((string)jd));
            }
            else
            {
                Console.Error.WriteLine("Warning: Project file doesn't have a chunk order. You should export with a newer Altar.NET version.");
                f.ChunkOrder = new SectionHeaders[] {
                    SectionHeaders.General     ,
                    SectionHeaders.Options     ,
                    SectionHeaders.Language    ,
                    SectionHeaders.Extensions  ,
                    SectionHeaders.Sounds      ,
                    SectionHeaders.AudioGroup  ,
                    SectionHeaders.Sprites     ,
                    SectionHeaders.Backgrounds ,
                    SectionHeaders.Paths       ,
                    SectionHeaders.Scripts     ,
                    SectionHeaders.Globals     ,
                    SectionHeaders.Shaders     ,
                    SectionHeaders.Fonts       ,
                    SectionHeaders.Timelines   ,
                    SectionHeaders.Objects     ,
                    SectionHeaders.Rooms       ,
                    SectionHeaders.DataFiles   ,
                    SectionHeaders.TexturePage ,
                    SectionHeaders.Code        ,
                    SectionHeaders.Variables   ,
                    SectionHeaders.Functions   ,
                    SectionHeaders.Strings     ,
                    SectionHeaders.Textures    ,
                    SectionHeaders.Audio       ,
                    SectionHeaders.EmbedImage  ,
                };
            }

            if (projFile.Has("general"))
            {
                Console.WriteLine("Loading general...");
                try
                {
                    f.General = DeserializeGeneral(LoadJson(baseDir, (string)(projFile["general"])));
                }
                catch (Exception)
                {
                    Console.Error.WriteLine("Error loading general");
                    throw;
                }
            }
            if (projFile.Has("options"))
            {
                Console.WriteLine("Loading options...");
                try
                {
                    f.Options = DeserializeOptions(LoadJson(baseDir, (string)(projFile["options"])));
                }
                catch (Exception)
                {
                    Console.Error.WriteLine("Error loading options");
                    throw;
                }
            }
            if (projFile.Has("strings"))
            {
                Console.WriteLine("Loading strings...");
                try
                {
                    f.Strings = DeserializeArray(LoadJson(baseDir, (string)(projFile["strings"])), jd => (string)jd);
                }
                catch (Exception)
                {
                    Console.Error.WriteLine("Error loading strings");
                    throw;
                }
            }

            var variables = new ReferenceDef[0];
            var functions = new ReferenceDef[0];
            if (projFile.Has("variables"))
            {
                Console.WriteLine("Loading variables...");
                try
                {
                    var vardata = LoadJson(baseDir, (string)(projFile["variables"]));
                    variables = DeserializeArray(vardata.IsArray ? vardata : vardata["variables"], DeserializeReferenceDef);
                    if (vardata.Has("extra"))
                    {
                        f.VariableExtra = DeserializeArray(vardata["extra"], jd => (uint)jd);
                    }
                }
                catch (Exception)
                {
                    Console.Error.WriteLine("Error loading variables");
                    throw;
                }
            }
            if (projFile.Has("functions"))
            {
                Console.WriteLine("Loading functions...");
                try
                {
                    var funcdata = LoadJson(baseDir, (string)(projFile["functions"]));
                    functions = DeserializeArray(funcdata.IsArray ? funcdata : funcdata["functions"], DeserializeReferenceDef);
                    if (funcdata.Has("locals"))
                    {
                        f.FunctionLocals = DeserializeArray(funcdata["locals"], DeserializeFuncLocals);
                    }
                }
                catch (Exception)
                {
                    Console.Error.WriteLine("Error loading functions");
                    throw;
                }
            }
            f.RefData = new RefData { Variables = variables, Functions = functions };

            if (projFile.Has("textures"))
            {
                Console.WriteLine("Loading textures...");
                var textures = projFile["textures"].ToArray();
                var ts = new TextureInfo[textures.Length];
                for (int i = 0; i < textures.Length; i++)
                {
                    try
                    {
                        var texinfo = new TextureInfo
                        {
                            PngData = File.ReadAllBytes(Path.Combine(baseDir, (string)(textures[i])))
                        };

                        var bp = new UniquePtr(texinfo.PngData);

                        unsafe
                        {
                            var png = (PngHeader*)bp.BPtr;

                            texinfo.Width = Utils.SwapEnd32(png->IHDR.Width);
                            texinfo.Height = Utils.SwapEnd32(png->IHDR.Height);
                        }

                        ts[i] = texinfo;
                    }
                    catch (Exception)
                    {
                        Console.Error.WriteLine($"Error loading {textures[i]}");
                        throw;
                    }
                }
                f.Textures = ts;
            }
            if (projFile.Has("tpags"))
            {
                Console.Write("Loading texture pages... ");
                var cl = Console.CursorLeft;
                var ct = Console.CursorTop;
                
                var tpags = projFile["tpags"].ToArray();
                var tps = new TexturePageInfo[tpags.Length];
                for (int i = 0; i < tpags.Length; i++)
                {
                    Console.SetCursorPosition(cl, ct);
                    Console.WriteLine(O_PAREN + (i + 1) + SLASH + tpags.Length + C_PAREN);
                    try
                    {
                        tps[i] = DeserializeTPag(LoadJson(baseDir, (string)(tpags[i])));
                    }
                    catch (Exception)
                    {
                        Console.Error.WriteLine($"Error loading {tpags[i]}");
                        throw;
                    }
                }
                f.TexturePages = tps;
            }
            if (projFile.Has("audio"))
            {
                Console.WriteLine("Loading audio...");
                var audio = projFile["audio"].ToArray();
                var ais = new AudioInfo[audio.Length];
                for (int i = 0; i < audio.Length; i++)
                {
                    try
                    {
                        var audioinfo = new AudioInfo
                        {
                            Wave = File.ReadAllBytes(Path.Combine(baseDir, (string)(audio[i])))
                        };
                        ais[i] = audioinfo;
                    }
                    catch (Exception)
                    {
                        Console.Error.WriteLine($"Error loading {audio[i]}");
                        throw;
                    }
                }
                f.Audio = ais;
            }
            if (projFile.Has("sprites"))
            {
                Console.Write("Loading sprites... ");
                var cl = Console.CursorLeft;
                var ct = Console.CursorTop;

                var sprites = projFile["sprites"].ToArray();
                var ss = new SpriteInfo[sprites.Length];
                for (int i = 0; i < sprites.Length; i++)
                {
                    Console.SetCursorPosition(cl, ct);
                    Console.WriteLine(O_PAREN + (i + 1) + SLASH + sprites.Length + C_PAREN);
                    try
                    {
                        ss[i] = DeserializeSprite(LoadJson(baseDir, (string)(sprites[i])));
                        ss[i].Name = Path.GetFileNameWithoutExtension((string)(sprites[i]));
                    }
                    catch (Exception)
                    {
                        Console.Error.WriteLine($"Error loading {sprites[i]}");
                        throw;
                    }
                }
                f.Sprites = ss;
            }
            if (projFile.Has("objs"))
            {
                Console.Write("Loading objects... ");
                var cl = Console.CursorLeft;
                var ct = Console.CursorTop;

                var objs = projFile["objs"].ToArray();
                var objNames = objs.Select(o => Path.GetFileNameWithoutExtension((string)o)).ToArray();
                var os = new ObjectInfo[objs.Length];
                for (int i = 0; i < objs.Length; i++)
                {
                    Console.SetCursorPosition(cl, ct);
                    Console.WriteLine(O_PAREN + (i + 1) + SLASH + objs.Length + C_PAREN);
                    try
                    {
                        os[i] = DeserializeObj(
                                        LoadJson(baseDir, (string)(objs[i])),
                                        f.Sprites,
                                        s => (uint)Array.IndexOf(objNames, s));
                        os[i].Name = objNames[i];
                    }
                    catch (Exception)
                    {
                        Console.Error.WriteLine($"Error loading {objs[i]}");
                        throw;
                    }
                }
                f.Objects = os;
            }
            if (projFile.Has("code"))
            {
                Console.WriteLine("Loading code...");
                var code = projFile["code"].ToArray();
                var cs = new CodeInfo[code.Length];

                var strings = new StringsListBuilder();
                strings.AddStrings(f.Strings);
                IDictionary<string, uint> objectIndices = new Dictionary<string, uint>(f.Objects.Length);
                for (uint i = 0; i < f.Objects.Length; i++) objectIndices[f.Objects[i].Name] = i;

                for (int i = 0; i < code.Length; i++)
                {
                    Console.WriteLine((string)(code[i]));
                    try
                    {
                        cs[i] = Assembler.DeserializeCodeFromFile(Path.Combine(baseDir, (string)(code[i])), f.General.BytecodeVersion,
                                        strings, objectIndices);
                        cs[i].Name = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension((string)(code[i])));
                        cs[i].ArgumentCount = 1;
                        if (f.FunctionLocals != null)
                        {
                            for (int j = 0; j < f.FunctionLocals.Length; j++)
                            {
                                int fastIndex = (j + i) % f.FunctionLocals.Length;
                                if (f.FunctionLocals[fastIndex].FunctionName == cs[i].Name)
                                {
                                    cs[i].ArgumentCount = f.FunctionLocals[fastIndex].LocalNames.Length;
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        Console.Error.WriteLine($"Error loading {code[i]}");
                        throw;
                    }
                }
                f.Code = cs;

                f.Strings = strings.GetStrings();
            }
            if (projFile.Has("sounds"))
            {
                Console.WriteLine("Loading sounds...");
                var sounds = projFile["sounds"].ToArray();
                var ss = new SoundInfo[sounds.Length];
                for (int i = 0; i < sounds.Length; i++)
                {
                    try
                    {
                        ss[i] = DeserializeSound(LoadJson(baseDir, (string)(sounds[i])));
                        ss[i].Name = Path.GetFileNameWithoutExtension((string)(sounds[i]));
                    }
                    catch (Exception)
                    {
                        Console.Error.WriteLine($"Error loading {sounds[i]}");
                        throw;
                    }
                }
                f.Sound = ss;
            }
            if (projFile.Has("bg"))
            {
                Console.WriteLine("Loading backgrounds...");
                var bg = projFile["bg"].ToArray();
                var bs = new BackgroundInfo[bg.Length];
                for (int i = 0; i < bg.Length; i++)
                {
                    try
                    {
                        bs[i] = DeserializeBg(LoadJson(baseDir, (string)(bg[i])));
                        bs[i].Name = Path.GetFileNameWithoutExtension((string)(bg[i]));
                    }
                    catch (Exception)
                    {
                        Console.Error.WriteLine($"Error loading {bg[i]}");
                        throw;
                    }
                }
                f.Backgrounds = bs;
            }
            if (projFile.Has("paths"))
            {
                Console.WriteLine("Loading paths...");
                var paths = projFile["paths"].ToArray();
                var ps = new PathInfo[paths.Length];
                for (int i = 0; i < paths.Length; i++)
                {
                    try
                    {
                        ps[i] = DeserializePath(LoadJson(baseDir, (string)(paths[i])));
                        ps[i].Name = Path.GetFileNameWithoutExtension((string)(paths[i]));
                    }
                    catch (Exception)
                    {
                        Console.Error.WriteLine($"Error loading {paths[i]}");
                        throw;
                    }
                }
                f.Paths = ps;
            }
            if (projFile.Has("scripts"))
            {
                Console.WriteLine("Loading scripts...");
                var scripts = projFile["scripts"].ToArray();
                var ss = new ScriptInfo[scripts.Length];
                for (int i = 0; i < scripts.Length; i++)
                {
                    try
                    {
                        ss[i] = DeserializeScript(LoadJson(baseDir, (string)(scripts[i])), f.Code);
                        ss[i].Name = Path.GetFileNameWithoutExtension((string)(scripts[i]));
                    }
                    catch (Exception)
                    {
                        Console.Error.WriteLine($"Error loading {scripts[i]}");
                        throw;
                    }
                }
                f.Scripts = ss;
            }
            if (projFile.Has("fonts"))
            {
                Console.WriteLine("Loading fonts...");
                var fonts = projFile["fonts"].ToArray();
                var fs = new FontInfo[fonts.Length];
                for (int i = 0; i < fonts.Length; i++)
                {
                    try
                    {
                        fs[i] = DeserializeFont(LoadJson(baseDir, (string)(fonts[i])));
                        fs[i].CodeName = Path.GetFileNameWithoutExtension((string)(fonts[i]));
                    }
                    catch (Exception)
                    {
                        Console.Error.WriteLine($"Error loading {fonts[i]}");
                        throw;
                    }
                }
                f.Fonts = fs;
            }
            if (projFile.Has("rooms"))
            {
                Console.Write("Loading rooms... ");
                var cl = Console.CursorLeft;
                var ct = Console.CursorTop;

                var rooms = projFile["rooms"].ToArray();
                var rs = new RoomInfo[rooms.Length];
                for (int i = 0; i < rooms.Length; i++)
                {
                    Console.SetCursorPosition(cl, ct);
                    Console.WriteLine(O_PAREN + (i + 1) + SLASH + rooms.Length + C_PAREN);
                    try
                    {
                        rs[i] = DeserializeRoom(LoadJson(baseDir, (string)(rooms[i])), f.Backgrounds, f.Objects);
                        rs[i].Name = Path.GetFileNameWithoutExtension((string)(rooms[i]));
                    }
                    catch (Exception)
                    {
                        Console.Error.WriteLine($"Error loading {rooms[i]}");
                        throw;
                    }
                }
                f.Rooms = rs;
            }
            if (projFile.Has("audiogroups"))
            {
                Console.WriteLine("Loading audio groups...");
                try
                {
                    f.AudioGroups = DeserializeArray(LoadJson(baseDir, (string)(projFile["audiogroups"])), jd => (string)jd);
                }
                catch (Exception)
                {
                    Console.Error.WriteLine("Error loading audio groups");
                    throw;
                }
            }
            if (projFile.Has("shaders"))
            {
                Console.WriteLine("Loading shaders...");
                var shaders = projFile["shaders"].ToArray();
                var ss = new ShaderInfo[shaders.Length];
                for (int i = 0; i < shaders.Length; i++)
                {
                    try
                    {
                        ss[i] = DeserializeShader(LoadJson(baseDir, (string)(shaders[i])));
                        ss[i].Name = Path.GetFileNameWithoutExtension((string)(shaders[i]));
                    }
                    catch (Exception)
                    {
                        Console.Error.WriteLine($"Error loading {shaders[i]}");
                        throw;
                    }
                }
                f.Shaders = ss;
            }
            return f;
        }
    }
}
