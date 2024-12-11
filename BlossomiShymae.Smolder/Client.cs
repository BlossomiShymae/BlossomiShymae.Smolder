using BlossomiShymae.Smolder.Files;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BlossomiShymae.Smolder;

/// <summary>
/// A client interface for CommunityDragon.
/// </summary>
public class Client
{
    internal SemaphoreSlim SemaphoreSlim = new(20, 20);

    private int _concurrentDownloadCount = 20;

    /// <summary>
    /// The maximum amount of download tasks to concurrently run.
    /// </summary>
    public int ConcurrentDownloadCount
    {
        get
        {
            return _concurrentDownloadCount;
        }
        init
        {
            SemaphoreSlim = new SemaphoreSlim(value, value);
            _concurrentDownloadCount = value;
        }
    }

    /// <summary>
    /// The HTTP client to use when making requests.
    /// </summary>
    public HttpClient HttpClient { get; init; } = new();
    /// <summary>
    /// The interface that will be used for logging client events.
    /// </summary>
    public ILogger<Client> Logger { get; init; } = NullLogger<Client>.Instance;
    /// <summary>
    /// The output folder that will be used to save downloaded files.
    /// </summary>
    public string OutputPath { get; init; } = "out";
    /// <summary>
    /// The amount of retries to attempt before cancelling a request.
    /// </summary>
    public int Retries { get; init; } = 3;
    /// <summary>
    /// The maximum depth to download files for a folder.
    /// The default of 0 enables recursion.
    /// </summary>
    public int MaxDepth { get; init; } = 0;
    /// <summary>
    /// The option to overwrite the output folder. The output folder will be deleted
    /// by default if it already exists.
    /// </summary>
    public bool OverwriteFolder { get; init; } = true;
    /// <summary>
    /// The option to skip files that already exists. Disabling this will overwrite files
    /// previously downloaded.
    /// </summary>
    public bool SkipFile { get; init; } = true;
    /// <summary>
    /// The filter to use when downloading files. Files will only be downloaded if it 
    /// contains this filter.
    /// </summary>
    public string Filter { get; init; } = string.Empty;

    /// <summary>
    /// Download files in the directory link provided using the JSON files strategy. 
    /// Supports both raw and universe subdomains.
    /// <example>
    /// <code>
    /// await client.DownloadDirectoryAsync("https://raw.communitydragon.org/latest/game/data/images/");
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="url">The CommunityDragon URL directory to download from.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task DownloadDirectoryAsync(string url, CancellationToken cancellationToken = default)
    {
        DeleteDirectory();

        var directories = new ConcurrentStack<string>();
        var pointerUrl = url.Replace(".org", ".org/json");
        var _url = pointerUrl;
        Logger.LogInformation("Downloading directory: {Url}", _url);

        var tasks = new ConcurrentBag<Task>();
        do
        {
            Logger.LogDebug("Getting json files: {Url}", Uri.UnescapeDataString(pointerUrl));
            var files = await GetJsonFilesAsync(pointerUrl, cancellationToken)
                .ConfigureAwait(false);

            if (files.Count == 0)
                break;

            Parallel.ForEach(files, file =>
            {
                switch (file.Raw.FileType)
                {
                    case FileType.Directory:
                        if (MaxDepth > 0)
                        {
                            var pathDifference = Path.Join(pointerUrl, file.Raw.EncodedName)
                                .Replace(_url, string.Empty);
                            var depth = pathDifference.Split('/', StringSplitOptions.RemoveEmptyEntries).Length;
                            if (depth >= MaxDepth)
                            {
                                Logger.LogDebug("Skipping directory as it exceeds max depth: {Tuple}", (depth, MaxDepth, file.Raw.Name));
                                return;
                            }
                        }
                        Logger.LogDebug("Pushing directory: {Tuple}", (file.Referrer, pointerUrl, file.Raw.Name));
                        directories.Push(Path.Join(pointerUrl, file.Raw.EncodedName, "/"));
                        break;
                    case FileType.File:
                        tasks.Add(DownloadFileAsync(_url, file, cancellationToken));
                        break;
                    case FileType.Other:
                        Logger.LogWarning("Skipping 'other' type, file is likely missing: {Url}", file.Url);
                        break;
                }
            });

            Logger.LogDebug("Directories left: {Count}", directories.Count);
        } while (directories.TryPop(out pointerUrl));

        Logger.LogDebug("Finished adding files to queue");

        await Task.WhenAll(tasks)
            .ConfigureAwait(false);

        Logger.LogInformation("Finished directory: {Url}", _url);
    }

    /// <summary>
    /// Download files in the directory link provided using the exported files strategy. 
    /// Supports only the raw subdomain.
    /// For url(s) with the universe subdomain, use <see cref="Client.DownloadDirectoryAsync(string, CancellationToken)"/>.
    /// <example>
    /// <code>
    /// var latest = await client.GetExportedFilesAsync("latest");
    /// await client.DownloadDirectoryAsync("https://raw.communitydragon.org/latest/game/data/images/", latest);
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="url">The CommunityDragon URL directory to download from.</param>
    /// <param name="exportedFiles">The associated exported files to use. File versions must match with given URL.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task DownloadDirectoryAsync(string url, List<ExportedFile> exportedFiles, CancellationToken cancellationToken = default)
    {
        DeleteDirectory();

        Logger.LogInformation("Downloading directory: {Url}", url);
        var patchVersion = url.Replace("https://raw.communitydragon.org", string.Empty)
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .First();
        Logger.LogDebug("Parsed patch version: {PatchVersion}", patchVersion);

        var prepath = $"https://raw.communitydragon.org/{patchVersion}";
        var path = url.Replace(prepath, string.Empty);
        var directories = path.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        var tasks = new List<Task>();
        foreach (var exportedFile in exportedFiles)
        {
            tasks.Add(DownloadFileAsync(url, directories, exportedFile, cancellationToken));
        }

        await Task.WhenAll(tasks)
            .ConfigureAwait(false);

        Logger.LogInformation("Finished directory: {Url}", url);
    }

    /// <summary>
    /// Search exported files by using a query. Regex patterns are supported.
    /// <example>
    /// <code>
    /// var latest = await client.GetExportedFilesAsync("latest");
    /// var results = client.Search("smolder", latest);
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="query">The search query to use. Supports Regex patterns.</param>
    /// <param name="exportedFiles">The exported files to search from.</param>
    /// <returns></returns>
    public List<string> Search(string query, List<ExportedFile> exportedFiles)
    {
        return Regex.Matches(string.Join("\n", exportedFiles.Select(f => f.Url)), $"^.*{query}.*$", RegexOptions.Multiline)
            .Select(m => m.Value)
            .ToList();
    }

    /// <summary>
    /// Get exported files by patch version.
    /// <example>
    /// <code>
    /// var latest = await client.GetExportedFilesAsync("latest");
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="patchVersion">The patch version to get the files.exported.txt from.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ValidationException"></exception>
    public async Task<List<ExportedFile>> GetExportedFilesAsync(string patchVersion, CancellationToken cancellationToken = default)
    {
        var patchVersions = await GetPatchVersionsAsync(cancellationToken)
            .ConfigureAwait(false);
        if (!patchVersions.Contains(patchVersion))
            throw new ValidationException("Invalid version provided");

        var bytes = await HttpClient.GetByteArrayAsync($"https://raw.communitydragon.org/{patchVersion}/cdragon/files.exported.txt", cancellationToken)
            .ConfigureAwait(false);

        return System.Text.Encoding.UTF8.GetString(bytes)
            .Split('\n')
            .AsParallel()
            .Select(line => new ExportedFile { Path = line, Version = patchVersion })
            .ToList();
    }

    /// <summary>
    /// Get processed JSON files from the directory link provided.
    /// <example>
    /// <code>
    /// var files = await client.GetJsonFilesAsync("https://raw.communitydragon.org/latest/game/data/images/");
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="url">The CommunityDragon URL directory to download from.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<JsonFile>> GetJsonFilesAsync(string url, CancellationToken cancellationToken = default)
    {
        var rawFiles = await GetRawFilesAsync(url, cancellationToken)
            .ConfigureAwait(false);

        return rawFiles.AsParallel().Select(f => new JsonFile()
        {
            Raw = f,
            Referrer = url
        }).ToList();
    }

    /// <summary>
    /// Get raw JSON files from the directory link provided.
    /// <example>
    /// <code>
    /// var files = await client.GetRawFilesAsync("https://raw.communitydragon.org/latest/game/data/images/");
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="url">The CommunityDragon URL directory to download from.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<RawFile[]> GetRawFilesAsync(string url, CancellationToken cancellationToken = default)
    {
        ValidateUrl(url);

        var res = await HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, url), cancellationToken)
            .ConfigureAwait(false);

        if (!res.IsSuccessStatusCode)
            throw new HttpRequestException(null, null, res.StatusCode);

        var contentStream = await res.Content.ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var rawFiles = await JsonSerializer.DeserializeAsync(contentStream, SourceGenerationContext.Default.RawFileArray, cancellationToken)
            .ConfigureAwait(false);

            return rawFiles ?? [];
        }
        catch (JsonException ex) // "Downloading the champion game data from patch 8.18 works fine but when I try to download the same data from patch 8.19 and beyond I get this error" - .voldemort
        {
            Logger.LogError(ex, "Failed to deserialize JSON file: {Tuple}", url);
            throw;
        }

    }

    /// <summary>
    /// Get all the available patch versions from the root directory.
    /// <example>
    /// <code>
    /// var patchVersions = await client.GetPatchVersionsAsync();
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<string>> GetPatchVersionsAsync(CancellationToken cancellationToken = default)
    {
        var rawFiles = await GetRawFilesAsync("https://raw.communitydragon.org/json/", cancellationToken)
            .ConfigureAwait(false);

        return rawFiles.Where(IsPatchVersion)
            .Select(f => f.Name)
            .ToList();
    }
    private void DeleteDirectory()
    {
        if (OverwriteFolder && Directory.Exists(OutputPath))
        {
            try
            {
                Directory.Delete(OutputPath, true);
            }
            catch (IOException ex)
            {
                Logger.LogWarning(ex, "Directory is likely being used");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to overwrite directory: {Path}", OutputPath);
                throw;
            }
        }
    }

    private async Task DownloadFileAsync(string url, List<string> directories, ExportedFile exportedFile, CancellationToken cancellationToken)
    {
        if (!exportedFile.Url.Contains(url))
            return;

        var exportedDirectories = exportedFile.Directories.Except(directories)
            .Prepend(OutputPath)
            .ToArray();
        Logger.LogDebug("Directory path: {Directories}", exportedDirectories);
        var directoryPath = Path.Join(exportedDirectories);

        if (MaxDepth > 0)
        {
            var depth = exportedDirectories.Length;
            if (depth > MaxDepth)
            {
                Logger.LogDebug("Skipping directory as it exceeds max depth: {Tuple}", (depth, MaxDepth, $"[{string.Join(", ", exportedDirectories)}]", exportedFile.FileName));
                return;
            }
        }

        Directory.CreateDirectory(directoryPath);

        await DownloadAsync(exportedFile.Url, exportedFile.FileName, directoryPath, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task DownloadFileAsync(string url, JsonFile file, CancellationToken cancellationToken)
    {
        string directoryPath = GetDirectoryPath(url, Uri.UnescapeDataString(file.Referrer));

        Directory.CreateDirectory(directoryPath);

        await DownloadAsync(file.Url, file.Raw.Name, directoryPath, cancellationToken)
            .ConfigureAwait(false);
    }

    private string GetDirectoryPath(string url, string referrer)
    {
        var _directories = referrer.Replace(url, string.Empty)
            .Split("/", StringSplitOptions.RemoveEmptyEntries)
            .Prepend(OutputPath)
            .ToArray();
        var directoryPath = Path.Join(_directories);
        return directoryPath;
    }

    private async Task DownloadAsync(string fileUrl, string fileName, string directoryPath, CancellationToken cancellationToken)
    {
        var filePath = Path.Join(directoryPath, fileName);
        if (SkipFile && File.Exists(filePath))
        {
            Logger.LogDebug("Skipping file as it exists: {FilePath}", filePath);
            return;
        }
        if (!string.IsNullOrWhiteSpace(Filter) && !fileName.Contains(Filter))
        {
            Logger.LogDebug("File doesn't pass filter: {Tuple}", (fileName, Filter));
            return;
        }

        for (int i = 0; i < Retries; i++)
        {
            try
            {
                await SemaphoreSlim.WaitAsync(cancellationToken)
                    .ConfigureAwait(false);

                Logger.LogInformation("Downloading file: {Url}", fileUrl);
                var res = await HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, fileUrl), HttpCompletionOption.ResponseContentRead, cancellationToken)
                .ConfigureAwait(false);

                if ((int)res.StatusCode >= 500)
                {
                    Logger.LogWarning("Received 5xx request: {Code}", res.StatusCode);
                    continue;
                }
                else if (!res.IsSuccessStatusCode)
                {
                    Logger.LogError("Received 4XX request: {Tuple}", (res.StatusCode, fileUrl));
                    throw new HttpRequestException(null, null, res.StatusCode);
                }

                Logger.LogDebug("Successful request: {Url}", fileUrl);

                using var fileStream = new FileStream(filePath, FileMode.Create);

                await res.Content.CopyToAsync(fileStream, cancellationToken)
                    .ConfigureAwait(false);

                return;
            }
            catch (HttpRequestException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Logger.LogError("Bad file url: {Url}", fileUrl);
                    break;
                }
                throw;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                Logger.LogError("Timed out request: {Url}", fileUrl);
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        Logger.LogError("Failed retrying: {Url}", fileUrl);
        throw new Exception("Failed to download file");
    }

    private static bool IsPatchVersion(RawFile rawFile)
    {
        return rawFile.Name != "runeterra"
            && rawFile.Name != "favicon.ico"
            && rawFile.Name != "status.live.txt"
            && rawFile.Name != "status.pbe.txt";
    }

    private static void ValidateUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ValidationException($"The URL must not be empty: {url}");
        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            throw new ValidationException($"The URL must be a valid URI string: {url}");

        var uri = new Uri(url);
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            throw new ValidationException($"The URL must be HTTP or HTTPS: {url}");
        if (!url.Contains("communitydragon.org"))
            throw new ValidationException($"The URL must be in a valid CommunityDragon path: {url}");
        if (!url.EndsWith('/'))
            throw new ValidationException($"The URL must end with '/': {url}");
    }
}