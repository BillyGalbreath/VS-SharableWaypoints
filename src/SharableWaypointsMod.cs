using Vintagestory.API.Common;

namespace SharableWaypoints;

public class SharableWaypointsModSystem : ModSystem {
    public override bool ShouldLoad(EnumAppSide side) {
        return side.IsClient();
    }
}
