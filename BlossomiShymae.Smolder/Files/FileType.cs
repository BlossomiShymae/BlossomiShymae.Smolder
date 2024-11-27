using System.Text.Json.Serialization;

namespace BlossomiShymae.Smolder.Files;

/// <summary>
/// A file type associated with a file.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<FileType>))]
public enum FileType
{
    /// <summary>
    /// Contains other files.
    /// </summary>
    Directory,
    /// <summary>
    /// Contains information.
    /// </summary>
    File
}