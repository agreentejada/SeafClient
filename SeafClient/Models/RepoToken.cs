using System.Text.Json.Serialization;

namespace SeafClient.Models;

/// <summary>Permission of a repo API token.</summary>
public enum SeafRepoTokenPermission
{
    /// <summary>Read only.</summary>
    R,
    /// <summary>Read and write.</summary>
    Rw,
}

/// <summary>A library-scoped API token ("repo API token") used by <see cref="SeafRepoTokenSession"/>.</summary>
public sealed record SeafRepoApiToken
{
    [JsonPropertyName("repo_id")] public string LibraryId { get; init; } = "";

    /// <summary>Name of the application; unique per library and used to update/delete the token.</summary>
    public string AppName { get; init; } = "";

    public string? GeneratedBy { get; init; }
    public SeafRepoTokenPermission Permission { get; init; }

    /// <summary>The secret token value.</summary>
    public string ApiToken { get; init; } = "";
}

/// <summary>Library information returned to a repo API token (<c>/api/v2.1/via-repo-token/repo-info/</c>).</summary>
public sealed record SeafRepoInfo
{
    [JsonPropertyName("repo_id")] public string Id { get; init; } = "";
    [JsonPropertyName("repo_name")] public string Name { get; init; } = "";

    /// <summary>Total size in bytes.</summary>
    public long Size { get; init; }

    public long FileCount { get; init; }
    public DateTimeOffset? LastModified { get; init; }
}
