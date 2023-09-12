using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace SharableWaypoints.Server;

public class SharableWaypointsServer : Common.SharableWaypoints {
    private static readonly Dictionary<string, int> GROUP_CACHE = new();

    public SharableWaypointsServer(SharableWaypointsMod mod) : base(mod) {
        Harmony.Patch(typeof(WaypointMapLayer).GetMethod("AddWp", BindingFlags.Instance | BindingFlags.NonPublic),
            prefix: typeof(SharableWaypointsServer).GetMethod("PreAddWp"),
            postfix: typeof(SharableWaypointsServer).GetMethod("PostAddWp"));
        Harmony.Patch(typeof(WaypointMapLayer).GetMethod("AddWaypoint", BindingFlags.Instance | BindingFlags.Public),
            postfix: typeof(SharableWaypointsServer).GetMethod("PostAddWaypoint"));
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(WaypointMapLayer), "AddWp")]
    public static void PreAddWp(WaypointMapLayer __instance, Vec3d pos, CmdArgs args, IServerPlayer player, int groupId, string icon, bool pinned) {
        GROUP_CACHE.Add(player.PlayerUID, groupId);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(WaypointMapLayer), "AddWp")]
    public static void PostAddWp(WaypointMapLayer __instance, Vec3d pos, CmdArgs args, IServerPlayer player, int groupId, string icon, bool pinned) {
        GROUP_CACHE.Remove(player.PlayerUID);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(WaypointMapLayer), "AddWaypoint")]
    public static void PostAddWaypoint(WaypointMapLayer __instance, Waypoint waypoint, IServerPlayer player) {
        waypoint.OwningPlayerGroupId = GROUP_CACHE.GetValueOrDefault(player.PlayerUID, -1);
    }
}
