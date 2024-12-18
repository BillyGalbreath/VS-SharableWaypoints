﻿using System.Diagnostics.CodeAnalysis;
using SharableWaypoints.Client;
using SharableWaypoints.Server;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace SharableWaypoints;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class SharableWaypointsMod : ModSystem {
    private SharableWaypointsClient? _client;
    private SharableWaypointsServer? _server;

    public override bool ShouldLoad(EnumAppSide side) {
        return true;
    }

    public override void StartClientSide(ICoreClientAPI api) {
        _client = new SharableWaypointsClient(this);
    }

    public override void StartServerSide(ICoreServerAPI api) {
        _server = new SharableWaypointsServer(this);
    }

    public override void Dispose() {
        _client?.Dispose();
        _client = null;

        _server?.Dispose();
        _server = null;
    }
}
