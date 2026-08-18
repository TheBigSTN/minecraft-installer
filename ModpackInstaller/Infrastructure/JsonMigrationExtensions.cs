using System.Text.Json;
using System.Text.Json.Nodes;

namespace ModpackInstaller.Infrastructure;

public static class JsonMigrationExtensions {
    public static T? GetField<T>(this JsonObject root, string fieldName) {
        if (!root.TryGetPropertyValue(fieldName, out var node) || node == null)
            return default;

        try {
            return node.Deserialize<T>();
        }
        catch {
            return default;
        }
    }

    public static T GetFieldOrDefault<T>(this JsonObject root, string fieldName, T defaultValue) {
        return root.GetField<T>(fieldName) ?? defaultValue;
    }

    public static void SetField<T>(this JsonObject root, string fieldName, T value) {
        root[fieldName] = JsonSerializer.SerializeToNode(value);
    }
}