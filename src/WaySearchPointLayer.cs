using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace WaySearchPoint;

internal class WaySearchPointLayer : MapLayer
{
    public override string LayerGroupCode => "way-search-point";
    public override EnumMapAppSide DataSide => EnumMapAppSide.Server;
    private readonly GuiMapSearchDialog _dialog;
    private WorldMapManager _mapSink;
    private ICoreServerAPI _sapi;
    public override string Title => "WaySearchPoint";
    private bool _wasOpened = false;
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
        else if (api.Side == EnumAppSide.Server)
        {
            _sapi = api as ICoreServerAPI;
        }

        GetWaypointLayer();
    }

    private void OnEvery100millis(float obj)
    {
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
            worldMapManager.MapLayers.FirstOrDefault((MapLayer ml) => ml is WaypointMapLayer) as WaypointMapLayer;
        if (_waypointLayer == null)
        {
            throw new ArgumentException("Could not find WaypointMapLayer.");
        }
    }

    public override void ComposeDialogExtras(GuiDialogWorldMap guiDialogWorldMap, GuiComposer compo)
    {
        _dialog.Compose("worldmap-layer-" + LayerGroupCode, guiDialogWorldMap, compo);
    }

    public override void OnLoaded()
    {
        if (_sapi != null)
        {
            FetchWaypoints(_sapi);
        }
    }

    private void FetchWaypoints(ICoreServerAPI sapi)
    {
        if (sapi == null)
        {
            return;
        }

        var waypointsV2 = sapi.WorldManager.SaveGame.GetData("playerMapMarkers_v2");
        if (waypointsV2 != null)
        {
            var waypoints = SerializerUtil.Deserialize<List<Waypoint>>(waypointsV2);
            sapi.World.Logger.Notification("Successfully loaded " + waypoints.Count + " waypoints");
        }
        else
        {
            var waypointsV1 = sapi.WorldManager.SaveGame.GetData("playerMapMarkers");
            if (waypointsV1 != null)
                JsonUtil.FromBytes<List<Waypoint>>(waypointsV1);
        }
    }
}