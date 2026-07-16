using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModpackInstaller.Converters;

public sealed class FlexibleGuidConverter : JsonConverter<Guid>
{
    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();

        if (string.IsNullOrWhiteSpace(value))
            throw new JsonException("The JSON value is not in a supported Guid format");

        return Guid.TryParse(value, out var guid) 
            ? guid 
            : throw new JsonException("The JSON value is not in a supported Guid format");
    }

    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options) {
        writer.WriteStringValue(value.ToString("N"));
    }
}

public sealed class FlexibleNullableGuidConverter : JsonConverter<Guid?>
{
    public override Guid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();

        if (string.IsNullOrWhiteSpace(value))
            return null;

        return Guid.TryParse(value, out var guid) 
            ? guid 
            : throw new JsonException("The JSON value is not in a supported Guid format");
    }

    public override void Write(Utf8JsonWriter writer, Guid? value, JsonSerializerOptions options) {
        if (value.HasValue)
            writer.WriteStringValue(value.Value.ToString("N"));
        else
            writer.WriteNullValue();
    }
}