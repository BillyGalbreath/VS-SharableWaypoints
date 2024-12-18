using System.Reflection;
using HarmonyLib;

namespace SharableWaypoints.Common;

public abstract class SharableWaypoints {
    private readonly SharableWaypointsMod _mod;
    private readonly Harmony _harmony;

    protected SharableWaypoints(SharableWaypointsMod mod) {
        _mod = mod;
        _harmony = new Harmony(_mod.Mod.Info.ModID);
    }

    protected void Patch<T>(string original, Delegate? prefix = null, Delegate? postfix = null, Delegate? transpiler = null, Delegate? finalizer = null, Type[]? types = null) {
        if (_harmony == null) {
            throw new InvalidOperationException("Harmony has not been instantiated yet!");
        }

        Patch(AccessTools.Method(typeof(T), original), prefix, postfix, transpiler, finalizer);
    }

    private void Patch(MethodBase? method, Delegate? prefix = null, Delegate? postfix = null, Delegate? transpiler = null, Delegate? finalizer = null) {
        if (_harmony == null) {
            throw new InvalidOperationException("Harmony has not been instantiated yet!");
        }

        if (prefix != null) {
            _harmony.Patch(method, prefix: prefix);
        }

        if (postfix != null) {
            _harmony.Patch(method, postfix: postfix);
        }

        if (transpiler != null) {
            _harmony.Patch(method, transpiler: transpiler);
        }

        if (finalizer != null) {
            _harmony.Patch(method, finalizer: finalizer);
        }
    }

    public void Dispose() {
        _harmony.UnpatchAll(_mod.Mod.Info.ModID);
    }
}
