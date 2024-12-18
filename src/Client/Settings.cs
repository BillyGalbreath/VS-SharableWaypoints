using Vintagestory.API.Util;

namespace SharableWaypoints.Client;

public class Settings {
    private readonly Dictionary<string, string?> _storedWaypointData = new();

    private static Settings? _instance;

    public static string? GetWaypointName(string index) {
        return (_instance ??= Read())._storedWaypointData.Get(index);
    }

    public static void SetWaypointName(string index, string name) {
        (_instance ??= Read())._storedWaypointData[index] = name;
        Write();
    }
}
