using SharableWaypoints.Client;
using SharableWaypoints.Server;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace SharableWaypoints;

public class SharableWaypointsMod : ModSystem {
    private SharableWaypointsClient? client;
    private SharableWaypointsServer? server;

    public override bool AllowRuntimeReload => true;

    public override bool ShouldLoad(EnumAppSide side) {
        return true;
    }

    public override void StartClientSide(ICoreClientAPI api) {
        client = new SharableWaypointsClient(this);
    }

    public override void StartServerSide(ICoreServerAPI api) {
        server = new SharableWaypointsServer(this);
    }

    public override void Dispose() {
        client?.Dispose();
        client = null;

        server?.Dispose();
        server = null;
    }
}
