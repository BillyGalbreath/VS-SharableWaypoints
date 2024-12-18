using System.Text;
using System.Text.RegularExpressions;
using Cairo;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Color = System.Drawing.Color;

namespace SharableWaypoints.Client.Gui;

[ProtoContract]
public class SharableWaypoint {
    [ProtoMember(6)] public Vec3d Position = new Vec3d();
    [ProtoMember(10)] public string Title;
    [ProtoMember(9)] public string Text;
    [ProtoMember(1)] public int Color;
    [ProtoMember(2)] public string Icon = "circle";
    [ProtoMember(7)] public bool ShowInWorld;
    [ProtoMember(5)] public bool Pinned;

    [ProtoMember(4)] public string OwningPlayerUid = null;
    [ProtoMember(3)] public int OwningPlayerGroupId = -1;

    [ProtoMember(8)] public bool Temporary;

    [ProtoMember(11)] public string Guid { get; set; }
}

public delegate LoadedTexture CreateIconTextureDelegate();

public class SharableWaypointMapLayer : MarkerMapLayer {
    // Server side
    public List<SharableWaypoint> Waypoints = new List<SharableWaypoint>();

    private readonly ICoreClientAPI? _capi;
    private readonly ICoreServerAPI? _sapi;

    // Client side
    public List<SharableWaypoint> ownWaypoints = new List<SharableWaypoint>();
    List<MapComponent> wayPointComponents = new List<MapComponent>();
    public MeshRef quadModel;

    List<MapComponent> tmpWayPointComponents = new List<MapComponent>();

    public Dictionary<string, LoadedTexture> texturesByIcon;

    public override bool RequireChunkLoaded => false;

    public override string Title => "Player Set Markers";
    public override EnumMapAppSide DataSide => EnumMapAppSide.Server;

    public override string LayerGroupCode => "waypoints";

    /// <summary>
    /// List
    /// </summary>
    public OrderedDictionary<string, CreateIconTextureDelegate?> Icons { get; set; } = new();

    static string[] hexcolors = new string[] {
        "#F9D0DC", "#F179AF", "#F15A4A", "#ED272A", "#A30A35", "#FFDE98", "#EFFD5F", "#F6EA5E", "#FDBB3A", "#C8772E", "#F47832",
        "C3D941", "#9FAB3A", "#94C948", "#47B749", "#366E4F", "#516D66", "93D7E3", "#7698CF", "#20909E", "#14A4DD", "#204EA2",
        "#28417A", "#C395C4", "#92479B", "#8E007E", "#5E3896", "D9D4CE", "#AFAAA8", "#706D64", "#4F4C2B", "#BF9C86", "#9885530", "#5D3D21", "#FFFFFF", "#080504"
    };

    public List<int> Colors { get; }

    public SharableWaypointMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink) {
        Colors = new List<int>();
        foreach (string hex in hexcolors) {
            Colors.Add(ColorUtil.Hex2Int(hex));
        }

        _capi = api as ICoreClientAPI;
        _sapi = api as ICoreServerAPI;

        List<IAsset>? icons = api.Assets.GetMany("textures/icons/worldmap/", null, false);
        foreach (IAsset icon in icons) {
            string name = icon.Name[..icon.Name.IndexOf('.')];

            name = Regex.Replace(name, @"\d+\-", "");

            if (api.Side == EnumAppSide.Server) {
                Icons[name] = () => null!;
            } else {
                Icons[name] = () => {
                    int size = (int)Math.Ceiling(20 * RuntimeEnv.GUIScale);
                    return _capi!.Gui.LoadSvg(icon.Location, size, size, size, size, ColorUtil.WhiteArgb);
                };

                _capi!.Gui.Icons.CustomIcons["wp" + name.UcFirst()] = (ctx, x, y, w, h, rgba) => {
                    int col = ColorUtil.ColorFromRgba(rgba);
                    _capi.Gui.DrawSvg(icon, ctx.GetTarget() as ImageSurface, ctx.Matrix, x, y, (int)w, (int)h, col);
                };
            }
        }

        if (api.Side == EnumAppSide.Server) {
            _sapi!.Event.GameWorldSave += OnSaveGameGettingSaved;
            _sapi.Event.PlayerDeath += Event_PlayerDeath;

            CommandArgumentParsers? parsers = _sapi.ChatCommands.Parsers;
            _sapi.ChatCommands.Create("waypoint")
                .WithDescription("Put a waypoint at this location which will be visible for you on the map")
                .RequiresPrivilege(Privilege.chat)
                .BeginSubCommand("deathwp")
                .WithDescription("Enable/Disable automatic adding of a death waypoint")
                .WithArgs(parsers.OptionalBool("enabled"))
                .RequiresPlayer()
                .HandleWith(OnCmdWayPointDeathWp)
                .EndSubCommand()
                .BeginSubCommand("add")
                .WithDescription("Add a waypoint to the map")
                .RequiresPlayer()
                .WithArgs(parsers.Color("color"), parsers.All("title"))
                .HandleWith(OnCmdWayPointAdd)
                .EndSubCommand()
                .BeginSubCommand("addp")
                .RequiresPlayer()
                .WithDescription("Add a waypoint to the map")
                .WithArgs(parsers.Color("color"), parsers.All("title"))
                .HandleWith(OnCmdWayPointAddp)
                .EndSubCommand()
                .BeginSubCommand("addat")
                .WithDescription("Add a waypoint to the map")
                .RequiresPlayer()
                .WithArgs(parsers.WorldPosition("position"), parsers.Bool("pinned"), parsers.Color("color"), parsers.All("title"))
                .HandleWith(OnCmdWayPointAddat)
                .EndSubCommand()
                .BeginSubCommand("addati")
                .WithDescription("Add a waypoint to the map")
                .RequiresPlayer()
                .WithArgs(parsers.Word("icon"), parsers.WorldPosition("position"), parsers.Bool("pinned"), parsers.Color("color"), parsers.All("title"))
                .HandleWith(OnCmdWayPointAddati)
                .EndSubCommand()
                .BeginSubCommand("modify")
                .WithDescription("")
                .RequiresPlayer()
                .WithArgs(parsers.Int("waypoint_id"), parsers.Color("color"), parsers.Word("icon"), parsers.Bool("pinned"), parsers.All("title"))
                .HandleWith(OnCmdWayPointModify)
                .EndSubCommand()
                .BeginSubCommand("remove")
                .WithDescription("Remove a waypoint by its id. Get a lost of ids using /waypoint list")
                .RequiresPlayer()
                .WithArgs(parsers.Int("waypoint_id"))
                .HandleWith(OnCmdWayPointRemove)
                .EndSubCommand()
                .BeginSubCommand("list")
                .WithDescription("List your own waypoints")
                .RequiresPlayer()
                .WithArgs(parsers.OptionalWordRange("details", "details", "d"))
                .HandleWith(OnCmdWayPointList)
                .EndSubCommand()
                ;

            _sapi.ChatCommands.Create("tpwp")
                .WithDescription("Teleport yourself to a waypoint starting with the supplied name")
                .RequiresPrivilege(Privilege.tp)
                .WithArgs(parsers.All("name"))
                .HandleWith(OnCmdTpTo)
                ;
        } else {
            quadModel = _capi!.Render.UploadMesh(QuadMeshUtil.GetQuad());
        }
    }

    private bool IsMapDisallowed(out TextCommandResult response) {
        if (!api.World.Config.GetBool("allowMap", true)) {
            response = TextCommandResult.Success(Lang.Get("Maps are disabled on this server"));
            return true;
        }

        response = null!;
        return false;
    }

    private TextCommandResult OnCmdWayPointList(TextCommandCallingArgs args) {
        if (IsMapDisallowed(out TextCommandResult textCommandResult)) {
            return textCommandResult;
        }

        bool detailed = args[0] as string == "details" || args[0] as string == "d";
        StringBuilder wps = new();
        int fauxId = 0;
        foreach (SharableWaypoint sharableWaypoint in Waypoints.Where(waypoint => waypoint.OwningPlayerUid == args.Caller.Player.PlayerUID).ToArray()) {
            Vec3d? pos = sharableWaypoint.Position.Clone();
            pos.X -= api.World.DefaultSpawnPosition.X;
            pos.Z -= api.World.DefaultSpawnPosition.Z;

            string line = $"{fauxId}: {sharableWaypoint.Title} at {pos.AsBlockPos}";
            if (detailed) {
                line += $" {ColorUtil.Int2Hex(sharableWaypoint.Color)} {sharableWaypoint.Icon}";
            }

            wps.AppendLine(line);

            fauxId++;
        }

        return TextCommandResult.Success(Lang.Get(wps.Length == 0 ? "You have no waypoints" : "Your waypoints:" + "\n" + wps));
    }

    private TextCommandResult OnCmdWayPointRemove(TextCommandCallingArgs args) {
        if (IsMapDisallowed(out TextCommandResult textCommandResult)) {
            return textCommandResult;
        }

        IServerPlayer player = (IServerPlayer)args.Caller.Player;
        int id = (int)args.Parsers[0].GetValue();
        SharableWaypoint[] ownwpaypoints = Waypoints.Where(waypoint => waypoint.OwningPlayerUid == player?.PlayerUID).ToArray();

        if (ownwpaypoints.Length == 0) {
            return TextCommandResult.Success(Lang.Get("You have no waypoints to delete"));
        }

        if (args.Parsers[0].IsMissing || id < 0 || id >= ownwpaypoints.Length) {
            return TextCommandResult.Success(Lang.Get("Invalid waypoint number, valid ones are 0..{0}", ownwpaypoints.Length - 1));
        }

        Waypoints.Remove(ownwpaypoints[id]);
        RebuildMapComponents();
        ResendWaypoints(player);
        return TextCommandResult.Success(Lang.Get("Ok, deleted waypoint."));
    }

    private TextCommandResult OnCmdWayPointDeathWp(TextCommandCallingArgs args) {
        if (IsMapDisallowed(out TextCommandResult? textCommandResult)) {
            return textCommandResult;
        }

        if (!api.World.Config.GetBool("allowDeathwaypointing", true)) {
            return TextCommandResult.Success(Lang.Get("Death waypointing is disabled on this server"));
        }

        IServerPlayer? player = (IServerPlayer)args.Caller.Player;
        if (args.Parsers[0].IsMissing) {
            bool on = player.GetModData<bool>("deathWaypointing");
            return TextCommandResult.Success(Lang.Get("Death waypoint is {0}", on ? Lang.Get("on") : Lang.Get("off")));
        } else {
            bool on = (bool)args.Parsers[0].GetValue();
            player.SetModData("deathWaypointing", on);
            return TextCommandResult.Success(Lang.Get("Death waypoint now {0}", on ? Lang.Get("on") : Lang.Get("off")));
        }
    }

    private void Event_PlayerDeath(IServerPlayer byPlayer, DamageSource damageSource) {
        if (!api.World.Config.GetBool("allowMap", true) || !api.World.Config.GetBool("allowDeathwaypointing", true) || !byPlayer.GetModData("deathWaypointing", true)) return;

        string title = Lang.Get("You died here");
        for (int i = 0; i < Waypoints.Count; i++) {
            var wp = Waypoints[i];
            if (wp.OwningPlayerUid == byPlayer.PlayerUID && wp.Title == title) {
                Waypoints.RemoveAt(i);
                i--;
            }
        }

        Waypoint waypoint = new Waypoint() {
            Color = ColorUtil.ColorFromRgba(200, 200, 200, 255),
            OwningPlayerUid = byPlayer.PlayerUID,
            Position = byPlayer.Entity.Pos.XYZ,
            Title = title,
            Icon = "gravestone",
            Pinned = true
        };

        AddWaypoint(waypoint, byPlayer);
    }

    private TextCommandResult OnCmdTpTo(TextCommandCallingArgs args) {
        var player = args.Caller.Player;
        var name = (args.Parsers[0].GetValue() as string).ToLowerInvariant();
        var playersWaypoints = Waypoints.Where((p) => p.OwningPlayerUid == player.PlayerUID).ToArray();

        foreach (var wp in playersWaypoints) {
            if (wp.Title != null && wp.Title.StartsWith(name, StringComparison.InvariantCultureIgnoreCase)) {
                player.Entity.TeleportTo(wp.Position);
                return TextCommandResult.Success(Lang.Get("Ok teleported you to waypoint {0}.", wp.Title));
            }
        }

        return TextCommandResult.Success(Lang.Get("No such waypoint found"));
    }

    private TextCommandResult OnCmdWayPointAdd(TextCommandCallingArgs args) {
        if (IsMapDisallowed(out var textCommandResult)) return textCommandResult;

        var parsedColor = (Color)args.Parsers[0].GetValue();
        var title = args.Parsers[1].GetValue() as string;
        var player = args.Caller.Player as IServerPlayer;
        return AddWp(player, player.Entity.Pos.XYZ, title, parsedColor, "circle", false);
    }

    private TextCommandResult OnCmdWayPointAddp(TextCommandCallingArgs args) {
        if (IsMapDisallowed(out var textCommandResult)) return textCommandResult;

        var parsedColor = (Color)args.Parsers[0].GetValue();
        var title = args.Parsers[1].GetValue() as string;
        var player = args.Caller.Player as IServerPlayer;
        return AddWp(player, player.Entity.Pos.XYZ, title, parsedColor, "circle", true);
    }

    private TextCommandResult OnCmdWayPointAddat(TextCommandCallingArgs args) {
        if (IsMapDisallowed(out var textCommandResult)) return textCommandResult;

        var pos = args.Parsers[0].GetValue() as Vec3d;
        var pinned = (bool)args.Parsers[1].GetValue();
        var parsedColor = (Color)args.Parsers[2].GetValue();
        var title = args.Parsers[3].GetValue() as string;


        var player = args.Caller.Player as IServerPlayer;
        return AddWp(player, pos, title, parsedColor, "circle", pinned);
    }

    private TextCommandResult OnCmdWayPointAddati(TextCommandCallingArgs args) {
        if (IsMapDisallowed(out var textCommandResult)) return textCommandResult;

        var icon = args.Parsers[0].GetValue() as string;
        var pos = args.Parsers[1].GetValue() as Vec3d;
        var pinned = (bool)args.Parsers[2].GetValue();
        var parsedColor = (Color)args.Parsers[3].GetValue();
        var title = args.Parsers[4].GetValue() as string;

        var player = args.Caller.Player as IServerPlayer;
        return AddWp(player, pos, title, parsedColor, icon, pinned);
    }

    private TextCommandResult OnCmdWayPointModify(TextCommandCallingArgs args) {
        if (IsMapDisallowed(out var textCommandResult)) return textCommandResult;

        var wpIndex = (int)args.Parsers[0].GetValue();

        var parsedColor = (Color)args.Parsers[1].GetValue();
        var icon = args.Parsers[2].GetValue() as string;
        var pinned = (bool)args.Parsers[3].GetValue();
        var title = args.Parsers[4].GetValue() as string;

        var player = args.Caller.Player as IServerPlayer;

        var playerWaypoints = Waypoints.Where(p => p.OwningPlayerUid == player.PlayerUID).ToArray();

        if (args.Parsers[0].IsMissing || wpIndex < 0 || playerWaypoints.Length < wpIndex - 1) {
            return TextCommandResult.Success(Lang.Get("command-modwaypoint-invalidindex", playerWaypoints.Length));
        }

        if (string.IsNullOrEmpty(title)) {
            return TextCommandResult.Success(Lang.Get("command-waypoint-notext"));
        }

        playerWaypoints[wpIndex].Color = parsedColor.ToArgb() | (255 << 24);
        playerWaypoints[wpIndex].Title = title;
        playerWaypoints[wpIndex].Pinned = pinned;

        if (icon != null) {
            playerWaypoints[wpIndex].Icon = icon;
        }

        ResendWaypoints(player);
        return TextCommandResult.Success(Lang.Get("Ok, waypoint nr. {0} modified", wpIndex));
    }

    private TextCommandResult AddWp(IServerPlayer player, Vec3d pos, string title, Color parsedColor, string icon, bool pinned) {
        if (string.IsNullOrEmpty(title)) {
            return TextCommandResult.Success(Lang.Get("command-waypoint-notext"));
        }

        var waypoint = new SharableWaypoint() {
            Color = parsedColor.ToArgb() | (255 << 24),
            OwningPlayerUid = player.PlayerUID,
            Position = pos,
            Title = title,
            Icon = icon,
            Pinned = pinned,
            Guid = Guid.NewGuid().ToString()
        };

        var nr = AddWaypoint(waypoint, player);
        return TextCommandResult.Success(Lang.Get("Ok, waypoint nr. {0} added", nr));
    }

    public int AddWaypoint(SharableWaypoint waypoint, IServerPlayer player) {
        Waypoints.Add(waypoint);

        SharableWaypoint[] ownwpaypoints = Waypoints.Where((p) => p.OwningPlayerUid == player.PlayerUID).ToArray();

        ResendWaypoints(player);

        return ownwpaypoints.Length - 1;
    }

    private void OnSaveGameGettingSaved() {
        _sapi.WorldManager.SaveGame.StoreData("playerMapMarkers_v2", SerializerUtil.Serialize(Waypoints));
    }


    public override void OnViewChangedServer(IServerPlayer fromPlayer, List<Vec2i> nowVisible, List<Vec2i> nowHidden) {
        ResendWaypoints(fromPlayer);
    }


    public override void OnMapOpenedClient() {
        reloadIconTextures();

        ensureIconTexturesLoaded();

        RebuildMapComponents();
    }

    public void reloadIconTextures() {
        if (texturesByIcon != null) {
            foreach (var val in texturesByIcon) {
                val.Value.Dispose();
            }
        }

        texturesByIcon = null;
        ensureIconTexturesLoaded();
    }

    protected void ensureIconTexturesLoaded() {
        if (texturesByIcon != null) return;

        texturesByIcon = new Dictionary<string, LoadedTexture>();

        foreach (var val in Icons) {
            texturesByIcon[val.Key] = val.Value();
        }
    }


    public override void OnMapClosedClient() {
        foreach (var val in tmpWayPointComponents) {
            wayPointComponents.Remove(val);
        }

        tmpWayPointComponents.Clear();
    }

    public override void Dispose() {
        if (texturesByIcon != null) {
            foreach (var val in texturesByIcon) {
                val.Value.Dispose();
            }
        }

        texturesByIcon = null;
        quadModel?.Dispose();

        base.Dispose();
    }

    public override void OnLoaded() {
        if (_sapi != null) {
            try {
                byte[] data = _sapi.WorldManager.SaveGame.GetData("playerMapMarkers_v2");
                if (data != null) {
                    Waypoints = SerializerUtil.Deserialize<List<SharableWaypoint>>(data);
                    _sapi.World.Logger.Notification("Successfully loaded " + Waypoints.Count + " waypoints");
                } else {
                    data = _sapi.WorldManager.SaveGame.GetData("playerMapMarkers");
                    if (data != null) Waypoints = JsonUtil.FromBytes<List<SharableWaypoint>>(data);
                }

                for (int i = 0; i < Waypoints.Count; i++) {
                    var wp = Waypoints[i];
                    if (wp == null) {
                        _sapi.World.Logger.Error("Waypoint with no information loaded, will remove");
                        Waypoints.RemoveAt(i);
                        i--;
                    }

                    if (wp.Title == null) wp.Title = wp.Text; // Not sure how this happens. For some reason the title moved into text
                }
            } catch (Exception e) {
                _sapi.World.Logger.Error("Failed deserializing player map markers. Won't load them, sorry! Exception thrown:");
                _sapi.World.Logger.Error(e);
            }

            foreach (var wp in Waypoints) {
                if (wp.Guid == null) wp.Guid = Guid.NewGuid().ToString();
            }
        }
    }

    public override void OnDataFromServer(byte[] data) {
        ownWaypoints.Clear();
        ownWaypoints.AddRange(SerializerUtil.Deserialize<List<SharableWaypoint>>(data));
        RebuildMapComponents();
    }


    public void AddTemporaryWaypoint(Waypoint waypoint) {
        SharableWaypointMapComponent comp = new SharableWaypointMapComponent(ownWaypoints.Count, waypoint, this, api as ICoreClientAPI);
        wayPointComponents.Add(comp);
        tmpWayPointComponents.Add(comp);
    }


    private void RebuildMapComponents() {
        if (!mapSink.IsOpened) return;

        foreach (var val in tmpWayPointComponents) {
            wayPointComponents.Remove(val);
        }

        foreach (SharableWaypointMapComponent comp in wayPointComponents) {
            comp.Dispose();
        }

        wayPointComponents.Clear();

        for (int i = 0; i < ownWaypoints.Count; i++) {
            SharableWaypointMapComponent comp = new SharableWaypointMapComponent(i, ownWaypoints[i], this, api as ICoreClientAPI);

            wayPointComponents.Add(comp);
        }

        wayPointComponents.AddRange(tmpWayPointComponents);
    }


    public override void Render(GuiElementMap mapElem, float dt) {
        if (!Active) return;

        foreach (var val in wayPointComponents) {
            val.Render(mapElem, dt);
        }
    }

    public override void OnMouseMoveClient(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText) {
        if (!Active) return;

        foreach (var val in wayPointComponents) {
            val.OnMouseMove(args, mapElem, hoverText);
        }
    }

    public override void OnMouseUpClient(MouseEvent args, GuiElementMap mapElem) {
        if (!Active) return;

        foreach (var val in wayPointComponents) {
            val.OnMouseUpOnElement(args, mapElem);
            if (args.Handled) break;
        }
    }


    void ResendWaypoints(IServerPlayer toPlayer) {
        Dictionary<int, PlayerGroupMembership> memberOfGroups = toPlayer.ServerData.PlayerGroupMemberships;
        List<SharableWaypoint> hisMarkers = new List<SharableWaypoint>();

        foreach (SharableWaypoint marker in Waypoints) {
            if (toPlayer.PlayerUID != marker.OwningPlayerUid && !memberOfGroups.ContainsKey(marker.OwningPlayerGroupId)) continue;
            hisMarkers.Add(marker);
        }

        mapSink.SendMapDataToClient(this, toPlayer, SerializerUtil.Serialize(hisMarkers));
    }
}
