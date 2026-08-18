using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Nodes;

namespace ModpackInstaller.Services.Helpers;

public static class MigrationRunner {
    public static void Migrate<T>(JsonObject root)
        where T : IMigratedData {
        MigrateNode(root, typeof(T));
    }

    private static void MigrateNode(JsonNode? node, Type type) {
        if (node is null)
            return;

        // If this object is migratable, migrate it.
        if (typeof(IMigratedData).IsAssignableFrom(type)) {
            if (node is not JsonObject obj)
                return;

            MigrateObject(obj, type);
        }

        // Now inspect its children.
        if (node is JsonObject jsonObject) {
            foreach (var property in GetProperties(type)) {
                if (!jsonObject.TryGetPropertyValue(
                        property.Name,
                        out var child))
                    continue;

                MigrateNode(child, property.PropertyType);
            }
        }
        else if (node is JsonArray array) {
            var elementType = GetElementType(type);

            if (elementType is null)
                return;

            foreach (var item in array)
                MigrateNode(item, elementType);
        }
    }

    private static void MigrateObject(JsonObject root, Type type) {
        var version = root["_version"]?.GetValue<int>() ?? 0;

        if (version >= GetSchemaVersion(type))
            return;

        InvokeMigrate(type, root, version);
    }

    private static PropertyInfo[] GetProperties(Type type) {
        return type.GetProperties(
            BindingFlags.Instance |
            BindingFlags.Public);
    }

    private static Type? GetElementType(Type type) {
        if (type.IsArray)
            return type.GetElementType();

        if (type.IsGenericType &&
            type.GetGenericTypeDefinition() == typeof(List<>))
            return type.GetGenericArguments()[0];

        return null;
    }

    private static int GetSchemaVersion(Type type) {
        return (int)type
                    .GetProperty(nameof(IMigratedData.SchemaVersion),
                                 BindingFlags.Static |
                                 BindingFlags.Public)!
                    .GetValue(null)!;
    }

    private static void InvokeMigrate(
        Type type,
        JsonObject root,
        int version) {
        type.GetMethod(
                nameof(IMigratedData.Migrate),
                BindingFlags.Static |
                BindingFlags.Public)!
            .Invoke(null, [root, version]);
    }
}