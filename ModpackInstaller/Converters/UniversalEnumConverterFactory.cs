using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModpackInstaller.Models.Modrinth;
using Environment = System.Environment;

namespace ModpackInstaller.Converters;

public class UniversalEnumConverterFactory : JsonConverterFactory {
    private readonly JsonConverterFactory _stringEnumConverterFactory = new JsonStringEnumConverter();

    public override bool CanConvert(Type typeToConvert) {
        return typeToConvert.IsEnum;
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
        var underlyingType = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;

        // Check against your specific Environment enum type
        if (underlyingType == typeof(ModpackInstaller.Models.Modrinth.Environment)) {
            return new EnvironmentConverter();
        }

        // Otherwise, use the default string converter for all other enums
        return _stringEnumConverterFactory.CreateConverter(typeToConvert, options);
    }
}