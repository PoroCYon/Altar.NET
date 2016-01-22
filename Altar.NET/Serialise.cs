using System;
using System.Collections.Generic;
using System.Linq;
using LitJson;

namespace Altar
{
    public static class Serialise
    {
        static JsonData CreateObj() => JsonMapper.ToObject(SR.EMPTY_OBJ);
        static JsonData CreateArr() => JsonMapper.ToObject(SR.BRACKETS );

        static JsonData SerialisePoint(Point   p)
        {
            var r = CreateObj();

            r["x"] = p.X;
            r["y"] = p.Y;

            return r;
        }
        static JsonData SerialisePoint(PointF  p)
        {
            var r = CreateObj();

            r["x"] = p.X;
            r["y"] = p.Y;

            return r;
        }
        static JsonData SerialisePoint(Point16 p)
        {
            var r = CreateObj();

            r["x"] = p.X;
            r["y"] = p.Y;

            return r;
        }
        static JsonData SerialiseSize (Point   p)
        {
            var r = CreateObj();

            r["width"] = p.X;
            r["height"] = p.Y;

            return r;
        }
        static JsonData SerialiseSize (PointF  p)
        {
            var r = CreateObj();

            r["width"] = p.X;
            r["height"] = p.Y;

            return r;
        }
        static JsonData SerialiseSize (Point16 p)
        {
            var r = CreateObj();

            r["width"] = p.X;
            r["height"] = p.Y;

            return r;
        }
        static JsonData SerialiseRect (Rectangle    r)
        {
            var ret = CreateObj();

            ret["x"     ] = r.X;
            ret["y"     ] = r.Y;
            ret["width" ] = r.Width;
            ret["height"] = r.Height;

            return ret;
        }
        static JsonData SerialiseRect (Rectangle16  r)
        {
            var ret = CreateObj();

            ret["x"     ] = r.X;
            ret["y"     ] = r.Y;
            ret["width" ] = r.Width;
            ret["height"] = r.Height;

            return ret;
        }
        static JsonData SerialiseRect (BoundingBox  r)
        {
            var ret = CreateObj();

            ret["top"   ] = r.Top;
            ret["left"  ] = r.Left;
            ret["bottom"] = r.Bottom;
            ret["right" ] = r.Right;

            return ret;
        }
        static JsonData SerialiseRect (BoundingBox2 r)
        {
            var ret = CreateObj();

            ret["top"   ] = r.Top;
            ret["left"  ] = r.Left;
            ret["bottom"] = r.Bottom;
            ret["right" ] = r.Right;

            return ret;
        }
        static JsonData SerialisePoint(PathPoint p)
        {
            var r = CreateObj();

            r["pos"  ] = SerialisePoint(p.Position);
            r["speed"] = p.Speed;

            return r;
        }

        static JsonData SerialiseObjPhysics(ObjectPhysics p)
        {
            var r = CreateObj();

            r["density"     ] = p.Density;
            r["restitution" ] = p.Restitution;
            r["group"       ] = p.Group;
            r["linear-damp" ] = p.LinearDamping;
            r["angular-damp"] = p.AngularDamping;
            r["friction"    ] = p.Friction;
            r["kinematic"   ] = p.Kinematic;
            r["unk-0"       ] = p.Unknown0;
            r["unk-1"       ] = p.Unknown1;

            return r;
        }

        static JsonData SerialiseRoomBg  (RoomBackground bg)
        {
            var r = CreateObj();

            r["enabled"   ] = bg.IsEnabled;
            r["foreground"] = bg.IsForeground;
            r["bg-index"  ] = bg.BgIndex;
            r["pos"       ] = SerialisePoint(bg.Position);
            r["tile-x"    ] = bg.TileX;
            r["tile-y"    ] = bg.TileY;
            r["speed"     ] = SerialisePoint(bg.Speed);
            r["stretch"   ] = bg.StretchSprite;

            return r;
        }
        static JsonData SerialiseRoomView(GMFile f, RoomView view)
        {
            var r = CreateObj();

            r["enabled"] = view.IsEnabled;
            r["view"   ] = SerialiseRect(view.View);
            r["port"   ] = SerialiseRect(view.Port);
            r["border" ] = SerialisePoint(view.Border);
            r["speed"  ] = SerialisePoint(view.Speed);

            if (view.ObjectId.HasValue) r["obj"] = f.Objects[view.ObjectId.Value].Name;

            return r;
        }
        static JsonData SerialiseRoomObj (GMFile f, RoomObject obj)
        {
            var r = CreateObj();

            r["pos"     ] = SerialisePoint(obj.Position);
            r["obj"     ] = f.Objects[obj.DefIndex].Name;
            r["scale"   ] = SerialisePoint(obj.Scale);
            r["colour"  ] = SR.HASH + obj.Colour.ToHexString();
            r["rotation"] = obj.Rotation;

            return r;
        }
        static JsonData SerialiseRoomTile(GMFile f, RoomTile tile)
        {
            var r = CreateObj();

            r["pos"       ] = SerialisePoint(tile.Position);
            r["bg"        ] = f.Backgrounds [tile.DefIndex].Name;
            r["source-pos"] = SerialisePoint(tile.SourcePosition);
            r["size"      ] = SerialiseSize (tile.Size);
            r["scale"     ] = SerialisePoint(tile.Scale);
            r["colour"    ] = SR.HASH + tile.Colour.ToHexString();

            return r;
        }

        public static JsonData SerialiseGeneral(GMFile f)
        {
            var r = CreateObj();
            var gen8 = f.General;

            r["debug"     ] = gen8.IsDebug;
            r["bc-version"] = gen8.BytecodeVersion;
            r["filename"  ] = gen8.FileName;
            r["config"    ] = gen8.Configuration;
            r["game-id"   ] = gen8.GameId;
            r["version"   ] = gen8.Version.ToString();

            r["window-size"] = SerialiseSize(gen8.WindowSize);

            r["license-md5"] = CreateArr();
            for (int i = 0; i < gen8.LicenseMD5Hash.Length; i++)
                r["license-md5"][i] = gen8.LicenseMD5Hash[i];

            r["license-crc32"] = gen8.LicenceCRC32;
            r["display-name" ] = gen8.DisplayName;
            r["timestamp"    ] = gen8.Timestamp.ToString(SR.SHORT_L /* 's': sortable */);

            return r;
        }
        public static JsonData SerialiseOptions(GMFile f)
        {
            var r = CreateObj();
            var optn = f.Options;

            foreach (var kvp in optn.Constants)
                r["constants"][kvp.Key] = kvp.Value;

            return r;
        }

        public static JsonData SerialiseSound (GMFile f, int id)
        {
            var r = CreateObj();
            var sond = f.Sound[id];

            r["embedded"  ] = sond.IsEmbedded;
            r["compressed"] = sond.IsCompressed;
            r["type"      ] = sond.Type;
            r["file"      ] = sond.File;
            r["volume"    ] = sond.VolumeMod;
            r["pitch"     ] = sond.PitchMod;
            r["pan"       ] = sond.PanMod;

            return r;
        }
        public static JsonData SerialiseSprite(GMFile f, int id)
        {
            var r = CreateObj();
            var sprt = f.Sprites[id];

            r["size"     ] = SerialiseSize(sprt.Size);
            r["bounding" ] = SerialiseRect(sprt.Bounding);
            r["bbox-mode"] = sprt.BBoxMode;
            r["sepmasks" ] = sprt.SepMasks;
            r["origin"   ] = SerialisePoint(sprt.Origin);

            r["textures"] = CreateArr();
            for (int i = 0; i < sprt.TextureIndices.Length; i++)
                r["textures"][i] = sprt.TextureIndices[i];

            return r;
        }
        public static JsonData SerialiseBg    (GMFile f, int id)
        {
            var r = CreateObj();
            var bgnd = f.Backgrounds[id];

            r["texture"] = bgnd.TexPageIndex;

            return r;
        }
        public static JsonData SerialisePath  (GMFile f, int id)
        {
            var r = CreateObj();
            var path = f.Paths[id];

            r["smooth"   ] = path.IsSmooth;
            r["closed"   ] = path.IsClosed;
            r["precision"] = path.Precision;

            r["points"] = CreateArr();
            for (int i = 0; i < path.Points.Length; i++)
                r["points"][i] = SerialisePoint(path.Points[i]);

            return r;
        }
        public static JsonData SerialiseScript(GMFile f, int id)
        {
            var r = CreateObj();
            var scpt = f.Scripts[id];

            r["code"] = f.Code[scpt.CodeId].Name;

            return r;
        }
        public static JsonData SerialiseFonts (GMFile f, int id)
        {
            var r = CreateObj();
            var font = f.Fonts[id];

            r["code-name" ] = font.CodeName;
            r["sys-name"  ] = font.SystemName;
            r["bold"      ] = font.IsBold;
            r["italic"    ] = font.IsItalic;
            r["anti-alias"] = font.AntiAliasing.ToString().ToLowerInvariant();
            r["charset"   ] = font.Charset;
            r["texture"   ] = font.TexPagId;
            r["scale"     ] = SerialisePoint(font.Scale);

            r["chars"] = CreateArr();
            for (int i = 0; i < font.Characters.Length; i++)
            {
                r["chars"]["char"  ] = font.Characters[i].Character;
                r["chars"]["framle"] = SerialiseRect(font.Characters[i].TPagFrame);
                r["chars"]["shift" ] = font.Characters[i].Shift;
                r["chars"]["offset"] = font.Characters[i].Offset;
            }

            return r;
        }
        public static JsonData SerialiseObj   (GMFile f, int id)
        {
            var r = CreateObj();
            var objt = f.Objects[id];

            r["sprite" ] = f.Sprites[objt.SpriteIndex].Name;
            r["visible"] = objt.IsVisible;
            r["solid"  ] = objt.IsSolid;
            r["depth"  ] = objt.Depth;
            r["persist"] = objt.IsPersistent;

            if (objt.ParentId .HasValue) r["parent" ] = f.Objects[objt.ParentId.Value].Name;
            if (objt.TexMaskId.HasValue) r["texmask"] = objt.TexMaskId.Value;

            r["physics"] = SerialiseObjPhysics(objt.Physics);

            r["data"] = CreateArr();
            for (int i = 0; i < objt.OtherFloats.Length; i++)
                r["data"][i] = objt.OtherFloats[i];

            r["points"] = CreateArr();
            for (int i = 0; i < objt.ShapePoints.Length; i++)
                r["points"][i] = SerialisePoint(objt.ShapePoints[i]);

            return r;
        }
        public static JsonData SerialiseRoom  (GMFile f, int id)
        {
            var r = CreateObj();
            var room = f.Rooms[id];

            r["caption"      ] = room.Caption;
            r["size"         ] = SerialiseSize(room.Size);
            r["speed"        ] = room.Speed;
            r["persist"      ] = room.IsPersistent;
            r["colour"       ] = SR.HASH + room.Colour.ToHexString();
            r["enable-views" ] = room.EnableViews;
            r["show-colour"  ] = room.ShowColour;
            r["world"        ] = room.World;
            r["bounding"     ] = SerialiseRect(room.Bounding);
            r["gravity"      ] = SerialisePoint(room.Gravity);
            r["metres-per-px"] = room.MetresPerPixel;

            r["bgs"] = CreateArr();
            for (int i = 0; i < room.Backgrounds.Length; i++)
                r["bgs"][i] = SerialiseRoomBg(room.Backgrounds[i]);
            r["views"] = CreateArr();
            for (int i = 0; i < room.Views.Length; i++)
                r["views"][i] = SerialiseRoomView(f, room.Views[i]);
            r["objs"] = CreateArr();
            for (int i = 0; i < room.Objects.Length; i++)
                r["objs"][i] = SerialiseRoomObj(f, room.Objects[i]);
            r["tiles"] = CreateArr();
            for (int i = 0; i < room.Tiles.Length; i++)
                r["tiles"][i] = SerialiseRoomTile(f, room.Tiles[i]);

            return r;
        }
        public static JsonData SerialiseTPag  (GMFile f, int id)
        {
            var r = CreateObj();
            var tpag = f.TexturePages[id];

            r["pos"     ] = SerialisePoint(tpag.Position);
            r["size"    ] = SerialiseSize(tpag.Size);
            r["offset"  ] = SerialisePoint(tpag.RenderOffset);
            r["bounding"] = SerialiseRect(tpag.BoundingBox);
            r["sheet-id"] = tpag.SpritesheetId;

            return r;
        }

        public static JsonData SerialiseStrings(GMFile f)
        {
            var r = CreateArr();

            var strg = f.Strings;
            for (int i = 0; i < strg.Length; i++)
                r[i] = strg[i];

            return r;
        }

        public static JsonData SerialiseVariables(GMFile f)
        {
            var r = CreateArr();

            var rdata = f.RefData;

            for (int i = 0; i < rdata.Variables.Length; i++)
                r[i] = rdata.Variables[i].Name;

            return r;
        }
        public static JsonData SerialiseFunctions(GMFile f)
        {
            var r = CreateArr();

            var rdata = f.RefData;

            for (int i = 0; i < rdata.Functions.Length; i++)
                r[i] = rdata.Functions[i].Name;

            return r;
        }

        //TODO: serialise project build file
    }
}
