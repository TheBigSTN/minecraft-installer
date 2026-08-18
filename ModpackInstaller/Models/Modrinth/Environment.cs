using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModpackInstaller.Models.Modrinth;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Environment {
    ClientOnly,
    ServerOnly,
    ClientAndServer,
    ClientOnlyServerOptional,
    ServerOnlyClientOptional,
    SingleplayerOnly,
    DedicatedServerOnly,
    ClientOrServer,
    ClientOrServerPrefersBoth,
    Unknown,
    HaveToRequest
}

public class EnvironmentConverter : JsonConverter<Environment> {
    public override Environment Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) {
        var value = reader.GetString();

        return value switch {
            "client_and_server" or "ClientAndServer"
                => Environment.ClientAndServer,

            "client_only" or "ClientOnly"
                => Environment.ClientOnly,

            "client_only_server_optional" or "ClientOnlyServerOptional"
                => Environment.ClientOnlyServerOptional,

            "singleplayer_only" or "SingleplayerOnly"
                => Environment.SingleplayerOnly,

            "server_only" or "ServerOnly"
                => Environment.ServerOnly,

            "server_only_client_optional" or "ServerOnlyClientOptional"
                => Environment.ServerOnlyClientOptional,

            "dedicated_server_only" or "DedicatedServerOnly"
                => Environment.DedicatedServerOnly,

            "client_or_server" or "ClientOrServer"
                => Environment.ClientOrServer,

            "client_or_server_prefers_both" or "ClientOrServerPrefersBoth"
                => Environment.ClientOrServerPrefersBoth,

            "unknown" or "Unknown"
                => Environment.Unknown,

            "have_to_request" or "HaveToRequest"
                => Environment.HaveToRequest,

            _ => throw new JsonException(
                $"Unknown environment value '{value}'.")
        };
    }

    public override void Write(Utf8JsonWriter writer, Environment value, JsonSerializerOptions options) {
        var stringValue = value switch {
            Environment.ClientAndServer => "ClientAndServer",
            Environment.ClientOnly => "ClientOnly",
            Environment.ClientOnlyServerOptional => "ClientOnlyServerOptional",
            Environment.SingleplayerOnly => "SingleplayerOnly",
            Environment.ServerOnly => "ServerOnly",
            Environment.ServerOnlyClientOptional => "ServerOnlyClientOptional",
            Environment.DedicatedServerOnly => "DedicatedServerOnly",
            Environment.ClientOrServer => "ClientOrServer",
            Environment.ClientOrServerPrefersBoth => "ClientOrServerPrefersBoth",
            Environment.HaveToRequest => "HaveToRequest",
            Environment.Unknown => "Unknown",
            _ => throw new ArgumentOutOfRangeException(nameof(value), "Unknown value")
        };

        writer.WriteStringValue(stringValue);
    }
}