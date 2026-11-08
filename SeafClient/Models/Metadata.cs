using System.Text.Json;
using System.Text.Json.Serialization;

namespace SeafClient.Models;

/// <summary>Status of the metadata-server features of a library (Seafile 12+ Pro).</summary>
public sealed record SeafMetadataStatus
{
    public bool Enabled { get; init; }
    public bool TagsEnabled { get; init; }
    public bool FaceRecognitionEnabled { get; init; }
    public bool SummaryEnabled { get; init; }
}

/// <summary>
/// A metadata record (one per file/folder). Built-in columns start with '_' (<c>_name</c>, <c>_parent_dir</c>,
/// <c>_description</c>, ...); custom columns use their column key.
/// </summary>
public sealed class SeafMetadataRecord
{
    public SeafMetadataRecord(IReadOnlyDictionary<string, JsonElement> fields) => Fields = fields;

    public IReadOnlyDictionary<string, JsonElement> Fields { get; }

    public string Id => GetString("_id") ?? "";
    public string? Name => GetString("_name");
    public string? ParentDir => GetString("_parent_dir");
    public bool IsDir => Fields.TryGetValue("_is_dir", out var v) && v.ValueKind == JsonValueKind.True;
    public string? Description => GetString("_description");

    public string? GetString(string key) =>
        Fields.TryGetValue(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}

/// <summary>One update in a batch: the record id and the column values to set.</summary>
public sealed record SeafMetadataUpdate(string RecordId, IReadOnlyDictionary<string, object?> Values)
{
    [JsonPropertyName("record_id")] public string RecordId { get; init; } = RecordId;
    [JsonPropertyName("record")] public IReadOnlyDictionary<string, object?> Values { get; init; } = Values;
}
