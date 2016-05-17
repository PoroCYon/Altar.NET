using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LitJson;

namespace Altar.Repack
{
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
        static T[] DeserializeArray<T>(dynamic  jArr, Func<dynamic , T> converter)
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

                        if (s.Length != 9 || s.Length != 7 /* (AA)RRGGBB + a '#' */ || (s.Length > 0 && !s.StartsWith(SR.HASH, StringComparison.Ordinal)))
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

        static Point        DeserializePoint  (dynamic j) => new Point  (j.x, j.y);
        static PointF       DeserializePointF (dynamic j) => new PointF (j.x, j.y);
        static Point16      DeserializePoint16(dynamic j) => new Point16(j.x, j.y);
        static Point        DeserializeSize   (dynamic j) => new Point  (j.width, j.height);
        static PointF       DeserializeSizeF  (dynamic j) => new PointF (j.width, j.height);
        static Point16      DeserializeSize16 (dynamic j) => new Point16(j.width, j.height);
        static Rectangle    DeserializeRect   (dynamic j) => new Rectangle   (j.x, j.y, j.width, j.height);
        static Rectangle16  DeserializeRect16 (dynamic j) => new Rectangle16 (j.x, j.y, j.width, j.height);
        static BoundingBox  DeserializeBBox   (dynamic j) => new BoundingBox (j.x, j.y, j.width, j.height);
        static BoundingBox2 DeserializeBBox2  (dynamic j) => new BoundingBox2(j.x, j.y, j.width, j.height);
        static PathPoint    DeserializePathPt (dynamic j) => new PathPoint { Position = DeserializePointF(j.pos), Speed = j.speed };

        #region static FontCharacter DeserializeFontChar  (dynamic j)
        static FontCharacter DeserializeFontChar  (dynamic j) => new FontCharacter
        {
            Character = (char)j["char"],
            TPagFrame = DeserializeRect16(j.frame),
            Shift     = j.shift ,
            Offset    = j.offset
        };
        #endregion
        #region static ObjectPhysics DeserializeObjPhysics(dynamic j)
        static ObjectPhysics DeserializeObjPhysics(dynamic j) => new ObjectPhysics
        {
            Density        = j.density    ,
            Restitution    = j.restitution,
            Group          = j.group      ,
            LinearDamping  = j.lineardamp ,
            AngularDamping = j.angulardamp,
            Friction       = j.friction   ,
            Kinematic      = j.kinematic  ,
            Unknown0       = j.unk0       ,
            Unknown1       = j.unk1
        };
        #endregion

        static RoomBackground DeserializeRoomBg  (dynamic j, BackgroundInfo[] bgs )
        {
            var r = new RoomBackground
            {
                IsEnabled     = j.enabled   ,
                IsForeground  = j.foreground,
                TileX         = j.tilex     ,
                TileY         = j.tiley     ,
                StretchSprite = j.stretch   ,

                Position = DeserializePoint(j.pos  ),
                Speed    = DeserializePoint(j.speed)
            };

            if (((JsonData)j).Has("bg"))
            {
                var i = Array.FindIndex(bgs, b => b.Name == (string)j.bg);

                if (i > -1)
                    r.BgIndex = (uint)i;
                //TODO: emit warning instead
            }

            return r;
        }
        static RoomView       DeserializeRoomView(dynamic j, ObjectInfo    [] objs)
        {
            var r = new RoomView
            {
                IsEnabled = j.enabled,
                View      = DeserializeRect (j.view  ),
                Port      = DeserializeRect (j.port  ),
                Border    = DeserializePoint(j.border),
                Speed     = DeserializePoint(j.speed )
            };

            if (((JsonData)j).Has("obj"))
            {
                var i = Array.FindIndex(objs, oi => oi.Name == (string)j.obj);

                if (i > -1)
                    r.ObjectId = (uint)i;
                //TODO: emit warning instead
            }

            return r;
        }
        static RoomObject     DeserializeRoomObj (dynamic j, ObjectInfo    [] objs)
        {
            var r = new RoomObject
            {
                Position     = DeserializePoint (j.pos  )     ,
                Scale        = DeserializePointF(j.scale)     ,
                Colour       = ParseColour((JsonData)j.colour),
                Rotation     = j.rotation                     ,
                InstanceID   = j.instanceid                   ,
                CreateCodeID = j.createcodeid
            };

            var i = Array.FindIndex(objs, o => o.Name == (string)j.obj);
            if (i > -1)
                r.DefIndex = (uint)i;
            //TODO: emit warning instead

            return r;
        }
        static RoomTile       DeserializeRoomTile(dynamic j, BackgroundInfo[] bgs )
        {
            var r = new RoomTile
            {
                Position       = DeserializePoint (j.pos      ) ,
                SourcePosition = DeserializePoint (j.sourcepos) ,
                Size           = DeserializeSize  (j.size     ) ,
                Scale          = DeserializePointF(j.scale    ) ,
                Colour         = ParseColour((JsonData)j.colour),
                Depth          = j.tiledepth                    ,
                InstanceID     = j.instanceid
            };

            var i = Array.FindIndex(bgs, b => b.Name == (string)j.bg);
            if (i > -1)
                r.DefIndex = (uint)i;
            //TODO: emit warning instead

            return r;
        }

        #region public static GeneralInfo DeserializeGeneral(dynamic j)
        public static GeneralInfo DeserializeGeneral(dynamic j) => new GeneralInfo
        {
            IsDebug        = j.debug,
            FileName       = j.filename,
            Configuration  = j.config,
            GameID         = j.gameid,
            Version        = new Version(j.version),
            WindowSize     = DeserializeSize(j.size),
            LicenceCRC32   = j.licensecrc32,
            DisplayName    = j.displayname,
            Timestamp      = DateTime.Parse(j.timestamp, CultureInfo.InvariantCulture),
            LicenseMD5Hash = DeserializeArray(j.licensemd5, (Func<dynamic, byte>)(jd => (byte)jd)),

            InfoFlags     = (InfoFlags  )Enum.Parse(typeof(InfoFlags  ), (string)j.flags  , true),
            ActiveTargets = (GameTargets)Enum.Parse(typeof(GameTargets), (string)j.targets, true),

            SteamAppID = j.appid
        };
        #endregion
        #region public static OptionInfo  DeserializeOptions(dynamic j)
        public static OptionInfo  DeserializeOptions(dynamic j) => new OptionInfo
        {
            Constants = ((JsonData)j.constants).ToDictionary()
                .ToDictionary(kvp => kvp.Key, kvp => (string)kvp.Value),

            InfoFlags = (InfoFlags)Enum.Parse(typeof(InfoFlags), (string)j.flags, true)
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

        #region public static SoundInfo      DeserializeSound (dynamic j)
        public static SoundInfo DeserializeSound(dynamic j) => new SoundInfo
        {
            IsEmbedded   = j.embedded  ,
            IsCompressed = j.compressed,
            Type         = j.type      ,
            File         = j.file      ,
            VolumeMod    = j.volume    ,
            PitchMod     = j.pitch     ,
            GroupID      = j.groupid
        };
        #endregion
        #region public static SpriteInfo     DeserializeSprite(dynamic j)
        public static SpriteInfo DeserializeSprite(dynamic j) => new SpriteInfo
        {
            BBoxMode         = j.bboxmode,
            SeparateColMasks = j.sepmasks,
            Size             = DeserializeSize (j.size    ),
            Bounding         = DeserializeBBox2(j.bounding),
            Origin           = DeserializePoint(j.origin  ),
            TextureIndices   = DeserializeArray(j.textures, (Func<dynamic, uint>)(jd => (uint)jd)),
            CollisionMasks   = DeserializeArray(j.colmasks, (Func<JsonData, bool[,]>)DeserializeColMask)
        };
        #endregion
        #region public static BackgroundInfo DeserializeBg    (dynamic j)
        public static BackgroundInfo DeserializeBg(dynamic j) => new BackgroundInfo
        {
            TexPageIndex = j.texture
        };
        #endregion
        #region public static PathInfo       DeserializePath  (dynamic j)
        public static PathInfo DeserializePath(dynamic j) => new PathInfo
        {
            IsSmooth  = j.smooth   ,
            IsClosed  = j.closed   ,
            Precision = j.precision,
            Points    = DeserializeArray(j.points, (Func<dynamic, PathPoint>)(d => DeserializePathPt(d)))
        };
        #endregion
        public static ScriptInfo     DeserializeScript(dynamic j, CodeInfo[] code)
        {
            var i = String.IsNullOrEmpty(j.code) ? -1 : Array.FindIndex(code, c => c.Name == (string)j.code);

            return new ScriptInfo { CodeId = i < 0 ? UInt32.MaxValue : (uint)i };
        }
        #region public static FontInfo       DeserializeFont  (dynamic j)
        public static FontInfo DeserializeFont(dynamic j) => new FontInfo
        {
            SystemName   = j.sysname,
            IsBold       = j.bold,
            IsItalic     = j.italic,
            AntiAliasing = Enum.Parse(typeof(FontAntiAliasing), j.antialias),
            Charset      = j.charset,
            TexPagId     = j.texture,
            Scale        = DeserializePointF(j.scale),
            Characters   = DeserializeArray(j.chars, (Func<dynamic, FontCharacter>)(d => DeserializeFontChar(d)))
        };
        #endregion

        public static ObjectInfo      DeserializeObj (dynamic j, SpriteInfo[] sprites, Func<string, uint> objNameToId)
        {
            var spr = String.IsNullOrEmpty(j.sprite) ? -1 : Array.FindIndex(sprites, s => s.Name == (string)j.sprite);

            return new ObjectInfo
            {
                SpriteIndex  = spr < 0 ? UInt32.MaxValue : (uint)spr,
                IsVisible    = j.visible,
                IsSolid      = j.solid,
                Depth        = j.depth,
                IsPersistent = j.persist,

                ParentId   = ((JsonData)j).Has("parent" ) ? (uint?)objNameToId(j.parent) : null,
                TexMaskId  = ((JsonData)j).Has("texmask") ? (uint?)j.texmask             : null,

                Physics     = ((JsonData)j).Has("physics") ? (ObjectPhysics?)DeserializeObjPhysics(j.physics) : null,

                IsSensor       = j.sensor,
                CollisionShape = (CollisionShape)Enum.Parse(typeof(CollisionShape), (string)j.colshape, true),

                OtherFloats = DeserializeArray(j.data  , (Func<dynamic, float>)(d => (float)d)),
                ShapePoints = DeserializeArray(j.points, (Func<dynamic, Point>)(d => DeserializePoint(d)))
            };
        }
        #region public static RoomInfo        DeserializeRoom(dynamic j, BackgroundInfo[] bgs, ObjectInfo[] objs)
        public static RoomInfo DeserializeRoom(dynamic j, BackgroundInfo[] bgs, ObjectInfo[] objs) => new RoomInfo
        {
            Caption              = j.caption    ,
            Speed                = j.speed      ,
            IsPersistent         = j.persist    ,
            EnableViews          = j.enableviews,
            ShowColour           = j.showcolour ,
            ClearDisplayBuffer   = j.clearbuf   ,
            World                = j.world      ,
            MetresPerPixel       = j.metresperpx,
            DrawBackgroundColour = j.drawbgcol  ,

            Size               = DeserializeSize  (j.size    ),
            Colour             = ParseColour      (j.colour  ),
            Bounding           = DeserializeBBox  (j.bounding),
            Gravity            = DeserializePointF(j.gravity ),

            Backgrounds        = DeserializeArray(j.bgs, (Func<dynamic, RoomBackground>)(d => DeserializeRoomBg  (d, bgs ))),
            Views              = DeserializeArray(j.bgs, (Func<dynamic, RoomView      >)(d => DeserializeRoomView(d, objs))),
            Objects            = DeserializeArray(j.bgs, (Func<dynamic, RoomObject    >)(d => DeserializeRoomObj (d, objs))),
            Tiles              = DeserializeArray(j.bgs, (Func<dynamic, RoomTile      >)(d => DeserializeRoomTile(d, bgs )))
        };
        #endregion
        #region public static TexturePageInfo DeserializeTPag(dynamic j)
        public static TexturePageInfo DeserializeTPag(dynamic j) => new TexturePageInfo
        {
            Position      = DeserializePoint16(j.pos     ),
            Size          = DeserializeSize16 (j.size    ),
            RenderOffset  = DeserializePoint16(j.offset  ),
            BoundingBox   = DeserializeRect16 (j.bounding),
            SpritesheetId = j.sheetid
        };
        #endregion

        // strings, vars and funcs are compiled using the other things

        public static GMFile /* errors: different return type? */ ReadFile(string baseDir, JsonData projFile)
        {
            // OBJT: depends on SPRT, obj<->id map
            // ROOM: depends on OBJT, BGND
            // SCPT: depends on CODE

            //TODO: implement, emit error if field is not found -> intercept exns from dynamic stuff
            //TODO: (-> surround every call PER FILE with a try/catch)
#pragma warning disable RECS0083
            throw new NotImplementedException();
#pragma warning restore RECS0083
        }
    }
}
