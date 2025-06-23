using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace WaySearchPoint;

public static class CompatibilityUtils
{
    #region MapMoreIcons compatibility

    private static readonly Dictionary<string, AssetLocation>
        IconIndex = new(StringComparer.OrdinalIgnoreCase);

    private static readonly AssetLocation Fallback =
        new("game", "textures/icons/notfound.png");

    public static AssetLocation TryToGetFromMoreIconsMod(ICoreClientAPI capi, string iconCode)
    {
        if (capi.ModLoader.GetMod("moreicons") == null)
            return Fallback;

        if (IconIndex.Count == 0)
        {
            var assets = capi.Assets.GetMany(
                "textures/icons/worldmap/", "moreicons");

            foreach (var asset in assets)
            {
                string bareName = Path.GetFileNameWithoutExtension(asset.Location.Path);
                if (bareName != null) IconIndex[bareName] = asset.Location;
            }
        }

        return IconIndex.GetValueOrDefault(iconCode, Fallback);
    }

    #endregion

    #region Cartographer compatibility

    public static List<Waypoint> GetSharedWaypointsIfExists(WorldMapManager mapMgr, ICoreAPI api = null)
    {
        try
        {
            if (mapMgr == null) return new List<Waypoint>();

            var layerObj = mapMgr.MapLayers
                .FirstOrDefault(l => l.GetType().Name == "SharedWaypointMapLayer");
            if (layerObj == null) return new List<Waypoint>();

            var layerType = layerObj.GetType();
            var wpMember = (MemberInfo)layerType.GetField("clientWaypoints",
                               BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                           ?? layerType.GetProperty("clientWaypoints",
                               BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (wpMember == null) return new List<Waypoint>();

            var wpListObj = wpMember switch
            {
                FieldInfo fi => fi.GetValue(layerObj),
                PropertyInfo pi => pi.GetValue(layerObj),
                _ => null
            };
            if (wpListObj is not IEnumerable wpEnum) return new List<Waypoint>();

            var list = new List<Waypoint>();
            foreach (var wp in wpEnum)
            {
                if (wp == null) continue;

                list.Add(new Waypoint
                {
                    Title = SafeGet<string>(wp, "Title"),
                    Position = SafeGet<Vec3d>(wp, "Position"),
                    Icon = SafeGet<string>(wp, "Icon") ?? "circle",
                    Color = SafeGet(wp, "Color",
                        ColorUtil.ColorFromRgba(200, 200, 200, 255))
                });
            }

            return list;
        }
        catch (Exception e)
        {
            api?.World?.Logger.Warning($"[MyMod] Could not read shared waypoints: {e}");
            return new List<Waypoint>();
        }
    }

    private static T SafeGet<T>(object obj, string name, T def = default)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var type = obj.GetType();

        var f = type.GetField(name, flags);
        if (f != null && typeof(T).IsAssignableFrom(f.FieldType))
        {
            var v = f.GetValue(obj);
            if (v is T tv) return tv;
        }

        var p = type.GetProperty(name, flags);
        if (p != null && typeof(T).IsAssignableFrom(p.PropertyType))
        {
            var v = p.GetValue(obj);
            if (v is T tv) return tv;
        }

        return def;
    }

    #endregion
}