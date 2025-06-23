using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace WaySearchPoint;

public class WaySearchPointModSystem : ModSystem
{
    private const string Name = "WaySearchPoint";

    public override void StartClientSide(ICoreClientAPI api)
    {
        var mapManager = api.ModLoader.GetModSystem<WorldMapManager>();
        mapManager.RegisterMapLayer<WaySearchPointLayer>(Name, 0.01);
    }
}