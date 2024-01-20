using SharableWaypoints.Patches;
using Vintagestory.API.Common;

namespace SharableWaypoints.Server;

public class SharableWaypointsServer : Common.SharableWaypoints {
    public SharableWaypointsServer(ModSystem mod) : base(mod.Mod.Info.ModID) {
        if (Server118.TryPatch(Harmony)) {
            mod.Mod.Logger.Event("Successfully applied patches 1.18 server.");
        } else if (Server119.TryPatch(Harmony)) {
            mod.Mod.Logger.Event("Successfully applied patches 1.19 server.");
        } else {
            mod.Mod.Logger.Event("Unable to apply patches to server.");
        }
    }
}
