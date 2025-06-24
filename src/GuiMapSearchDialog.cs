using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace WaySearchPoint;

using Vintagestory.API.Client;
using Vintagestory.API.Config;

public class GuiMapSearchDialog : GuiDialog
{
    private readonly ICoreClientAPI _capi;
    private readonly WorldMapManager _worldMapManager;

    private List<Waypoint> _waypoints = new();
    private readonly List<IFlatListItem> _filteredWaypoints = new();
    private const double ListHeight = 440.0;
    private ElementBounds _outerBounds;
    private bool _isScrollEnabled;
    private bool _mouseWasInside;
    private float _scrollbarYPosition;
    private SortOptions _selectedSortOption = SortOptions.ByDistance;
    private string _currentText = string.Empty;

    public int WaypointsCount => _waypoints.Count;


    public GuiMapSearchDialog(ICoreAPI api, WorldMapManager mapSink) : base(api as ICoreClientAPI)
    {
        _capi = api as ICoreClientAPI;
        if (_capi != null) _capi.Event.MouseMove += OnGlobalMouseMove;
        _worldMapManager = mapSink;
    }

    public override string ToggleKeyCombinationCode => "way-search-point-dialog";

    public void Compose(string key, GuiDialogWorldMap guiDialogWorldMap, GuiComposer compo)
    {
        _outerBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftBottom);
        var backgroundBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        var dialogContainerBounds = ElementBounds.Fixed(0, 40, 300, 500);
        backgroundBounds.BothSizing = ElementSizing.FitToChildren;
        backgroundBounds.WithChildren(dialogContainerBounds);
        var inputBounds = ElementBounds.Fixed(GuiStyle.ElementToDialogPadding, 45.0, 300.0, 30.0);
        var sortTextBounds = ElementBounds.Fixed(GuiStyle.ElementToDialogPadding, 85.0, 50.0, 20.0);
        var sortDropDownBounds = ElementBounds.Fixed(GuiStyle.ElementToDialogPadding + 50.0, 80.0, 250.0, 30.0);
        var listBounds = ElementBounds.Fixed(0, 95, 275, ListHeight);
        var scrollbarBounds = listBounds.CopyOffsetedSibling(listBounds.fixedWidth + 5.0).WithFixedWidth(20.0);
        var parentBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        parentBounds.BothSizing = ElementSizing.FitToChildren;
        parentBounds.WithChildren(listBounds, listBounds, scrollbarBounds);
        SingleComposer = _capi.Gui.CreateCompo("WaypointSearch", _outerBounds)
            .AddShadedDialogBG(backgroundBounds)
            .AddDialogTitleBar(text: Lang.Get("way-search-point-dialog-title"),
                () => OnTitleBarClose(key, guiDialogWorldMap))
            .AddTextInput(inputBounds, text => OnTextChanged(text), key: "searchinput")
            .AddStaticText(bounds: sortTextBounds, text: Lang.Get("way-search-point-sort"),
                font: CairoFont.WhiteSmallText())
            .AddDropDown(bounds: sortDropDownBounds, values: Enum.GetNames(typeof(SortOptions)),
                names: SortOptionsExtensions.GetAllTranslated(),
                selectedIndex: Array.IndexOf(Enum.GetValues(typeof(SortOptions)), _selectedSortOption),
                onSelectionChanged: OnSortOptionSelected)
            .BeginChildElements(parentBounds)
            .BeginClip(listBounds.ForkBoundingParent())
            .AddInset(listBounds)
            .AddFlatList(listBounds, key: "flatlist", stacks: _filteredWaypoints, onleftClick: OnItemClick)
            .EndClip()
            .AddVerticalScrollbar(OnNewScrollbarValue, scrollbarBounds, "scrollbar")
            .EndChildElements()
            .Compose();

        UpdateScrollbar();
        SingleComposer.GetTextInput("searchinput").SetPlaceHolderText(Lang.Get("Search..."));
        SingleComposer.UnfocusOwnElements();
        SingleComposer.GetScrollbar("scrollbar").SetHeights((float)ListHeight, (float)ListHeight);

        guiDialogWorldMap.Composers[key] = SingleComposer;
    }

    private void OnSortOptionSelected(string code, bool selected)
    {
        SortOptions selectedOption = (SortOptions)Enum.Parse(typeof(SortOptions), code);
        _selectedSortOption = selectedOption;
        OnTextChanged(_currentText);
    }

    private void OnItemClick(int index)
    {
        GuiWaypointListItem guiListItem = (GuiWaypointListItem)_filteredWaypoints[index];
        Waypoint item = guiListItem.Waypoint;
        if (_worldMapManager.worldMapDlg.SingleComposer.GetElement("mapElem") is not GuiElementMap mapView)
        {
            capi.Logger.Error("WaySearchPoint - It seems that map is not loaded");
            return;
        }

        mapView.CenterMapTo(item.Position.AsBlockPos);
    }

    private void OnTitleBarClose(string key, GuiDialogWorldMap guiDialogWorldMap) =>
        guiDialogWorldMap.Composers[key].Enabled = false;

    private void OnNewScrollbarValue(float value)
    {
        if (!_isScrollEnabled) return;

        GuiElementFlatList list = SingleComposer.GetFlatList("flatlist");
        list.insideBounds.fixedY = 5 - value;
        list.insideBounds.CalcWorldBounds();
    }

    private void UpdateScrollbar()
    {
        var list = SingleComposer.GetFlatList("flatlist");
        list.CalcTotalHeight();

        if (!_isScrollEnabled) return;
        var scrollbar = SingleComposer.GetScrollbar("scrollbar");
        scrollbar.SetHeights((float)ListHeight, (float)list.insideBounds.fixedHeight);
    }

    private void ResetUpdateScrollbarToTop(bool shouldResetScroll)
    {
        if (shouldResetScroll)
        {
            SingleComposer.GetFlatList("flatlist").insideBounds.fixedY = 5;
            SingleComposer.GetScrollbar("scrollbar").CurrentYPosition = 0;
        }

        UpdateScrollbar();
    }

    private void OnTextChanged(string text, bool shouldResetScroll = true)
    {
        var playerPosition = capi.World.Player.Entity.Pos.XYZ;

        _currentText = text.Trim();
        _filteredWaypoints.Clear();
        if (string.IsNullOrWhiteSpace(_currentText))
        {
            SortSetWaypoints();
            ResetUpdateScrollbarToTop(shouldResetScroll);
            return;
        }

        var matches =
            WaySearchPointUtils.GetSortedMatches(_currentText, _waypoints, _selectedSortOption, playerPosition);

        var playerPos = capi.World.Player.Entity.Pos.XYZ;
        foreach (var waypoint in matches)
        {
            _filteredWaypoints.Add(new GuiWaypointListItem(waypoint, playerPos));
        }

        ResetUpdateScrollbarToTop(shouldResetScroll);
    }

    public void SetWaypoints(List<Waypoint> waypoints)
    {
        _waypoints.Clear();
        _filteredWaypoints.Clear();
        _waypoints = new List<Waypoint>(waypoints);
        if (!string.IsNullOrWhiteSpace(_currentText))
        {
            OnTextChanged(_currentText, false);
        }
        else
        {
            SortSetWaypoints();
            UpdateScrollbar();
        }
    }

    private void SortSetWaypoints()
    {
        var playerPos = capi.World.Player.Entity.Pos.XYZ;
        var sortedWaypoints =
            WaySearchPointUtils.GetSorted(_waypoints, _selectedSortOption, playerPos);
        sortedWaypoints.ToList().ForEach(waypoint =>
            _filteredWaypoints.Add(new GuiWaypointListItem(waypoint, playerPos)));
    }

    private void OnGlobalMouseMove(MouseEvent args)
    {
        _outerBounds?.CalcWorldBounds();
        var mxGui = (int)GuiElement.scaled(args.X);
        var myGui = (int)GuiElement.scaled(args.Y);

        var isNowInside = _outerBounds?.PointInside(mxGui, myGui) ?? false;
        if (isNowInside == _mouseWasInside) return;
        _mouseWasInside = isNowInside;
        if (isNowInside)
        {
            _isScrollEnabled = true;
            UpdateScrollbar();
            SingleComposer.GetScrollbar("scrollbar").CurrentYPosition = _scrollbarYPosition;
            OnNewScrollbarValue(_scrollbarYPosition);
        }
        else
        {
            _isScrollEnabled = false;
            var scrollbar = SingleComposer.GetScrollbar("scrollbar");
            _scrollbarYPosition = scrollbar.CurrentYPosition;
            scrollbar.SetHeights((float)ListHeight, (float)ListHeight);
        }
    }

    public void OnMapToggled()
    {
        _scrollbarYPosition = 0;
        _filteredWaypoints.Clear();
        SortSetWaypoints();
        _currentText = string.Empty;
        ResetUpdateScrollbarToTop(true);
    }

    public override void Dispose()
    {
        SingleComposer.Dispose();
        capi.Event.MouseMove -= OnGlobalMouseMove;
        base.Dispose();
    }
}