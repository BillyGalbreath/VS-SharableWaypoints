using HarmonyLib;

namespace SharableWaypoints.Common;

public abstract class SharableWaypoints {
    private readonly string _modId;

    protected Harmony Harmony { get; }

    protected SharableWaypoints(string modId) {
        _modId = modId;
        Harmony = new Harmony(modId);
    }

    public void Dispose() {
        Harmony.UnpatchAll(_modId);
    }
}
