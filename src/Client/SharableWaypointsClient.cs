using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SharableWaypoints.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace SharableWaypoints.Client;

[HarmonyPatch]
public class SharableWaypointsClient : Common.SharableWaypoints {
    public SharableWaypointsClient(SharableWaypointsMod mod) : base(mod) {
        Harmony.Patch(typeof(WaypointMapLayer).GetMethod("OnDataFromServer", BindingFlags.Instance | BindingFlags.Public),
            prefix: typeof(SharableWaypointsClient).GetMethod("PreOnDataFromServer"));
        Harmony.Patch(typeof(GuiDialogEditWayPoint).GetMethod("TryOpen", BindingFlags.Instance | BindingFlags.Public),
            prefix: typeof(SharableWaypointsClient).GetMethod("PreTryOpen"));
        Harmony.Patch(typeof(GuiDialogEditWayPoint).GetMethod("onDelete", BindingFlags.Instance | BindingFlags.NonPublic),
            prefix: typeof(SharableWaypointsClient).GetMethod("PreOnDelete"));
        Harmony.Patch(typeof(GuiDialogEditWayPoint).GetMethod("onSave", BindingFlags.Instance | BindingFlags.NonPublic),
            prefix: typeof(SharableWaypointsClient).GetMethod("PreOnSave"));
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(WaypointMapLayer), "OnDataFromServer")]
    public static bool PreOnDataFromServer(WaypointMapLayer __instance, byte[] data) {
        __instance.ownWaypoints.Clear();
        __instance.GetField<List<MapComponent>>("tmpWayPointComponents")?.Clear();

        string? uuid = ((ICoreClientAPI)__instance.GetField<ICoreAPI>("api")!).World.Player.PlayerUID;

        foreach (Waypoint waypoint in SerializerUtil.Deserialize<List<Waypoint>>(data)) {
            if (waypoint.OwningPlayerUid == uuid) {
                __instance.ownWaypoints.Add(waypoint);
            }
            else {
                __instance.AddTemporaryWaypoint(waypoint);
            }
        }

        __instance.Invoke("RebuildMapComponents");

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GuiDialogEditWayPoint), "TryOpen")]
    public static bool PreTryOpen(GuiDialogEditWayPoint __instance, bool __result) {
        ICoreClientAPI capi = (ICoreClientAPI)__instance.GetField<ICoreAPI>("capi")!;
        if (__instance.GetField<Waypoint>("waypoint")?.OwningPlayerUid == capi.World.Player.PlayerUID) {
            return true;
        }

        capi.ShowChatMessage("Cannot edit waypoints you do not own!");
        capi.Event.RegisterCallback(_ => { __instance.TryClose(); }, 1);

        __result = false;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GuiDialogEditWayPoint), "onDelete")]
    public static bool PreOnDelete(GuiDialogEditWayPoint __instance, bool __result) {
        ICoreClientAPI capi = (ICoreClientAPI)__instance.GetField<ICoreAPI>("capi")!;
        if (__instance.GetField<Waypoint>("waypoint")?.OwningPlayerUid == capi.World.Player.PlayerUID) {
            return true;
        }

        capi.ShowChatMessage("Cannot delete waypoints you do not own!");

        __result = true;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GuiDialogEditWayPoint), "onSave")]
    public static bool PreOnSave(GuiDialogEditWayPoint __instance, bool __result) {
        ICoreClientAPI capi = (ICoreClientAPI)__instance.GetField<ICoreAPI>("capi")!;
        if (__instance.GetField<Waypoint>("waypoint")?.OwningPlayerUid == capi.World.Player.PlayerUID) {
            return true;
        }

        capi.ShowChatMessage("Cannot edit waypoints you do not own!");

        __result = true;
        return false;
    }
}
