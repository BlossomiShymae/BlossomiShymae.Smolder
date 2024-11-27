using System.Text.Json.Serialization;

namespace BlossomiShymae.Smolder.Files;

/// <summary>
/// Represents a raw JSON file fetched from https://raw.communitydragon.org/.
/// </summary>
public record RawFile
{
    /// <summary>
    /// The file name.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonInclude] 
    public string Name { get; internal init; } = string.Empty;

    /// <summary>
    /// The file type.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonInclude] 
    public FileType FileType { get; internal init; } = new();

    /// <summary>
    /// The file modification time.
    /// </summary>
    [JsonPropertyName("mtime")]
    [JsonInclude] 
    public string MTime { get; internal init; } = string.Empty;
    
    /// <summary>
    /// The file size represented in bytes.
    /// </summary>
    [JsonPropertyName("size")]
    [JsonInclude] 
    public int? Size { get; internal init; }

    /// <summary>
    /// The file name in escaped representation.
    /// </summary>
    [JsonIgnore]
    public string EncodedName => Uri.EscapeDataString(Name);
}

[JsonSerializable(typeof(RawFile[]))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}