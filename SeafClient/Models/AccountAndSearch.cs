using System.Text.Json.Serialization;

namespace SeafClient.Models;

/// <summary>The current user's profile (<c>/api/v2.1/user/</c>).</summary>
public sealed record SeafUserProfile
{
    public string Email { get; init; } = "";
    public string Name { get; init; } = "";
    public string? ContactEmail { get; init; }
    public string? Telephone { get; init; }
    public string? LoginId { get; init; }
    public bool ListInAddressBook { get; init; }
}

/// <summary>Profile fields to change; null fields are left untouched.</summary>
/// <remarks>Seafile ignores empty strings, so a field cannot be cleared through this API once set.</remarks>
public sealed class SeafProfileUpdate
{
    /// <summary>Display name (max 64 characters, no '/').</summary>
    public string? Name { get; set; }
    public string? ContactEmail { get; set; }
    public string? Telephone { get; set; }
    public bool? ListInAddressBook { get; set; }
}

/// <summary>A desktop or mobile client linked to the account (<c>/api2/devices/</c>).</summary>
public sealed record SeafDevice
{
    /// <summary>The device's API token key.</summary>
    public string? Key { get; init; }
    public string? User { get; init; }

    /// <summary>e.g. <c>windows</c>, <c>mac</c>, <c>linux</c>, <c>android</c>, <c>ios</c>. Needed to unlink the device.</summary>
    public string Platform { get; init; } = "";

    /// <summary>Needed to unlink the device.</summary>
    public string DeviceId { get; init; } = "";

    public string? DeviceName { get; init; }
    public string? ClientVersion { get; init; }
    public string? PlatformVersion { get; init; }
    public DateTimeOffset? LastAccessed { get; init; }
    public string? LastLoginIp { get; init; }
    public DateTimeOffset? WipedAt { get; init; }
    public bool IsDesktopClient { get; init; }
}

/// <summary>An entry of the activity feed (<c>/api/v2.1/activities/</c>). Seafile Pro.</summary>
public sealed record SeafActivity
{
    /// <summary>e.g. <c>create</c>, <c>edit</c>, <c>rename</c>, <c>move</c>, <c>delete</c>, <c>recover</c>, <c>batch_create</c>.</summary>
    public string OpType { get; init; } = "";

    /// <summary><c>file</c>, <c>dir</c> or <c>repo</c>.</summary>
    public string ObjType { get; init; } = "";

    [JsonPropertyName("repo_id")] public string LibraryId { get; init; } = "";
    [JsonPropertyName("repo_name")] public string? LibraryName { get; init; }
    public string? CommitId { get; init; }
    public string Path { get; init; } = "";
    public string Name { get; init; } = "";

    /// <summary>The previous path, for renames and moves.</summary>
    public string? OldPath { get; init; }
    public string? OldName { get; init; }

    public string AuthorEmail { get; init; } = "";
    public string? AuthorName { get; init; }
    public string? AuthorContactEmail { get; init; }
    public string? AvatarUrl { get; init; }
    public DateTimeOffset? Time { get; init; }

    /// <summary>Number of items in a batch operation.</summary>
    public int? Count { get; init; }
}

/// <summary>A user returned by the user search (<c>/api2/search-user/</c>).</summary>
public sealed record SeafUserSearchResult
{
    public string Email { get; init; } = "";
    public string Name { get; init; } = "";
    public string? ContactEmail { get; init; }
    public string? AvatarUrl { get; init; }
}

/// <summary>A file found by name in one library (<c>/api/v2.1/search-file/</c>).</summary>
public sealed record SeafFileSearchHit
{
    public string Path { get; init; } = "";
    public SeafEntryType Type { get; init; }
    public long Size { get; init; }
    [JsonPropertyName("mtime")] public DateTimeOffset? Modified { get; init; }

    [JsonIgnore] public string Name => SeafPath.GetName(Path);
}

/// <summary>Which libraries a full-text search covers.</summary>
public enum SeafSearchScope { All, Mine, Shared, Group, Public }

/// <summary>Parameters of a full-text file search.</summary>
public sealed class SeafSearchQuery
{
    public required string Query { get; set; }

    /// <summary>Limit to one library. Overrides <see cref="Scope"/>.</summary>
    public string? LibraryId { get; set; }

    /// <summary>Limit to a folder inside <see cref="LibraryId"/>.</summary>
    public string? Path { get; set; }

    public SeafSearchScope Scope { get; set; } = SeafSearchScope.All;

    /// <summary>Match file names only, not file content.</summary>
    public bool FileNamesOnly { get; set; }

    /// <summary>Restrict to files or to directories.</summary>
    public SeafEntryType? EntryType { get; set; }

    /// <summary>File types such as <c>Text</c>, <c>Document</c>, <c>Image</c>, <c>Video</c>, <c>Audio</c>, <c>PDF</c>, <c>Markdown</c>.</summary>
    public IReadOnlyList<string>? FileTypes { get; set; }

    /// <summary>File extensions without the dot.</summary>
    public IReadOnlyList<string>? Extensions { get; set; }

    public DateTimeOffset? ModifiedFrom { get; set; }
    public DateTimeOffset? ModifiedTo { get; set; }

    /// <summary>Minimum size in bytes.</summary>
    public long? SizeFrom { get; set; }

    /// <summary>Maximum size in bytes.</summary>
    public long? SizeTo { get; set; }

    public int Page { get; set; } = 1;
    public int PerPage { get; set; } = 10;
}

/// <summary>One page of full-text search results.</summary>
public sealed record SeafSearchResult
{
    public int Total { get; init; }
    public bool HasMore { get; init; }
    public IReadOnlyList<SeafSearchHit> Results { get; init; } = [];
}

public sealed record SeafSearchHit
{
    [JsonPropertyName("repo_id")] public string LibraryId { get; init; } = "";
    [JsonPropertyName("repo_name")] public string? LibraryName { get; init; }
    public string Name { get; init; } = "";

    [JsonPropertyName("fullpath")] public string Path { get; init; } = "";
    public bool IsDir { get; init; }
    public long Size { get; init; }
    [JsonPropertyName("last_modified")] public DateTimeOffset? Modified { get; init; }

    /// <summary>Matching text with highlights, for content matches.</summary>
    public string? ContentHighlight { get; init; }

    public bool RepoEncrypted { get; init; }
    public string? RepoOwnerEmail { get; init; }
    public string? RepoOwnerName { get; init; }
}
