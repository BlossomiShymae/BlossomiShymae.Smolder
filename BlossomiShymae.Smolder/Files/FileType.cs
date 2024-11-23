using System.Text.Json.Serialization;

namespace BlossomiShymae.Smolder.Files;

/// <summary>
/// A file type associated with a file.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FileType
{
    Directory,
    File
}