using System.Reflection;

namespace SharableWaypoints.Common;

public static class ReflectionHelper {
    public static T? GetField<T>(this object obj, string name) {
        return (T?)obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(obj);
    }

    public static void Invoke(this object obj, string name) {
        obj.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(obj, null);
    }
}
