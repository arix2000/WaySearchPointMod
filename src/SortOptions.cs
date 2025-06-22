using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Config;

namespace WaySearchPoint;

public enum SortOptions
{
    ByDistance,
    Alphabetically
}

public static class SortOptionsExtensions
{
    public static string[] GetAllTranslated()
    {
        return Enum.GetValues(typeof(SortOptions))
            .Cast<SortOptions>()
            .Select(option => option.ToDisplayText())
            .ToArray();
    }

    public static string ToDisplayText(this SortOptions option)
    {
        return option switch
        {
            SortOptions.Alphabetically => Lang.Get("way-search-point-sort-option-0"),
            SortOptions.ByDistance => Lang.Get("way-search-point-sort-option-1"),
            _ => option.ToString()
        };
    }
}