using LitJson;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Altar.Unpack
{
    public static class Serialize
    {
        // not using the DynamicJson here -> greater perf

        static JsonData CreateObj() => JsonMapper.ToObject(SR.EMPTY_OBJ);
        static JsonData CreateArr() => JsonMapper.ToObject(SR.BRACKETS );

        static JsonData SerializeArray<T, TRet>(IEnumerable<T> coll, Func<T, TRet> converter)
        {
            if (coll      == null)
                throw new ArgumentNullException(nameof(coll     ));
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));

            var r = CreateArr();

            foreach (var t in coll) r.Add(converter(t));

            return r;
        }

        static JsonData SerializePoint(Point   p)
        {
            var r = CreateObj();

            r["x"] = p.X;
            r["y"] = p.Y;

            return r;
        }
        static JsonData SerializePoint(PointF  p)
        {
            var r = CreateObj();

            r["x"] = p.X;
            r["y"] = p.Y;

            return r;
        }
        static JsonData SerializePoint(Point16 p)
        {
            var r = CreateObj();

            r["x"] = p.X;
            r["y"] = p.Y;

            return r;
        }
        static JsonData SerializeSize (Point   p)
        {
            var r = CreateObj();

            r["width" ] = p.X;
            r["height"] = p.Y;

            return r;
        }
        static JsonData SerializeSize (PointF  p)
        {
            var r = CreateObj();

            r["width" ] = p.X;
            r["height"] = p.Y;

            return r;
        }
        static JsonData SerializeSize (Point16 p)
        {
            var r = CreateObj();

            r["width" ] = p.X;
            r["height"] = p.Y;

            return r;
        }
        static JsonData SerializeRect (Rectangle    r)
        {
            var ret = CreateObj();

            ret["x"     ] = r.X;
            ret["y"     ] = r.Y;
            ret["width" ] = r.Width;
            ret["height"] = r.Height;

            return ret;
        }
        static JsonData SerializeRect (Rectangle16  r)
        {
            var ret = CreateObj();

            ret["x"     ] = r.X;
            ret["y"     ] = r.Y;
            ret["width" ] = r.Width;
            ret["height"] = r.Height;

            return ret;
        }
        static JsonData SerializeRect (BoundingBox  r)
        {
            var ret = CreateObj();

            ret["top"   ] = r.Top;
            ret["left"  ] = r.Left;
            ret["bottom"] = r.Bottom;
            ret["right" ] = r.Right;

            return ret;
        }
        static JsonData SerializeRect (BoundingBox2 r)
        {
            var ret = CreateObj();

            ret["top"   ] = r.Top;
            ret["left"  ] = r.Left;
            ret["bottom"] = r.Bottom;
            ret["right" ] = r.Right;

            return ret;
        }
        static JsonData SerializePoint(PathPoint p)
        {
            var r = CreateObj();

            r["pos"  ] = SerializePoint(p.Position);
            r["speed"] = p.Speed;

            return r;
        }

        static JsonData SerializeFontChar  (FontCharacter c)
        {
            var e = CreateObj();

            e["char"  ] = c.Character;
            e["frame" ] = SerializeRect(c.TPagFrame);
            e["shift" ] = c.Shift;
            e["offset"] = c.Offset;

            return e;
        }
        static JsonData SerializeObjPhysics(ObjectPhysics p)
        {
            var r = CreateObj();

            r["density"    ] = p.Density;
            r["restitution"] = p.Restitution;
            r["group"      ] = p.Group;
            r["lineardamp" ] = p.LinearDamping;
            r["angulardamp"] = p.AngularDamping;
            r["friction"   ] = p.Friction;
            r["kinematic"  ] = p.Kinematic;
            r["unk0"       ] = p.Unknown0;
            r["unk1"       ] = p.Unknown1;

            return r;
        }

        static JsonData SerializeRoomBg  (RoomBackground bg  , LazyArray<BackgroundInfo> bgs )
        {
            var r = CreateObj();

            r["enabled"   ] = bg.IsEnabled;
            r["foreground"] = bg.IsForeground;
            r["pos"       ] = SerializePoint(bg.Position);
            r["tilex"     ] = bg.TileX;
            r["tiley"     ] = bg.TileY;
            r["speed"     ] = SerializePoint(bg.Speed);
            r["stretch"   ] = bg.StretchSprite;

            if (bg.BgIndex.HasValue)
                r["bg"] = bgs[bg.BgIndex.Value].Name;

            return r;
        }
        static JsonData SerializeRoomView(RoomView       view, LazyArray<ObjectInfo> objs)
        {
            var r = CreateObj();

            r["enabled"] = view.IsEnabled;
            r["view"   ] = SerializeRect(view.View);
            r["port"   ] = SerializeRect(view.Port);
            r["border" ] = SerializePoint(view.Border);
            r["speed"  ] = SerializePoint(view.Speed);

            if (view.ObjectId.HasValue)
                r["obj"] = objs[view.ObjectId.Value].Name;

            return r;
        }
        static JsonData SerializeRoomObj (RoomObject     obj , LazyArray<ObjectInfo> objs)
        {
            var r = CreateObj();

            r["pos"     ] = SerializePoint(obj.Position);
            r["obj"     ] = objs[obj.DefIndex].Name;
            r["scale"   ] = SerializePoint(obj.Scale);
            r["colour"  ] = obj.Colour.ToHexString();
            r["rotation"] = obj.Rotation;

            r["instanceid"  ] = obj.InstanceID  ;
            r["createcodeid"] = obj.CreateCodeID;

            return r;
        }
        static JsonData SerializeRoomTile(RoomTile       tile, LazyArray<BackgroundInfo> bgs )
        {
            var r = CreateObj();

            r["pos"      ] = SerializePoint(tile.Position);
            r["bg"       ] = bgs[tile.DefIndex].Name;
            r["sourcepos"] = SerializePoint(tile.SourcePosition);
            r["size"     ] = SerializeSize (tile.Size);
            r["scale"    ] = SerializePoint(tile.Scale);
            r["colour"   ] = tile.Colour.ToHexString();

            r["tiledepth" ] = tile.Depth     ;
            r["instanceid"] = tile.InstanceID;

            return r;
        }

        static JsonData SerializeRoomObjInst(RoomObjInst oi)
        {
            var r = CreateObj();
            r["name"     ] = oi.Name;
            r["index"    ] = oi.Index;
            r["unk1"     ] = oi.Unk1 ;
            r["depth"    ] = oi.Depth;
            r["unk3"     ] = oi.Unk3 ;
            r["instances"] = SerializeArray(oi.Instances, Utils.Identity);
            return r;
        }

        static JsonData SerializeColMask(bool[,] colMask)
        {
            var j = CreateObj();
            var a = CreateArr();

            int w = colMask.GetLength(0),
                h = colMask.GetLength(1);

            j["w"] = w;
            j["h"] = h;

            for (int y = 0; y < h; y++)
            {
                var l = CreateArr();

                for (int x = 0; x < w; x++)
                    l.Add(colMask[x, y]);

                a.Add(l);
            }

            j["data"] = a;

            return j;
        }

        public static JsonData SerializeGeneral(GeneralInfo gen8)
        {
            var r = CreateObj();

            r["debug"   ] = gen8.IsDebug;
            r["bytecodeversion"] = gen8.BytecodeVersion;
            r["filename"] = gen8.FileName;
            r["config"  ] = gen8.Configuration;
            r["gameid"  ] = gen8.GameID;
            r["name"    ] = gen8.Name;
            r["version" ] = gen8.Version.ToString();
            r["windowsize"] = SerializeSize(gen8.WindowSize);
            r["licensemd5"] = SerializeArray(gen8.LicenseMD5Hash, Utils.Identity);
            r["licensecrc32"] = gen8.LicenceCRC32;
            r["displayname" ] = gen8.DisplayName;
            r["timestamp"   ] = gen8.Timestamp.ToString(SR.SHORT_L /* 's': sortable */);

            r["targets"] = gen8.ActiveTargets.ToString();
            r["unknown"] = SerializeArray(gen8.unknown, Utils.Identity);
            r["appid"  ] = gen8.SteamAppID              ;
            r["flags"  ] = gen8.InfoFlags    .ToString();

            r["numbers"] = SerializeArray(gen8.WeirdNumbers, Utils.Identity);

            return r;
        }
        public static JsonData SerializeOptions(OptionInfo  optn)
        {
            var r = CreateObj();

            r["flags"] = optn.InfoFlags.ToString();

            r["pad0"] = SerializeArray(optn._pad0, Utils.Identity);
            r["pad1"] = SerializeArray(optn._pad1, Utils.Identity);

            r["constants"] = CreateObj();
            foreach (var kvp in optn.Constants)
                r["constants"][kvp.Key] = kvp.Value;

            return r;
        }

        public static JsonData SerializeSound (SoundInfo      sond)
        {
            var r = CreateObj();

            r["embedded"  ] = sond.IsEmbedded;
            r["compressed"] = sond.IsCompressed;
            r["type"      ] = sond.Type;
            r["file"      ] = sond.File;
            r["volume"    ] = sond.VolumeMod;
            r["pitch"     ] = sond.PitchMod;
            r["group"     ] = sond.Group;
            r["audioid"   ] = sond.AudioID;

            return r;
        }
        public static JsonData SerializeSprite(SpriteInfo     sprt)
        {
            var r = CreateObj();

            r["size"    ] = SerializeSize(sprt.Size);
            r["bounding"] = SerializeRect(sprt.Bounding);
            r["bboxmode"] = sprt.BBoxMode;
            r["sepmasks"] = sprt.SeparateColMasks;
            r["origin"  ] = SerializePoint(sprt.Origin);
            if (sprt.Version >= 2)
            {
                r["unknown1"] = sprt.UnknownFloat;
            }
            if (sprt.TextureIndices != null)
                r["textures"] = SerializeArray(sprt.TextureIndices, Utils.Identity);

            if (sprt.CollisionMasks != null)
                r["colmasks"] = SerializeArray(sprt.CollisionMasks, SerializeColMask);

            return r;
        }
        public static JsonData SerializeBg    (BackgroundInfo bgnd)
        {
            var r = CreateObj();

            if (bgnd.TexPageIndex.HasValue) r["texture"] = bgnd.TexPageIndex.Value;

            return r;
        }
        public static JsonData SerializePath  (PathInfo       path)
        {
            var r = CreateObj();

            r["smooth"   ] = path.IsSmooth;
            r["closed"   ] = path.IsClosed;
            r["precision"] = path.Precision;
            r["points"   ] = SerializeArray(path.Points, SerializePoint);

            return r;
        }
        public static JsonData SerializeScript(ScriptInfo     scpt, LazyArray<CodeInfo> code)
        {
            var r = CreateObj();

            r["code"] = scpt.CodeId == UInt32.MaxValue ? String.Empty : code[scpt.CodeId].Name;

            return r;
        }
        public static JsonData SerializeFont  (FontInfo       font)
        {
            var r = CreateObj();

            r["sysname"  ] = font.SystemName;
            r["emsize"   ] = font.EmSize;
            r["bold"     ] = font.IsBold;
            r["italic"   ] = font.IsItalic;
            r["antialias"] = font.AntiAliasing.ToString().ToLowerInvariant();
            r["charset"  ] = font.Charset;
            r["texture"  ] = font.TexPagId;
            r["scale"    ] = SerializePoint(font.Scale);

            r["chars"] = SerializeArray(font.Characters, SerializeFontChar);

            return r;
        }

        public static JsonData SerializeObj   (ObjectInfo objt, LazyArray<SpriteInfo> sprites, LazyArray<ObjectInfo> objs)
        {
            var r = CreateObj();

            r["sprite" ] = objt.SpriteIndex == UInt32.MaxValue ? String.Empty : sprites[objt.SpriteIndex].Name;
            r["visible"] = objt.IsVisible;
            r["solid"  ] = objt.IsSolid;
            r["depth"  ] = objt.Depth;
            r["persist"] = objt.IsPersistent;

            if (objt.ParentId .HasValue)
                r["parent" ] = objs[objt.ParentId.Value].Name;
            if (objt.TexMaskId.HasValue)
                r["texmask"] = objt.TexMaskId.Value;

            if (objt.Physics.HasValue)
                r["physics"] = SerializeObjPhysics(objt.Physics.Value);

            r["sensor"  ] = objt.IsSensor;
            r["colshape"] = objt.CollisionShape.ToString().ToLowerInvariant();

            r["data"  ] = SerializeArray(objt.OtherFloats, Utils.Identity);

            if (objt.ShapePoints != null)
                r["points"] = SerializeArray(objt.ShapePoints, x => SerializeArray(x, y => SerializeArray(y, Utils.Identity)));

            return r;
        }
        public static JsonData SerializeRoom  (RoomInfo room, LazyArray<BackgroundInfo> bgs, LazyArray<ObjectInfo> objs)
        {
            var r = CreateObj();

            r["caption"    ] = room.Caption;
            r["size"       ] = SerializeSize(room.Size);
            r["speed"      ] = room.Speed;
            r["persist"    ] = room.IsPersistent;
            r["colour"     ] = room.Colour.ToHexString();
            r["enableviews"] = room.EnableViews;
            r["showcolour" ] = room.ShowColour;
            r["clearbuf"   ] = room.ClearDisplayBuffer;
            r["flag"       ] = room.UnknownFlag;
            r["world"      ] = room.World;
            r["bounding"   ] = SerializeRect(room.Bounding);
            r["gravity"    ] = SerializePoint(room.Gravity);
            r["metresperpx"] = room.MetresPerPixel;

            r["drawbgcol"] = room.DrawBackgroundColour;
            r["unknown"] = room._unknown;

            r["bgs"  ] = SerializeArray(room.Backgrounds, b => SerializeRoomBg  (b, bgs ));
            r["views"] = SerializeArray(room.Views      , v => SerializeRoomView(v, objs));
            r["objs" ] = SerializeArray(room.Objects    , o => SerializeRoomObj (o, objs));
            r["tiles"] = SerializeArray(room.Tiles      , t => SerializeRoomTile(t, bgs ));
            if (room.ObjInst != null) r["objinst"] = SerializeArray(room.ObjInst, i => SerializeRoomObjInst(i));

            return r;
        }
        public static JsonData SerializeTPag  (TexturePageInfo tpag)
        {
            var r = CreateObj();

            r["src"    ] = SerializeRect(tpag.Source     );
            r["dest"   ] = SerializeRect(tpag.Destination);
            r["size"   ] = SerializeSize(tpag.Size       );
            r["sheetid"] = tpag.SpritesheetId;

            return r;
        }
        public static JsonData SerializeShader(ShaderInfo shdr)
        {
            var r = CreateObj();

            r["sources"   ] = SerializeArray(shdr.Sources         , Utils.Identity);
            r["attributes"] = SerializeArray(shdr.VertexAttributes, Utils.Identity);

            return r;
        }

        public static JsonData SerializeStrings(GMFile f) => SerializeArray(f.Strings, Utils.Identity);
        public static JsonData SerializeAudioGroups(GMFile f) => SerializeArray(f.AudioGroups, Utils.Identity);

        private static JsonData SerializeFuncLocalsInfo(FunctionLocalsInfo fli)
        {
            var r = CreateObj();

            r["name"] = fli.FunctionName;
            r["locals"] = SerializeArray(fli.LocalNames, Utils.Identity);

            return r;
        }

        private static JsonData SerializeReferenceDef(ReferenceDef rd)
        {
            if (!rd.HasExtra)
            {
                return new JsonData(rd.Name);
            }
            var r = CreateObj();

            r["name"] = rd.Name;
            //r["occurrences"] = rd.Occurrences;
            //r["type"] = rd.VariableType.ToString();
            //if (rd.FirstOffset != 0xFFFFFFFF)
            //    r["firstoffset"] = rd.FirstOffset;
            if (rd.HasExtra)
            {
                r["instancetype"] = (int)rd.InstanceType;
                r["unknown"] = rd.unknown2;
            }

            return r;
        }

        public static JsonData SerializeVars(GMFile f)
        {
            var r = CreateObj();

            if (f.VariableExtra != null) r["extra"] = SerializeArray(f.VariableExtra, Utils.Identity);
            r["variables"] = SerializeArray(f.RefData.Variables, SerializeReferenceDef);

            return r;
        }

        public static JsonData SerializeFuncs(GMFile f) {
            var r = CreateObj();

            r["functions"] = SerializeArray(f.RefData.Functions, SerializeReferenceDef);
            if (f.FunctionLocals != null) r["locals"] = SerializeArray(f.FunctionLocals, SerializeFuncLocalsInfo);

            return r;
        }

        public unsafe static JsonData SerializeProject(GMFile f, ExportOptions eo, List<IntPtr> chunks = null)
        {
            var r = CreateObj();

            r["chunkorder"] = SerializeArray(f.ChunkOrder, sh => sh.ToChunkName());

            if (eo.General) r["general"] = "general.json";
            if (eo.Options) r["options"] = "options.json";

            if (eo.String   ) r["strings"  ] = "strings.json";
            if (eo.Variables) r["variables"] = "variables.json";
            if (eo.Functions) r["functions"] = "functions.json";

            // ---

            if (eo.Texture)
            {
                r["textures"] = CreateArr();
                for (int i = 0; i < f.Textures.Length; i++)
                    r["textures"].Add(SR.DIR_TEX + i.ToString(CultureInfo.InvariantCulture) + SR.EXT_PNG);
            }

            if (eo.TPag)
            {
                r["tpags"] = CreateArr();
                for (int i = 0; i < f.TexturePages.Length; i++)
                    r["tpags"].Add(SR.DIR_TXP + i.ToString(CultureInfo.InvariantCulture) + SR.EXT_JSON); 
            }

            // ---

            var infoTable = new Dictionary<int, SoundInfo>();

            foreach (var s in f.Sound)
                if ((s.IsEmbedded || s.IsCompressed) && s.AudioID != -1)
                    infoTable[s.AudioID] = s;

            if (eo.Audio)
            {
                r["audio"] = CreateArr();
                for (int i = 0; i < f.Audio.Length; i++)
                    r["audio"].Add(SR.DIR_WAV + infoTable[i].Name + SR.EXT_WAV); 
            }

            if (eo.Disassemble || eo.Decompile)
            {
                r["code"] = CreateArr();
                for (int i = 0; i < f.Code.Length; i++)
                {
                    if (eo.Disassemble)
                        r["code"].Add(SR.DIR_CODE + f.Code[i].Name + SR.EXT_GML_ASM);
                    if (eo.Decompile)
                        r["code"].Add(SR.DIR_CODE + f.Code[i].Name + SR.EXT_GML_LSP);
                }
            }

            // ---

            if (f.Sound       != null && eo.Sound     ) r["sounds" ] = SerializeArray(f.Sound      , s => SR.DIR_SND  + s.Name     + SR.EXT_JSON);
            if (f.Sprites     != null && eo.Sprite    ) r["sprites"] = SerializeArray(f.Sprites    , s => SR.DIR_SPR  + s.Name     + SR.EXT_JSON);
            if (f.Backgrounds != null && eo.Background) r["bg"     ] = SerializeArray(f.Backgrounds, s => SR.DIR_BG   + s.Name     + SR.EXT_JSON);
            if (f.Paths       != null && eo.Path      ) r["paths"  ] = SerializeArray(f.Paths      , s => SR.DIR_PATH + s.Name     + SR.EXT_JSON);
            if (f.Scripts     != null && eo.Script    ) r["scripts"] = SerializeArray(f.Scripts    , s => SR.DIR_SCR  + s.Name     + SR.EXT_JSON);
            if (f.Fonts       != null && eo.Font      ) r["fonts"  ] = SerializeArray(f.Fonts      , s => SR.DIR_FNT  + s.CodeName + SR.EXT_JSON);
            if (f.Objects     != null && eo.Object    ) r["objs"   ] = SerializeArray(f.Objects    , s => SR.DIR_OBJ  + s.Name     + SR.EXT_JSON);
            if (f.Rooms       != null && eo.Room      ) r["rooms"  ] = SerializeArray(f.Rooms      , s => SR.DIR_ROOM + s.Name     + SR.EXT_JSON);
            if (f.Shaders     != null && eo.Shader    ) r["shaders"] = SerializeArray(f.Shaders    , s => SR.DIR_SHDR + s.Name     + SR.EXT_JSON);

            if (f.AudioGroups != null && eo.AudioGroups) r["audiogroups"] = "audiogroups.json";

            if (chunks != null)
            {
                r["chunks"] = CreateArr();

                for (int i = 0; i < chunks.Count; i++)
                {
                    var hdr = (SectionHeader*)chunks[i];
                    if (hdr != null && (!hdr->IsEmpty() || eo.DumpEmptyChunks))
                        r["chunks"].Add(hdr->MagicString() + SR.EXT_BIN);
                }
            }

            return r;
        }
    }
}
