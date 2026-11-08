using System.Text.Json.Serialization;

namespace SeafClient.Models;

/// <summary>Which libraries to list, mirroring the <c>type</c> filter of <c>GET /api/v2.1/repos/</c>.</summary>
public enum SeafLibraryType
{
    /// <summary>Libraries owned by the current user.</summary>
    Mine,
    /// <summary>Libraries shared directly with the current user.</summary>
    Shared,
    /// <summary>Libraries shared with a group the current user belongs to.</summary>
    Group,
    /// <summary>Libraries shared with everyone on the server.</summary>
    Public,
}

/// <summary>A Seafile library ("repo").</summary>
public sealed record SeafLibrary
{
    [JsonPropertyName("repo_id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("repo_name")]
    public string Name { get; init; } = "";

    /// <summary>How the library is visible to the current user: <c>mine</c>, <c>shared</c>, <c>group</c> or <c>public</c>. Null on detail responses.</summary>
    public string? Type { get; init; }

    public string? OwnerEmail { get; init; }
    public string? OwnerName { get; init; }
    public DateTimeOffset? LastModified { get; init; }
    public string? ModifierEmail { get; init; }
    public string? ModifierName { get; init; }

    /// <summary>Size in bytes.</summary>
    public long Size { get; init; }

    public bool Encrypted { get; init; }

    /// <summary>The current user's permission, e.g. <c>rw</c> or <c>r</c>.</summary>
    public string? Permission { get; init; }

    public bool Starred { get; init; }
    public string? Status { get; init; }

    /// <summary>Only set for <see cref="SeafLibraryType.Group"/> entries.</summary>
    public int? GroupId { get; init; }

    /// <summary>Only set for <see cref="SeafLibraryType.Group"/> entries.</summary>
    public string? GroupName { get; init; }

    /// <summary>Only set by the library detail endpoint.</summary>
    public long? FileCount { get; init; }

    [JsonIgnore]
    public bool CanWrite => Permission == "rw";
}
