using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ModpackInstaller.Infrastructure;

namespace ModpackInstaller.Services.Helpers;

public sealed class MigratedDataJsonConverter<T> : JsonConverter<T>
    where T : IMigratedData {
    private readonly JsonSerializerOptions _options;

    public MigratedDataJsonConverter(JsonSerializerOptions options) {
        _options = new JsonSerializerOptions(options);

        for (var i = _options.Converters.Count - 1; i >= 0; i--) {
            if (_options.Converters[i] is MigratedDataJsonConverterFactory)
                _options.Converters.RemoveAt(i);
        }
    }

    public override T? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) {
        var root = JsonNode.Parse(ref reader)?.AsObject();

        if (root is null)
            return default;

        MigrationRunner.Migrate<T>(root);

        return root.Deserialize<T>(_options);
    }

    public override void Write(
        Utf8JsonWriter writer,
        T value,
        JsonSerializerOptions options) {
        var root = JsonSerializer.SerializeToNode(value, _options)!.AsObject();

        root["_version"] = T.SchemaVersion;

        root.WriteTo(writer, options);
    }
}