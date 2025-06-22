using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace WaySearchPoint;

public static class WaySearchPointUtils
{
    public static IEnumerable<Waypoint> GetSortedMatches(string text, List<Waypoint> waypoints,
        SortOptions selectedSortOption, Vec3d playerPosition)
    {
        string lowerText = text.ToLowerInvariant();

        var matches = waypoints.Where(wp =>
            (!string.IsNullOrEmpty(wp.Title) && wp.Title.ToLowerInvariant().Contains(lowerText)) ||
            (!string.IsNullOrEmpty(wp.Text) && wp.Text.ToLowerInvariant().Contains(lowerText))
        );
        return GetSorted(matches, selectedSortOption, playerPosition);
    }

    public static IEnumerable<Waypoint> GetSorted(IEnumerable<Waypoint> waypoints, SortOptions selectedSortOption,
        Vec3d playerPosition)
    {
        switch (selectedSortOption)
        {
            case SortOptions.Alphabetically:
                return waypoints.OrderBy(wp => wp.Title ?? string.Empty);

            case SortOptions.ByDistance:
                return waypoints.OrderBy(wp => GetDistanceBetween(playerPosition, wp.Position));

            default:
                return waypoints;
        }
    }

    public static double GetDistanceBetween(Vec3d wp1, Vec3d wp2)
    {
        var dx = wp2.X - wp1.X;
        var dy = wp2.Z - wp1.Z;

        return Math.Sqrt(dx * dx + dy * dy);
    }


    /// getting Shared waypoints from Cartographer mod if mod installed
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

        // field?
        var f = type.GetField(name, flags);
        if (f != null && typeof(T).IsAssignableFrom(f.FieldType))
        {
            var v = f.GetValue(obj);
            if (v is T tv) return tv;
        }

        // property?
        var p = type.GetProperty(name, flags);
        if (p != null && typeof(T).IsAssignableFrom(p.PropertyType))
        {
            var v = p.GetValue(obj);
            if (v is T tv) return tv;
        }

        return def;
    }
}