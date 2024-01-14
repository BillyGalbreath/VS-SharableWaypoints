using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace SharableWaypoints.Patches;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public abstract class Server118 {
    private static readonly Dictionary<string, int> GroupCache = new();

    public static bool TryPatch(Harmony harmony) {
        try {
            harmony.Patch(typeof(WaypointMapLayer).GetMethod("OnCmdWayPoint", BindingFlags.Instance | BindingFlags.NonPublic, new[] { typeof(IServerPlayer), typeof(int), typeof(CmdArgs) }),
                prefix: typeof(Server118).GetMethod("PreOnCmdWayPoint"),
                postfix: typeof(Server118).GetMethod("PostOnCmdWayPoint"));
            harmony.Patch(typeof(WaypointMapLayer).GetMethod("AddWaypoint", BindingFlags.Instance | BindingFlags.Public, new[] { typeof(Waypoint), typeof(IServerPlayer) }),
                postfix: typeof(Server118).GetMethod("PostAddWaypoint"));
            return true;
        } catch (Exception) {
            return false;
        }
    }

    public static void PreOnCmdWayPoint(IServerPlayer player, int groupId) {
        GroupCache.Add(player.PlayerUID, groupId);
    }

    public static void PostOnCmdWayPoint(IServerPlayer player) {
        GroupCache.Remove(player.PlayerUID);
    }

    public static void PostAddWaypoint(Waypoint waypoint, IServerPlayer player) {
        waypoint.OwningPlayerGroupId = GroupCache.GetValueOrDefault(player.PlayerUID, -1);
    }
}
