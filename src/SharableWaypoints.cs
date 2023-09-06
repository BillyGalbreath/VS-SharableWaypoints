using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace SharableWaypoints;

[HarmonyPatch]
public class SharableWaypoints : ModSystem {
    private static Dictionary<string, int> groupCache;

    private Harmony harmony;

    public override bool ShouldLoad(EnumAppSide side) {
        return side.IsServer();
    }

    public override void StartServerSide(ICoreServerAPI api) {
        groupCache = new Dictionary<string, int>();

        harmony = new Harmony(Mod.Info.ModID);
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    public override void Dispose() {
        harmony?.UnpatchAll(Mod.Info.ModID);

        groupCache?.Clear();
        groupCache = null;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(WaypointMapLayer), "AddWp")]
    public static void PreAddWp(WaypointMapLayer __instance, Vec3d pos, CmdArgs args, IServerPlayer player, int groupId, string icon, bool pinned) {
        groupCache.Add(player.PlayerUID, groupId);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(WaypointMapLayer), "AddWp")]
    public static void PostAddWp(WaypointMapLayer __instance, Vec3d pos, CmdArgs args, IServerPlayer player, int groupId, string icon, bool pinned) {
        groupCache.Remove(player.PlayerUID);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(WaypointMapLayer), "AddWaypoint")]
    public static void AddWaypoint(WaypointMapLayer __instance, Waypoint waypoint, IServerPlayer player) {
        waypoint.OwningPlayerGroupId = groupCache.GetValueOrDefault(player.PlayerUID, -1);
    }
}
