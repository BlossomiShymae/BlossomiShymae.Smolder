using System.Text.Json.Serialization;

namespace BlossomiShymae.Smolder.Files;

/// <summary>
/// An exported file representing a line in a files.exported.txt.
/// </summary>
public record ExportedFile
{
    /// <summary>
    /// The internal path represented in the game files.
    /// </summary>
    public string Path { get; internal init; } = string.Empty;
    /// <summary>
    /// The associated patch version.
    /// </summary>
    public string Version { get; internal init; } = string.Empty;

    /// <summary>
    /// The URL where the file is located on https://raw.communitydragon.org/.
    /// </summary>
    [JsonIgnore]
    public string Url => $"https://raw.communitydragon.org/{Version}/{Path}";

    /// <summary>
    /// The level of directories the file is in.
    /// </summary>
    [JsonIgnore]
    public List<string> Directories {
        get {
            if (!Path.Contains('/')) 
                return [];

            return Path.Split('/', StringSplitOptions.RemoveEmptyEntries)
                .SkipLast(1)
                .ToList();
        }
    }

    /// <summary>
    /// The internal file name represented in the game files.
    /// </summary>
    [JsonIgnore]
    public string FileName {
        get {
            if (!Path.Contains('/'))
                return Path;

            return Path.Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Last();
        }
    }
}