using System.Text.Json.Serialization;

namespace BlossomiShymae.Smolder.Files;

/// <summary>
/// A wrapper for raw JSON files with useful meta information.
/// </summary>
public record JsonFile
{
    /// <summary>
    /// The referrer URL for the raw JSON file.
    /// </summary>
    public string Referrer { get; internal init;} = string.Empty;
    /// <summary>
    /// The raw JSON file.
    /// </summary>
    public RawFile Raw { get; internal init; } = new();

    /// <summary>
    /// The URL where the file is located at https://raw.communitydragon.org/.
    /// </summary>
    [JsonIgnore]
    public string Url => string.Join(string.Empty, Referrer, Raw.EncodedName);
}