using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace SharableWaypoints.Server;

public class SharableWaypointsServer : Common.SharableWaypoints {
    private static readonly Dictionary<string, int> _groupCache = new();

    public SharableWaypointsServer(SharableWaypointsMod mod) : base(mod) {
        Patch<WaypointMapLayer>("OnCmdWayPointAdd", prefix: PreOnCmdWaypoint, postfix: PostOnCmdWaypoint, types: new[] { typeof(TextCommandCallingArgs) });
        Patch<WaypointMapLayer>("OnCmdWayPointAddp", prefix: PreOnCmdWaypoint, postfix: PostOnCmdWaypoint, types: new[] { typeof(TextCommandCallingArgs) });
        Patch<WaypointMapLayer>("OnCmdWayPointAddat", prefix: PreOnCmdWaypoint, postfix: PostOnCmdWaypoint, types: new[] { typeof(TextCommandCallingArgs) });
        Patch<WaypointMapLayer>("OnCmdWayPointAddati", prefix: PreOnCmdWaypoint, postfix: PostOnCmdWaypoint, types: new[] { typeof(TextCommandCallingArgs) });
        Patch<WaypointMapLayer>("AddWaypoint", postfix: PostAddWaypoint, types: new[] { typeof(Waypoint), typeof(IServerPlayer) });
    }

    private static void PreOnCmdWaypoint(TextCommandCallingArgs args) {
        _groupCache.Add(args.Caller.Player.PlayerUID, args.Caller.FromChatGroupId);
    }

    private static void PostOnCmdWaypoint(TextCommandCallingArgs args) {
        _groupCache.Remove(args.Caller.Player.PlayerUID);
    }

    private static void PostAddWaypoint(Waypoint waypoint, IServerPlayer player) {
        waypoint.OwningPlayerGroupId = _groupCache.GetValueOrDefault(player.PlayerUID, waypoint.OwningPlayerGroupId);
    }
}
