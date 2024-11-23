
using BlossomiShymae.Smolder;
using Meziantou.Extensions.Logging.Xunit;
using Xunit.Abstractions;

public class ClientTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task Get_PatchVersions_ReturnsCorrectFormat()
    {
        var client = new Client() { Logger = XUnitLogger.CreateLogger<Client>(testOutputHelper) };

        var patchVersions = await client.GetPatchVersionsAsync();

        Assert.NotEmpty(patchVersions);
        Assert.Contains("latest", patchVersions);
        Assert.Contains("pbe", patchVersions);
        Assert.Contains("14.22", patchVersions);
        Assert.DoesNotContain("status.live.txt", patchVersions);
        Assert.DoesNotContain("status.pbe.txt", patchVersions);
    }

    [Fact]
    public async Task GetExportedFiles_OnLatest_ReturnsExpectedFiles()
    {
        var client = new Client() { Logger = XUnitLogger.CreateLogger<Client>(testOutputHelper) };
        var patchVersion = "latest";
        
        var exportedFiles = await client.GetExportedFilesAsync(patchVersion);
        var itemsFile = exportedFiles.Find(f => f.Path == "plugins/rcp-be-lol-game-data/global/default/v1/items.json");
        var systemFile = exportedFiles.Find(f => f.Path == "system.yaml");

        Assert.NotEmpty(exportedFiles);
        Assert.Equal("https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/v1/items.json", itemsFile!.Url);
        Assert.Equal(["plugins", "rcp-be-lol-game-data", "global", "default", "v1"], itemsFile.Directories);
        Assert.Equal("https://raw.communitydragon.org/latest/system.yaml", systemFile!.Url);
        Assert.Equal("system.yaml", systemFile.FileName);
        Assert.Empty(systemFile.Directories);
    }

    [Fact]
    public async Task DownloadDirectory_OnExportedFiles_Returns()
    {
        var client = new Client() { OutputPath = "out-exported", Logger = XUnitLogger.CreateLogger<Client>(testOutputHelper) };
        var url = "https://raw.communitydragon.org/latest/game/data/images/";

        var latest = await client.GetExportedFilesAsync("latest");
        await client.DownloadDirectoryAsync(url, latest);        
    }

    [Fact]
    public async Task DownloadDirectory_OnJsonFiles_Returns()
    {
        var client = new Client() { OutputPath = "out-json", Logger = XUnitLogger.CreateLogger<Client>(testOutputHelper) };
        var url = "https://raw.communitydragon.org/latest/game/data/images/";

        await client.DownloadDirectoryAsync(url);
    }

    [Fact]
    public async Task DownloadDirectory_OnJsonFilesWithMaxDepth_Returns()
    {
        var client = new Client() { OutputPath = "out-json-max-depth", Logger = XUnitLogger.CreateLogger<Client>(testOutputHelper), MaxDepth = 1 };
        var url = "https://raw.communitydragon.org/latest/game/data/images/";

        await client.DownloadDirectoryAsync(url);
    }

    [Fact]
    public async Task DownloadDirectory_OnExportedFilesWithMaxDepth_Returns()
    {
        var client = new Client() { OutputPath = "out-exported-max-depth", Logger = XUnitLogger.CreateLogger<Client>(testOutputHelper), MaxDepth = 1 };
        var url = "https://raw.communitydragon.org/latest/game/data/images/";

        var latest = await client.GetExportedFilesAsync("latest");
        await client.DownloadDirectoryAsync(url, latest);
    }

    // [Fact]
    // public async Task DownloadDirectory_OnUniverse_Returns()
    // {
    //     var client = new Client() { OutputPath = "out-universe", Logger = XUnitLogger.CreateLogger<Client>(testOutputHelper) };
    //     var url = "https://universe.communitydragon.org/events/2024/mgs-crepe/assets/";

    //     await client.DownloadDirectoryAsync(url);
    // }

    [Fact]
    public async Task Search_ForGwen_ReturnsExpectedResults()
    {
        var client = new Client() { Logger = XUnitLogger.CreateLogger<Client>(testOutputHelper) };

        var exportedFiles = await client.GetExportedFilesAsync("latest");
        var results = client.Search("gwen", exportedFiles);

        Assert.NotEmpty(results);
        Assert.Contains("https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/assets/loot/companions/chibigwen/loot_chibigwen_cafecuties_cafecuties1_tier1.chibi_gwen_cafecuties.png", results);
    }
}