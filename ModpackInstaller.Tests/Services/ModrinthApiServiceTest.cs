using System.Threading.Tasks;
using JetBrains.Annotations;
using ModpackInstaller.Services;
using Xunit;

namespace ModpackInstaller.Tests.Services;

[TestSubject(typeof(ModrinthApiService))]
public class ModrinthApiServiceTest {
    [Fact]
    public async Task GetVersion_ReturnsVersion() {
        var version = await ModrinthApiService.GetVersionAsync("v0WpqBBM");

        Assert.NotNull(version);
        Assert.Equal("v0WpqBBM", version.Id);
    }
}