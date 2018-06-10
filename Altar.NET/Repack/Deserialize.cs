using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Altar.Decomp;
using Altar.Recomp;
using LitJson;
using static Altar.SR;

namespace Altar.Repack
{
    using AsmParser = Altar.Recomp.Parser;

    public static class Deserialize
    {
        static T[] DeserializeArray<T>(JsonData jArr, Func<JsonData, T> converter)
        {
            if (jArr == null)
                throw new ArgumentNullException(nameof(jArr));
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));

            var j = ((JsonData)jArr);

            var r = new T[j.Count];

            for (int i = 0; i < j.Count; i++)
                r[i] = converter(jArr[i]);

            return r;
        }
        /*static T[] DeserializeArray<T>(dynamic  jArr, Func<dynamic , T> converter)
        {
            if (jArr      == null)
                throw new ArgumentNullException(nameof(jArr     ));
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));

            var j = ((JsonData)jArr);

            var r = new T[j.Count];

            for (int i = 0; i < j.Count; i++)
                r[i] = converter(jArr[i]);

            return r;
        }*/

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

        static Point DeserializePoint(JsonData j) => new Point((int)j["x"], (int)j["y"]);
        static PointF DeserializePointF(JsonData j) => new PointF((float)j["x"], (float)j["y"]);
        static Point16 DeserializePoint16(JsonData j) => new Point16((ushort)j["x"], (ushort)j["y"]);
        static Point DeserializeSize(JsonData j) => new Point((int)j["width"], (int)j["height"]);
        static PointF DeserializeSizeF(JsonData j) => new PointF((float)j["width"], (float)j["height"]);
        static Point16 DeserializeSize16(JsonData j) => new Point16((ushort)j["width"], (ushort)j["height"]);
        static Rectangle DeserializeRect(JsonData j) => new Rectangle((int)j["x"], (int)j["y"], (int)j["width"], (int)j["height"]);
        static Rectangle16 DeserializeRect16(JsonData j) => new Rectangle16((ushort)j["x"], (ushort)j["y"], (ushort)j["width"], (ushort)j["height"]);
        static BoundingBox DeserializeBBox(JsonData j) => new BoundingBox((uint)j["top"], (uint)j["left"], (uint)j["right"], (uint)j["bottom"]);
        static BoundingBox2 DeserializeBBox2(JsonData j) => new BoundingBox2((uint)j["left"], (uint)j["right"], (uint)j["bottom"], (uint)j["top"]);
        static PathPoint DeserializePathPt(JsonData j) => new PathPoint { Position = DeserializePointF(j["pos"]), Speed = (float)j["speed"] };

        #region static FontCharacter DeserializeFontChar  (JsonData j)
        static FontCharacter DeserializeFontChar(JsonData j) => new FontCharacter
        {
            Character = (char)j["char"],
            TPagFrame = DeserializeRect16(j["frame"]),
            Shift = (ushort)j["shift"],
            Offset = (uint)j["offset"]
        };
        #endregion
        #region static ObjectPhysics DeserializeObjPhysics(JsonData j)
        static ObjectPhysics DeserializeObjPhysics(JsonData j) => new ObjectPhysics
        {
            Density = (float)j["density"],
            Restitution = (float)j["restitution"],
            Group = (float)j["group"],
            LinearDamping = (float)j["lineardamp"],
            AngularDamping = (float)j["angulardamp"],
            Friction = (float)j["friction"],
            Kinematic = (float)j["kinematic"],
            Unknown0 = (float)j["unk0"],
            Unknown1 = (int)j["unk1"]
        };
        #endregion

        static RoomBackground DeserializeRoomBg(JsonData j, BackgroundInfo[] bgs)
        {
            var r = new RoomBackground
            {
                IsEnabled = (bool)j["enabled"],
                IsForeground = (bool)j["foreground"],
                TileX = (bool)j["tilex"],
                TileY = (bool)j["tiley"],
                StretchSprite = (bool)j["stretch"],

                Position = DeserializePoint(j["pos"]),
                Speed = DeserializePoint(j["speed"])
            };

            if (((JsonData)j).Has("bg"))
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
                Index = (uint)j["index"],
                Instances = DeserializeArray(j["instances"], i => (uint)i),
                Name = (string)j["name"],
                Unk1 = (uint)j["unk1"],
                Unk2 = (uint)j["unk2"],
                Unk3 = (uint)j["unk3"]
            };
        }

        static RoomView DeserializeRoomView(JsonData j, ObjectInfo[] objs)
        {
            var r = new RoomView
            {
                IsEnabled = (bool)j["enabled"],
                View = DeserializeRect(j["view"]),
                Port = DeserializeRect(j["port"]),
                Border = DeserializePoint(j["border"]),
                Speed = DeserializePoint(j["speed"])
            };

            if (((JsonData)j).Has("obj"))
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
                Position = DeserializePoint(j["pos"]),
                Scale = DeserializePointF(j["scale"]),
                Colour = ParseColour((JsonData)j["colour"]),
                Rotation = (float)j["rotation"],
                InstanceID = (uint)j["instanceid"],
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
                Position = DeserializePoint(j["pos"]),
                SourcePosition = DeserializePoint(j["sourcepos"]),
                Size = DeserializeSize(j["size"]),
                Scale = DeserializePointF(j["scale"]),
                Colour = ParseColour((JsonData)j["colour"]),
                Depth = (uint)j["tiledepth"],
                InstanceID = (uint)j["instanceid"]
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
            IsDebug = (bool)j["debug"],
            BytecodeVersion = (uint)j["bytecodeversion"],
            FileName = (string)j["filename"],
            Configuration = (string)j["config"],
            GameID = (uint)j["gameid"],
            Name = (string)j["name"],
            Version = new Version((string)j["version"]),
            WindowSize = DeserializeSize(j["windowsize"]),
            LicenseMD5Hash = DeserializeArray(j["licensemd5"], (Func<dynamic, byte>)(jd => (byte)jd)),
            LicenceCRC32 = (uint)j["licensecrc32"],
            DisplayName = (string)j["displayname"],
            Timestamp = DateTime.Parse((string)j["timestamp"], CultureInfo.InvariantCulture),

            ActiveTargets = (GameTargets)Enum.Parse(typeof(GameTargets), (string)j["targets"], true),
            unknown = DeserializeArray(j["unknown"], (Func<dynamic, uint>)(jd => (uint)jd)),
            SteamAppID = (uint)j["appid"],
            InfoFlags = (InfoFlags)Enum.Parse(typeof(InfoFlags), (string)j["flags"], true),

            WeirdNumbers = DeserializeArray(j["numbers"], (Func<dynamic, uint>)(jd => (uint)jd))
        };
        #endregion
        #region public static OptionInfo  DeserializeOptions(JsonData j)
        public static OptionInfo DeserializeOptions(JsonData j) => new OptionInfo
        {
            Constants = ((JsonData)j["constants"]).ToDictionary()
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
            IsEmbedded = (bool)j["embedded"],
            IsCompressed = (bool)j["compressed"],
            Type = (string)j["type"],
            File = (string)j["file"],
            VolumeMod = (float)j["volume"],
            PitchMod = (float)j["pitch"],
            Group = (string)j["group"],
            AudioID = (int)j["audioid"]
        };
        #endregion
        #region public static SpriteInfo     DeserializeSprite(JsonData j)
        public static SpriteInfo DeserializeSprite(JsonData j) => new SpriteInfo
        {
            BBoxMode = (uint)j["bboxmode"],
            SeparateColMasks = (bool)j["sepmasks"],
            Size = DeserializeSize(j["size"]),
            Bounding = DeserializeBBox2(j["bounding"]),
            Origin = DeserializePoint(j["origin"]),
            TextureIndices = j.Has("textures") ? DeserializeArray(j["textures"], (Func<dynamic, uint>)(jd => (uint)jd)) : null,
            CollisionMasks = j.Has("colmasks") ? DeserializeArray(j["colmasks"], (Func<JsonData, bool[,]>)DeserializeColMask) : null,
            Version = j.Has("unknown1") ? 2 : 1,
            UnknownFloat = j.Has("unknown1") ? (float)j["unknown1"] : 0.0f
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
            IsSmooth = (bool)j["smooth"],
            IsClosed = (bool)j["closed"],
            Precision = (uint)j["precision"],
            Points = DeserializeArray(j["points"], (Func<dynamic, PathPoint>)(d => DeserializePathPt(d)))
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
            SystemName = (string)j["sysname"],
            EmSize = (uint)j["emsize"],
            IsBold = (bool)j["bold"],
            IsItalic = (bool)j["italic"],
            AntiAliasing = (FontAntiAliasing)Enum.Parse(typeof(FontAntiAliasing), (string)j["antialias"], true),
            Charset = (byte)j["charset"],
            TexPagId = (uint)j["texture"],
            Scale = DeserializePointF(j["scale"]),
            Characters = DeserializeArray(j["chars"], (Func<dynamic, FontCharacter>)(d => DeserializeFontChar(d)))
        };
        #endregion

        public static ObjectInfo DeserializeObj(JsonData j, SpriteInfo[] sprites, Func<string, uint> objNameToId)
        {
            var spr = String.IsNullOrEmpty((string)j["sprite"]) ? -1 : Array.FindIndex(sprites, s => s.Name == (string)j["sprite"]);

            return new ObjectInfo
            {
                SpriteIndex = spr < 0 ? UInt32.MaxValue : (uint)spr,
                IsVisible = (bool)j["visible"],
                IsSolid = (bool)j["solid"],
                Depth = (int)j["depth"],
                IsPersistent = (bool)j["persist"],

                ParentId = ((JsonData)j).Has("parent") ? (uint?)objNameToId((string)j["parent"]) : null,
                TexMaskId = ((JsonData)j).Has("texmask") ? (uint?)j["texmask"] : null,

                Physics = ((JsonData)j).Has("physics") ? (ObjectPhysics?)DeserializeObjPhysics(j["physics"]) : null,

                IsSensor = (bool)j["sensor"],
                CollisionShape = (CollisionShape)Enum.Parse(typeof(CollisionShape), (string)j["colshape"], true),

                OtherFloats = DeserializeArray(j["data"], (Func<dynamic, float>)(d => (float)d)),
                ShapePoints = DeserializeArray(j["points"],
                    (Func<dynamic, int[][]>)(d => DeserializeArray(d,
                        (Func<dynamic, int[]>)(e => DeserializeArray(e,
                            (Func<dynamic, int>)(f => (int)f))))))
            };
        }
        #region public static RoomInfo        DeserializeRoom(JsonData j, BackgroundInfo[] bgs, ObjectInfo[] objs)
        public static RoomInfo DeserializeRoom(JsonData j, BackgroundInfo[] bgs, ObjectInfo[] objs) => new RoomInfo
        {
            Caption = (string)j["caption"],
            Speed = (uint)j["speed"],
            IsPersistent = (bool)j["persist"],
            EnableViews = (bool)j["enableviews"],
            ShowColour = (bool)j["showcolour"],
            ClearDisplayBuffer = (bool)j["clearbuf"],
            UnknownFlag = (bool)j["flag"],
            World = (uint)j["world"],
            MetresPerPixel = (float)j["metresperpx"],
            DrawBackgroundColour = (bool)j["drawbgcol"],
            _unknown = (uint)j["unknown"],

            Size = DeserializeSize(j["size"]),
            Colour = ParseColour(j["colour"]),
            Bounding = DeserializeBBox(j["bounding"]),
            Gravity = DeserializePointF(j["gravity"]),

            Backgrounds = DeserializeArray(j["bgs"], (Func<dynamic, RoomBackground>)(d => DeserializeRoomBg(d, bgs))),
            Views = DeserializeArray(j["views"], (Func<dynamic, RoomView>)(d => DeserializeRoomView(d, objs))),
            Objects = DeserializeArray(j["objs"], (Func<dynamic, RoomObject>)(d => DeserializeRoomObj(d, objs))),
            Tiles = DeserializeArray(j["tiles"], (Func<dynamic, RoomTile>)(d => DeserializeRoomTile(d, bgs))),
            ObjInst = j.Has("objinst") ? DeserializeArray(j["objinst"], DeserializeRoomObjInst) : null
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

        public static FunctionLocalsInfo DeserializeFuncLocals(JsonData j) => new FunctionLocalsInfo
        {
            FunctionName = (string)j["name"],
            LocalNames = DeserializeArray(j["locals"], jd => jd.ToString())
        };

        private static ReferenceDef DeserializeReferenceDef(JsonData j) =>
            j.IsString ? new ReferenceDef { Name = j.ToString(), FirstOffset = 0xFFFFFFFF } : new ReferenceDef
        {
            Name = (string)j["name"],
            Occurrences = j.Has("occurrences") ? (uint)j["occurrences"] : 0,
            FirstOffset = j.Has("firstoffset") ? (uint)j["firstoffset"] : 0xFFFFFFFF,
            HasExtra = j.Has("instancetype") || j.Has("unknown"),
            InstanceType = j.Has("instancetype") ? (InstanceType)(int)j["instancetype"] : InstanceType.StackTopOrGlobal,
            unknown2 = j.Has("unknown") ? (int)j["unknown"] : 0
        };

        private static CodeInfo DeserializeCodeFromFile(string filename, uint bcv,
            IDictionary<string, uint> stringIndices, IDictionary<string, uint> objectIndices)
        {
            IEnumerable<Instruction> instructions;
            if (filename.ToLowerInvariant().EndsWith(SR.EXT_GML_ASM))
            {
                var t = Tokenizer.Tokenize(File.ReadAllText(filename));
                instructions = AsmParser.Parse(t);
            }
            else if (filename.ToLowerInvariant().EndsWith(SR.EXT_GML_LSP))
            {
                // TODO
                throw new NotImplementedException();
            }
            else
            {
                throw new InvalidDataException("Unknown code format for '" + filename + "'");
            }
            return DeserializeAssembly(Path.GetFileNameWithoutExtension(filename), instructions, bcv,
                stringIndices, objectIndices);
        }

        private static InstanceType GetInstanceType(InstanceType instanceType, string objName, IDictionary<string, uint> objectIndices)
        {
            if (instanceType <= 0)
            {
                return instanceType;
            }
            else if (objName != null)
            {
                return (InstanceType)objectIndices[objName];
            }
            else
            {
                throw new InvalidDataException("Bad instance tuple");
            }
        }

        private static CodeInfo DeserializeAssembly(string name, IEnumerable<Instruction> instructions, uint bcv,
            IDictionary<string, uint> stringIndices, IDictionary<string, uint> objectIndices)
        {
            IList<Tuple<ReferenceSignature, uint>> functionReferences = new List<Tuple<ReferenceSignature, uint>>();
            IList<Tuple<ReferenceSignature, uint>> variableReferences = new List<Tuple<ReferenceSignature, uint>>();

            var binaryInstructions = new List<AnyInstruction>();
            uint size = 0;
            foreach (var inst in instructions)
            {
                if (inst is Label)
                {
                    continue;
                }
                var op = new OpCodes { VersionE = inst.OpCode.VersionE, VersionF = inst.OpCode.VersionF };
                var type = DisasmExt.Kind(op, bcv);
                AnyInstruction bininst = new AnyInstruction();
                switch (type)
                {
                    case InstructionKind.Set:
                        var setinst = (Set)inst;
                        bininst.Set = new SetInstruction
                        {
                            DestVar = new Reference(setinst.VariableType, 0),
                            Instance = GetInstanceType(setinst.InstanceType, setinst.InstanceName, objectIndices),
                            OpCode = op,
                            Types = new TypePair(setinst.Type1, setinst.Type2)
                        };
                        variableReferences.Add(new Tuple<ReferenceSignature, uint>(new ReferenceSignature
                        {
                            Name = setinst.TargetVariable,
                            InstanceType = setinst.InstanceType,
                            Instance = setinst.InstanceType == InstanceType.Local ? name : null,
                            VariableType = setinst.VariableType
                        }, size));
                        break;
                    case InstructionKind.Push:
                        var bp = new PushInstruction
                        {
                            OpCode = op,
                            Type = ((Push)inst).Type
                        };
                        if (bp.Type == DataType.Variable)
                        {
                            var p = (PushVariable)inst;
                            bp.Value = (short)GetInstanceType(p.InstanceType, p.InstanceName, objectIndices);
                            bp.ValueRest = new Reference(p.VariableType, 0).val;
                            variableReferences.Add(new Tuple<ReferenceSignature, uint>(new ReferenceSignature
                            {
                                Name = p.VariableName,
                                InstanceType = p.InstanceType,
                                Instance = p.InstanceType == InstanceType.Local ? name : null,
                                VariableType = p.VariableType
                            }, size));
                        }
                        else
                        {
                            var p = (PushConst)inst;
                            switch (p.Type)
                            {
                                case DataType.Int16:
                                    bp.Value = (short)(long)p.Value;
                                    break;
                                case DataType.Boolean:
                                    bp.ValueRest = (uint)(long)p.Value;
                                    break;
                                case DataType.Double:
                                case DataType.Single:
                                    bp.ValueRest = BitConverter.ToUInt64(BitConverter.GetBytes(Convert.ToDouble(p.Value)), 0);
                                    break;
                                case DataType.Int32:
                                case DataType.Int64:
                                    bp.ValueRest = BitConverter.ToUInt64(BitConverter.GetBytes(unchecked((long)(p.Value))), 0);
                                    break;
                                case DataType.String:
                                    bp.ValueRest = stringIndices[(string)p.Value];
                                    break;
                            }
                        }
                        bininst.Push = bp;
                        break;
                    case InstructionKind.Call:
                        var callinst = (Call)inst;
                        bininst.Call = new CallInstruction
                        {
                            Arguments = (ushort)callinst.Arguments,
                            Function = new Reference(callinst.FunctionType, 0),
                            OpCode = op,
                            ReturnType = callinst.ReturnType
                        };
                        functionReferences.Add(new Tuple<ReferenceSignature, uint>(new ReferenceSignature
                        {
                            Name = callinst.FunctionName,
                            InstanceType = InstanceType.StackTopOrGlobal,
                            VariableType = callinst.FunctionType
                        }, size));
                        break;
                    case InstructionKind.Break:
                        var breakinst = (Break)inst;
                        bininst.Break = new BreakInstruction
                        {
                            OpCode = op,
                            Signal = (short)breakinst.Signal,
                            Type = breakinst.Type
                        };
                        break;
                    case InstructionKind.DoubleType:
                        var doubleinst = (DoubleType)inst;
                        bininst.DoubleType = new DoubleTypeInstruction
                        {
                            OpCode = op,
                            Types = new TypePair(doubleinst.Type1, doubleinst.Type2)
                        };
                        if (inst is Compare)
                        {
                            var cmpinst = (Compare)inst;
                            bininst.DoubleType.ComparisonType = cmpinst.ComparisonType;
                        }
                        break;
                    case InstructionKind.SingleType:
                        var singleinst = (SingleType)inst;
                        bininst.SingleType = new SingleTypeInstruction
                        {
                            OpCode = op,
                            Type = singleinst.Type
                        };
                        if (inst is Dup)
                        {
                            var dupinst = (Dup)inst;
                            bininst.SingleType.DupExtra = dupinst.Extra;
                        }
                        break;
                    case InstructionKind.Goto:
                        var gotoinst = (Branch)inst;
                        uint absTarget = 0;
                        if (gotoinst.Label is long)
                        {
                            absTarget = (uint)(long)(gotoinst.Label);
                        }
                        else if (gotoinst.Label is string)
                        {
                            var s = (string)(gotoinst.Label);
                            // TODO
                        }
                        var relTarget = (int)absTarget - (int)size;
                        uint offset = unchecked((uint)relTarget);
                        if (relTarget < 0)
                        {
                            offset &= 0xFFFFFF;
                            offset += 0x1000000;
                        }
                        offset /= 4;
                        bininst.Goto = new BranchInstruction
                        {
                            Offset = new Int24(offset),
                            OpCode = op
                        };
                        break;
                    default:
                        Console.WriteLine("Unknown instruction type " + type + "!");
                        continue;
                }
                binaryInstructions.Add(bininst);
                unsafe
                {
                    size += DisasmExt.Size(&bininst, bcv)*4;
                }
            }

            return new CodeInfo
            {
                Size = (int)size,
                InstructionsCopy = binaryInstructions.ToArray(),
                functionReferences = functionReferences,
                variableReferences = variableReferences
            };
        }

        // strings, vars and funcs are compiled using the other things

        public static GMFile /* errors: different return type? */ ReadFile(string baseDir, JsonData projFile)
        {
            var f = new GMFile();
            // OBJT: depends on SPRT, obj<->id map
            // ROOM: depends on OBJT, BGND
            // SCPT: depends on CODE

            //TODO: implement, emit error if field is not found -> intercept exns from dynamic stuff
            //TODO: (-> surround every call PER FILE with a try/catch)

            if (projFile.Has("general"))
            {
                Console.WriteLine("Loading general...");
                f.General = DeserializeGeneral(JsonMapper.ToObject(File.OpenText(Path.Combine(baseDir, (string)(projFile["general"])))));
            }
            if (projFile.Has("options"))
            {
                Console.WriteLine("Loading options...");
                f.Options = DeserializeOptions(JsonMapper.ToObject(File.OpenText(Path.Combine(baseDir, (string)(projFile["options"])))));
            }
            if (projFile.Has("strings"))
            {
                Console.WriteLine("Loading strings...");
                f.Strings = DeserializeArray(JsonMapper.ToObject(File.OpenText(Path.Combine(baseDir, (string)(projFile["strings"])))), jd => (string)jd.ToString());
            }

            ReferenceDef[] variables=null, functions=null;
            if (projFile.Has("variables"))
            {
                Console.WriteLine("Loading variables...");
                var vardata = JsonMapper.ToObject(File.OpenText(Path.Combine(baseDir, (string)(projFile["variables"]))));
                variables = DeserializeArray(vardata.IsArray ? vardata : vardata["variables"], DeserializeReferenceDef);
                if (vardata.Has("extra"))
                {
                    f.VariableExtra = DeserializeArray(vardata["extra"], jd=>(uint)jd);
                }
            }
            if (projFile.Has("functions"))
            {
                Console.WriteLine("Loading functions...");
                var funcdata = JsonMapper.ToObject(File.OpenText(Path.Combine(baseDir, (string)(projFile["functions"]))));
                functions = DeserializeArray(funcdata.IsArray ? funcdata : funcdata["functions"], DeserializeReferenceDef);
                if (funcdata.Has("locals"))
                {
                    f.FunctionLocals = DeserializeArray(funcdata["locals"], DeserializeFuncLocals);
                }
            }
            f.RefData = new Decomp.RefData { Variables = variables, Functions = functions };

            if (projFile.Has("textures"))
            {
                Console.WriteLine("Loading textures...");
                var textures = projFile["textures"].ToArray();
                f.Textures = new TextureInfo[textures.Length];
                for (int i = 0; i < textures.Length; i++)
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

                    f.Textures[i] = texinfo;
                }
            }
            if (projFile.Has("tpags"))
            {
                Console.Write("Loading texture pages... ");
                var cl = Console.CursorLeft;
                var ct = Console.CursorTop;
                
                var tpags = projFile["tpags"].ToArray();
                f.TexturePages = new TexturePageInfo[tpags.Length];
                for (int i = 0; i < tpags.Length; i++)
                {
                    Console.SetCursorPosition(cl, ct);
                    Console.WriteLine(O_PAREN + (i + 1) + SLASH + tpags.Length + C_PAREN);
                    f.TexturePages[i] = DeserializeTPag(JsonMapper.ToObject(File.ReadAllText(Path.Combine(baseDir, (string)(tpags[i])))));
                }
            }
            if (projFile.Has("audio"))
            {
                Console.WriteLine("Loading audio...");
                var audio = projFile["audio"].ToArray();
                f.Audio = new AudioInfo[audio.Length];
                for (int i = 0; i < audio.Length; i++)
                {
                    var audioinfo = new AudioInfo
                    {
                        Wave = File.ReadAllBytes(Path.Combine(baseDir, (string)(audio[i])))
                    };
                    f.Audio[i] = audioinfo;
                }
            }
            if (projFile.Has("sprites"))
            {
                Console.Write("Loading sprites... ");
                var cl = Console.CursorLeft;
                var ct = Console.CursorTop;

                var sprites = projFile["sprites"].ToArray();
                f.Sprites = new SpriteInfo[sprites.Length];
                for (int i = 0; i < sprites.Length; i++)
                {
                    Console.SetCursorPosition(cl, ct);
                    Console.WriteLine(O_PAREN + (i + 1) + SLASH + sprites.Length + C_PAREN);
                    f.Sprites[i] = DeserializeSprite(JsonMapper.ToObject(File.ReadAllText(Path.Combine(baseDir, (string)(sprites[i])))));
                    f.Sprites[i].Name = Path.GetFileNameWithoutExtension((string)(sprites[i]));
                }
            }
            if (projFile.Has("objs"))
            {
                Console.Write("Loading objects... ");
                var cl = Console.CursorLeft;
                var ct = Console.CursorTop;

                var objs = projFile["objs"].ToArray();
                var objNames = objs.Select(o => Path.GetFileNameWithoutExtension((string)o)).ToArray();
                f.Objects = new ObjectInfo[objs.Length];
                for (int i = 0; i < objs.Length; i++)
                {
                    Console.SetCursorPosition(cl, ct);
                    Console.WriteLine(O_PAREN + (i + 1) + SLASH + objs.Length + C_PAREN);
                    f.Objects[i] = DeserializeObj(
                        JsonMapper.ToObject(File.ReadAllText(Path.Combine(baseDir, (string)(objs[i])))),
                        f.Sprites,
                        s => (uint)Array.IndexOf(objNames, s));
                    f.Objects[i].Name = objNames[i];
                }
            }
            if (projFile.Has("code"))
            {
                Console.WriteLine("Loading code...");
                var code = projFile["code"].ToArray();
                f.Code = new CodeInfo[code.Length];

                IDictionary<string, uint> stringIndices = new Dictionary<string, uint>(f.Strings.Length);
                for (uint i = 0; i < f.Strings.Length; i++) stringIndices[f.Strings[i]] = i;
                IDictionary<string, uint> objectIndices = new Dictionary<string, uint>(f.Objects.Length);
                for (uint i = 0; i < f.Objects.Length; i++) objectIndices[f.Objects[i].Name] = i;

                for (int i = 0; i < code.Length; i++)
                {
                    Console.WriteLine((string)(code[i]));
                    f.Code[i] = DeserializeCodeFromFile(Path.Combine(baseDir, (string)(code[i])), f.General.BytecodeVersion,
                        stringIndices, objectIndices);
                    f.Code[i].Name = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension((string)(code[i])));
                    /*if (f.Code[i].Name == "gml_Script_scr_namingscreen")
                    {
                        return f;
                    }*/
                }
            }
            if (projFile.Has("sounds"))
            {
                Console.WriteLine("Loading sounds...");
                var sounds = projFile["sounds"].ToArray();
                f.Sound = new SoundInfo[sounds.Length];
                for (int i = 0; i < sounds.Length; i++)
                {
                    f.Sound[i] = DeserializeSound(JsonMapper.ToObject(File.ReadAllText(Path.Combine(baseDir, (string)(sounds[i])))));
                    f.Sound[i].Name = Path.GetFileNameWithoutExtension((string)(sounds[i]));
                }
            }
            if (projFile.Has("bg"))
            {
                Console.WriteLine("Loading backgrounds...");
                var bg = projFile["bg"].ToArray();
                f.Backgrounds = new BackgroundInfo[bg.Length];
                for (int i = 0; i < bg.Length; i++)
                {
                    f.Backgrounds[i] = DeserializeBg(JsonMapper.ToObject(File.ReadAllText(Path.Combine(baseDir, (string)(bg[i])))));
                    f.Backgrounds[i].Name = Path.GetFileNameWithoutExtension((string)(bg[i]));
                }
            }
            if (projFile.Has("paths"))
            {
                Console.WriteLine("Loading paths...");
                var paths = projFile["paths"].ToArray();
                f.Paths = new PathInfo[paths.Length];
                for (int i = 0; i < paths.Length; i++)
                {
                    f.Paths[i] = DeserializePath(JsonMapper.ToObject(File.ReadAllText(Path.Combine(baseDir, (string)(paths[i])))));
                    f.Paths[i].Name = Path.GetFileNameWithoutExtension((string)(paths[i]));
                }
            }
            if (projFile.Has("scripts"))
            {
                Console.WriteLine("Loading scripts...");
                var scripts = projFile["scripts"].ToArray();
                f.Scripts = new ScriptInfo[scripts.Length];
                for (int i = 0; i < scripts.Length; i++)
                {
                    f.Scripts[i] = DeserializeScript(JsonMapper.ToObject(File.ReadAllText(Path.Combine(baseDir, (string)(scripts[i])))), f.Code);
                    f.Scripts[i].Name = Path.GetFileNameWithoutExtension((string)(scripts[i]));
                }
            }
            if (projFile.Has("fonts"))
            {
                Console.WriteLine("Loading fonts...");
                var fonts = projFile["fonts"].ToArray();
                f.Fonts = new FontInfo[fonts.Length];
                for (int i = 0; i < fonts.Length; i++)
                {
                    f.Fonts[i] = DeserializeFont(JsonMapper.ToObject(File.ReadAllText(Path.Combine(baseDir, (string)(fonts[i])))));
                    f.Fonts[i].CodeName = Path.GetFileNameWithoutExtension((string)(fonts[i]));
                }
            }
            if (projFile.Has("rooms"))
            {
                Console.Write("Loading rooms... ");
                var cl = Console.CursorLeft;
                var ct = Console.CursorTop;

                var rooms = projFile["rooms"].ToArray();
                f.Rooms = new RoomInfo[rooms.Length];
                for (int i = 0; i < rooms.Length; i++)
                {
                    Console.SetCursorPosition(cl, ct);
                    Console.WriteLine(O_PAREN + (i + 1) + SLASH + rooms.Length + C_PAREN);
                    f.Rooms[i] = DeserializeRoom(JsonMapper.ToObject(File.ReadAllText(Path.Combine(baseDir, (string)(rooms[i])))), f.Backgrounds, f.Objects);
                    f.Rooms[i].Name = Path.GetFileNameWithoutExtension((string)(rooms[i]));
                }
            }
            if (projFile.Has("audiogroups"))
            {
                Console.WriteLine("Loading audio groups...");
                f.AudioGroups = DeserializeArray(JsonMapper.ToObject(File.OpenText(Path.Combine(baseDir, (string)(projFile["audiogroups"])))), jd => (string)jd.ToString());
            }
            return f;
        }
    }
}
