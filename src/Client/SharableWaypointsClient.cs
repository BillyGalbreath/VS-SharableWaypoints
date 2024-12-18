using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace SharableWaypoints.Client;

public class SharableWaypointsClient : Common.SharableWaypoints {
    public SharableWaypointsClient(SharableWaypointsMod mod) : base(mod) {
        Patch<WaypointMapLayer>("OnDataFromServer", prefix: PreOnDataFromServer);
        Patch<GuiDialogAddWayPoint>("autoSuggestName", prefix: PreAutoSuggestName);
        Patch<GuiDialogAddWayPoint>("onSave", postfix: PostAddOnSave);
        Patch<GuiDialogEditWayPoint>("TryOpen", prefix: PreEditTryOpen, types: Array.Empty<Type>());
        Patch<GuiDialogEditWayPoint>("onDelete", prefix: PreEditOnDelete);
        Patch<GuiDialogEditWayPoint>("onSave", prefix: PreEditOnSave);
    }

    private static bool PreOnDataFromServer(WaypointMapLayer __instance, byte[] data, ref ICoreAPI ___api, ref List<MapComponent> ___tmpWayPointComponents) {
        __instance.ownWaypoints.Clear();
        ___tmpWayPointComponents.Clear();

        string? uuid = ((ICoreClientAPI)___api).World.Player.PlayerUID;

        foreach (Waypoint waypoint in SerializerUtil.Deserialize<List<Waypoint>>(data)) {
            if (waypoint.OwningPlayerUid == uuid) {
                __instance.ownWaypoints.Add(waypoint);
            } else {
                __instance.AddTemporaryWaypoint(waypoint);
            }
        }

        AccessTools.Method(__instance.GetType(), "RebuildMapComponents")?.Invoke(__instance, null);

        return false;
    }

    private static bool PreAutoSuggestName(GuiDialogAddWayPoint __instance, ref string ___curIcon, ref string ___curColor, ref bool ___ignoreNextAutosuggestDisable) {
        string? savedName = Settings.GetWaypointName($"{___curIcon}-{___curColor}");
        if (string.IsNullOrEmpty(savedName)) {
            return true;
        }

        GuiElementTextInput textInput = __instance.SingleComposer.GetTextInput("nameInput");
        ___ignoreNextAutosuggestDisable = true;
        textInput.SetValue(savedName);
        return false;
    }

    private static void PostAddOnSave(GuiDialogAddWayPoint __instance, ref string ___curIcon, ref string ___curColor) {
        string curName = __instance.SingleComposer.GetTextInput("nameInput").GetText();
        Settings.SetWaypointName($"{___curIcon}-{___curColor}", curName);
    }

    private static bool PreEditTryOpen(GuiDialogEditWayPoint __instance, ref bool __result, ref ICoreClientAPI ___capi, ref Waypoint ___waypoint) {
        if (___waypoint.OwningPlayerUid == ___capi.World.Player.PlayerUID) {
            return true;
        }

        ___capi.ShowChatMessage(Lang.Get("sharablewaypoints:cannot-edit"));
        ___capi.Event.RegisterCallback(_ => { __instance.TryClose(); }, 1);

        __result = false;
        return false;
    }

    private static bool PreEditOnDelete(ref bool __result, ref ICoreClientAPI ___capi, ref Waypoint ___waypoint) {
        if (___waypoint.OwningPlayerUid == ___capi.World.Player.PlayerUID) {
            return true;
        }

        ___capi.ShowChatMessage(Lang.Get("sharablewaypoints:cannot-delete"));

        __result = true;
        return false;
    }

    private static bool PreEditOnSave(ref bool __result, ref ICoreClientAPI ___capi, ref Waypoint ___waypoint) {
        if (___waypoint.OwningPlayerUid == ___capi.World.Player.PlayerUID) {
            return true;
        }

        ___capi.ShowChatMessage(Lang.Get("sharablewaypoints:cannot-save"));

        __result = true;
        return false;
    }
}
