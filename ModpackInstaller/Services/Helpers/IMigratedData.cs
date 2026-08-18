using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace ModpackInstaller.Services.Helpers;

public interface IMigratedData {
    static abstract int SchemaVersion { get; }
    static abstract void Migrate(JsonObject root, int fileVersion);
}