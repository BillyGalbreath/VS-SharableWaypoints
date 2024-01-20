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
public abstract class Server119 {
    private static readonly Dictionary<string, int> GroupCache = new();

    public static bool TryPatch(Harmony harmony) {
        try {
            MethodInfo? preOnCmdWaypoint = typeof(Server118).GetMethod("PreOnCmdWayPoint");
            MethodInfo? postOnCmdWaypoint = typeof(Server118).GetMethod("PostOnCmdWayPoint");

            harmony.Patch(typeof(WaypointMapLayer).GetMethod("OnCmdWayPointAdd", BindingFlags.Instance | BindingFlags.NonPublic, new[] { typeof(IServerPlayer), typeof(int), typeof(CmdArgs) }), prefix: preOnCmdWaypoint, postfix: postOnCmdWaypoint);
            harmony.Patch(typeof(WaypointMapLayer).GetMethod("OnCmdWayPointAddp", BindingFlags.Instance | BindingFlags.NonPublic, new[] { typeof(IServerPlayer), typeof(int), typeof(CmdArgs) }), prefix: preOnCmdWaypoint, postfix: postOnCmdWaypoint);
            harmony.Patch(typeof(WaypointMapLayer).GetMethod("OnCmdWayPointAddat", BindingFlags.Instance | BindingFlags.NonPublic, new[] { typeof(IServerPlayer), typeof(int), typeof(CmdArgs) }), prefix: preOnCmdWaypoint, postfix: postOnCmdWaypoint);
            harmony.Patch(typeof(WaypointMapLayer).GetMethod("OnCmdWayPointAddati", BindingFlags.Instance | BindingFlags.NonPublic, new[] { typeof(IServerPlayer), typeof(int), typeof(CmdArgs) }), prefix: preOnCmdWaypoint, postfix: postOnCmdWaypoint);

            harmony.Patch(typeof(WaypointMapLayer).GetMethod("AddWaypoint", BindingFlags.Instance | BindingFlags.Public, new[] { typeof(Waypoint), typeof(IServerPlayer) }),
                postfix: typeof(Server118).GetMethod("PostAddWaypoint"));
            return true;
        } catch (Exception) {
            return false;
        }
    }

    public static void PreOnCmdWayPoint(TextCommandCallingArgs args) {
        GroupCache.Add(args.Caller.Player.PlayerUID, args.Caller.FromChatGroupId);
    }

    public static void PostOnCmdWayPoint(TextCommandCallingArgs args) {
        GroupCache.Remove(args.Caller.Player.PlayerUID);
    }

    public static void PostAddWaypoint(Waypoint waypoint, IServerPlayer player) {
        waypoint.OwningPlayerGroupId = GroupCache.GetValueOrDefault(player.PlayerUID, waypoint.OwningPlayerGroupId);
    }
}
