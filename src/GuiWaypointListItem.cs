using System;
using System.Collections.Generic;
using System.Globalization;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace WaySearchPoint;

public class GuiWaypointListItem : IFlatListItem
{
    private LoadedTexture _texture;
    private LoadedTexture _distanceTexture;
    private LoadedTexture _iconTexture;
    private readonly Vec3d _playerPos;
    public Waypoint Waypoint { get; }

    private readonly List<string> _iconCodesToRename =
        new() { "circle", "turnip", "grain", "apple", "berries", "mushroom" };

    public GuiWaypointListItem(Waypoint waypoint, Vec3d playerPos)
    {
        Waypoint = waypoint;
        _playerPos = playerPos;
    }

    private void Recompose(ICoreClientAPI capi)
    {
        _texture?.Dispose();
        _distanceTexture?.Dispose();
        _texture = new TextTextureUtil(capi).GenTextTexture(Waypoint.Title ?? Waypoint.Text,
            CairoFont.WhiteSmallText());

        var distanceText = GetDistanceText();
        _distanceTexture = new TextTextureUtil(capi).GenTextTexture(distanceText, CairoFont.WhiteDetailText());
        LoadIconTexture(capi);
    }

    private string GetDistanceText()
    {
        var distance = Math.Ceiling(WaySearchPointUtils.GetDistanceBetween(_playerPos, Waypoint.Position));
        var distanceUnit = "m";
        if (distance > 2000)
        {
            distance /= 1000;
            distance = double.Round(distance, 1);
            distanceUnit = "km";
        }

        return distance.ToString(CultureInfo.CurrentCulture) + " " + distanceUnit;
    }

    private void LoadIconTexture(ICoreClientAPI capi)
    {
        var iconCode = string.IsNullOrEmpty(Waypoint.Icon) ? "0-circle" : Waypoint.Icon;
        if (_iconCodesToRename.Contains(iconCode))
        {
            iconCode = MapIconName(iconCode);
        }

        var svgPath =
            new AssetLocation("game", $"textures/icons/worldmap/{iconCode}.svg");

        if (!capi.Assets.Exists(svgPath))
        {
            svgPath = CompatibilityUtils.TryToGetFromOthersMods(capi, iconCode);
        }

        _iconTexture?.Dispose();
        _iconTexture = capi.Assets.Exists(svgPath)
            ? capi.Gui.LoadSvgWithPadding(svgPath, 24, 24, color: Waypoint.Color)
            : new LoadedTexture(capi);
    }

    private string MapIconName(string word)
    {
        var index = _iconCodesToRename.IndexOf(word);
        if (index == -1)
            return null;

        var prefix = index == 0 ? "0" : index.ToString("D2");
        return $"{prefix}-{word}";
    }

    public void RenderListEntryTo(ICoreClientAPI capi, float dt,
        double x, double y,
        double cellWidth, double cellHeight)
    {
        if (_texture == null || _iconTexture == null) Recompose(capi);

        var distX = x + cellWidth - _distanceTexture.Width - 8;
        capi.Render.Render2DTexturePremultipliedAlpha(
            _distanceTexture.TextureId,
            (float)distX, (float)(y + 2),
            _distanceTexture.Width, _distanceTexture.Height);

        RenderWaypointTitle(capi, dt, x, y, cellHeight, distX);

        capi.Render.Render2DTexturePremultipliedAlpha(
            _iconTexture.TextureId,
            (float)(x + 10), (float)y,
            24, 24);
    }

    private void RenderWaypointTitle(ICoreClientAPI capi, float dt,
        double x, double y, double cellHeight, double distX)
    {
        var scale = RuntimeEnv.GUIScale;

        var unscaledX = (x + 42) / scale;
        var unscaledY = y / scale;
        var unscaledW = (distX - (x + 42) - 8) / scale;
        var unscaledH = cellHeight / scale;

        var parent = capi.Gui.WindowBounds;

        var scissor = ElementBounds.Fixed(unscaledX, unscaledY, unscaledW, unscaledH);
        scissor.ParentBounds = parent;
        scissor.CalcWorldBounds();

        capi.Render.PushScissor(scissor, true);
        capi.Render.Render2DTexturePremultipliedAlpha(
            _texture.TextureId,
            (float)(x + 42), (float)(y + 2),
            _texture.Width, _texture.Height);
        capi.Render.PopScissor();
    }

    public void Dispose()
    {
        _texture?.Dispose();
        _iconTexture?.Dispose();
        _distanceTexture?.Dispose();
    }

    public bool Visible { get; set; } = true;
}