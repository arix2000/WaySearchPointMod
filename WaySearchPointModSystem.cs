using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace WaySearchPoint;

public class WaySearchPointModSystem : ModSystem
{
    private const string Name = "WaySearchPoint";

    public override void Start(ICoreAPI api)
    {
        var mapManager = api.ModLoader.GetModSystem<WorldMapManager>();
        mapManager.RegisterMapLayer<WaySearchPointLayer>(Name, 0.01);
    }
}