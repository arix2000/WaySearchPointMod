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
}