using System;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace SharableWaypoints;

[HarmonyPatch]
public class SharableWaypoints : ModSystem {
    private Harmony harmony;

    public override bool ShouldLoad(EnumAppSide side) {
        return side.IsServer();
    }

    public override void StartServerSide(ICoreServerAPI api) {
        harmony = new Harmony(Mod.Info.ModID);
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    public override void Dispose() {
        harmony?.UnpatchAll(Mod.Info.ModID);
    }

    // entire method was copied from github. only one line was added to make this entire feature work.
    // https://github.com/anegostudios/vsessentialsmod/blob/a9f96aedfee589718ae70c4c0b154aa7ac956429/Systems/WorldMap/WaypointLayer/WaypointMapLayer.cs#L387-L437
    //
    // sent in a PR to get this change into the base game instead of a harmony mod
    // https://github.com/anegostudios/vsessentialsmod/pull/3
    [HarmonyPrefix]
    [HarmonyPatch(typeof(WaypointMapLayer), "AddWp")]
    public static bool AddWp(WaypointMapLayer __instance, Vec3d pos, CmdArgs args, IServerPlayer player, int groupId, string icon, bool pinned) {
        if (args.Length == 0) {
            player.SendMessage(groupId, Lang.Get("command-waypoint-syntax"), EnumChatType.CommandError);
            return false;
        }

        Color parsedColor;
        string colorStr = args.PopWord();
        if (colorStr.StartsWith("#")) {
            try {
                int argb = int.Parse(colorStr.Replace("#", ""), NumberStyles.HexNumber);
                parsedColor = Color.FromArgb(argb);
            }
            catch (FormatException) {
                player.SendMessage(groupId, Lang.Get("command-waypoint-invalidcolor"), EnumChatType.CommandError);
                return false;
            }
        }
        else {
            parsedColor = Color.FromName(colorStr);
        }

        string title = args.PopAll();
        if (string.IsNullOrEmpty(title)) {
            player.SendMessage(groupId, Lang.Get("command-waypoint-notext"), EnumChatType.CommandError);
            return false;
        }

        Waypoint waypoint = new() {
            Color = parsedColor.ToArgb() | (255 << 24),
            OwningPlayerUid = player.PlayerUID,
            OwningPlayerGroupId = groupId, // boom.
            Position = pos,
            Title = title,
            Icon = icon,
            Pinned = pinned,
            Guid = Guid.NewGuid().ToString()
        };

        int nr = __instance.AddWaypoint(waypoint, player);
        player.SendMessage(groupId, Lang.Get("Ok, waypoint nr. {0} added", nr), EnumChatType.CommandSuccess);
        return false;
    }
}
