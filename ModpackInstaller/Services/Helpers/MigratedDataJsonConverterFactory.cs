using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModpackInstaller.Services.Helpers;

public sealed class MigratedDataJsonConverterFactory : JsonConverterFactory {
    public override bool CanConvert(Type typeToConvert) {
        return typeof(IMigratedData).IsAssignableFrom(typeToConvert);
    }

    public override JsonConverter CreateConverter(
        Type typeToConvert,
        JsonSerializerOptions options) {
        var converterType = typeof(MigratedDataJsonConverter<>)
            .MakeGenericType(typeToConvert);

        return (JsonConverter)Activator.CreateInstance(
            converterType,
            options)!;
    }
}