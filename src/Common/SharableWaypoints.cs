using HarmonyLib;

namespace SharableWaypoints.Common;

public abstract class SharableWaypoints {
    private readonly SharableWaypointsMod mod;

    protected Harmony Harmony { get; }

    protected SharableWaypoints(SharableWaypointsMod mod) {
        this.mod = mod;

        Harmony = new Harmony(mod.Mod.Info.ModID);
    }

    public void Dispose() {
        Harmony.UnpatchAll(mod.Mod.Info.ModID);
    }
}
