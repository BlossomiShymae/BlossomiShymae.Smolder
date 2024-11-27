
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BlossomiShymae.Smolder.Files;

namespace BlossomiShymae.Smolder.Benchmarks;

[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[JsonExporterAttribute.Brief]
[MarkdownExporterAttribute.GitHub]
public class DownloadDirectoryJson
{
    private static HttpClient _httpClient = default!;
    private static readonly string _directory = "https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/assets/loadouts/summonerbanners/";

    [GlobalSetup]
    public void GlobalSetup()
    {
        _httpClient = new HttpClient();
    }

    [Benchmark(Baseline = true)]
    public async Task WithBaseline()
    {
        var client = new Client() { HttpClient = _httpClient };
        await client.DownloadDirectoryAsync(_directory);
    }
}

[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[JsonExporterAttribute.Brief]
[MarkdownExporterAttribute.GitHub]
public class DownloadDirectoryExported
{
    private static HttpClient _httpClient = default!;
    private static readonly string _directory = "https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/assets/loadouts/summonerbanners/";
    public static List<ExportedFile> _exportedFiles = [];

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _httpClient = new HttpClient();

        var client = new Client();
        _exportedFiles = await client.GetExportedFilesAsync("latest");
    }

    [Benchmark]
    public async Task WithCached()
    {
        var client = new Client() { HttpClient = _httpClient };
        await client.DownloadDirectoryAsync(_directory, _exportedFiles);
    }

    [Benchmark]
    public async Task WithoutCached()
    {
        var client = new Client() { HttpClient = _httpClient };
        var exportedFiles = await client.GetExportedFilesAsync("latest");
        await client.DownloadDirectoryAsync(_directory, exportedFiles);
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<DownloadDirectoryJson>();
    }
}