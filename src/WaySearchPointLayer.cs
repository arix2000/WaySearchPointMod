using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace WaySearchPoint;

internal class WaySearchPointLayer : MapLayer
{
    public override string LayerGroupCode => "way-search-point";
    public override EnumMapAppSide DataSide => EnumMapAppSide.Client;
    private readonly GuiMapSearchDialog _dialog;
    private readonly WorldMapManager _mapSink;
    public override string Title => "WaySearchPoint";
    private bool _wasOpened;
    private WaypointMapLayer _waypointLayer;

    public WaySearchPointLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
        if (api.Side == EnumAppSide.Client)
        {
            _mapSink = mapSink as WorldMapManager;
            _dialog = new GuiMapSearchDialog(api, _mapSink);
            api.Event.RegisterGameTickListener(OnEveryTwoSeconds, 2000);
            api.Event.RegisterGameTickListener(OnEvery100millis, 100);
        }

        GetWaypointLayer();
    }

    private void OnEvery100millis(float obj)
    {
        // Guard against uninitialized map dialog (multiplayer race condition)
        if (_mapSink?.worldMapDlg == null) 
            return;
        
        var isOpened = _mapSink.worldMapDlg.DialogType == EnumDialogType.Dialog;
        if (isOpened != _wasOpened)
        {
            _wasOpened = isOpened;
            _dialog.OnMapToggled();
        }
    }

    private void OnEveryTwoSeconds(float dt)
    {
        base.OnTick(dt);
        var sharedWaypoints = CompatibilityUtils.GetSharedWaypointsIfExists(_mapSink, api);
        var allWaypoints = _waypointLayer.ownWaypoints.Concat(sharedWaypoints).ToList();
        if (allWaypoints.Count != _dialog.WaypointsCount)
        {
            _dialog.SetWaypoints(allWaypoints);
        }
    }

    private void GetWaypointLayer()
    {
        if (mapSink is not WorldMapManager worldMapManager)
        {
            throw new ArgumentException("Map manager is of unexpected type. Expected WorldMapManager.");
        }

        _waypointLayer =
            worldMapManager.MapLayers.FirstOrDefault(ml => ml is WaypointMapLayer) as WaypointMapLayer;
        if (_waypointLayer == null)
        {
            throw new ArgumentException("Could not find WaypointMapLayer.");
        }
    }

    public override void ComposeDialogExtras(GuiDialogWorldMap guiDialogWorldMap, GuiComposer compo)
    {
        _dialog.Compose("worldmap-layer-" + LayerGroupCode, guiDialogWorldMap, compo);
    }
}