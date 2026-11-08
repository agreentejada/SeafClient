using System.Text.Json.Serialization;

namespace SeafClient.Models;

public enum SeafEntryType
{
    File,
    Dir,
}

/// <summary>Which entries a directory listing returns.</summary>
public enum SeafListFilter
{
    All,
    FilesOnly,
    DirectoriesOnly,
}

/// <summary>An entry of a directory listing (<c>GET /api/v2.1/repos/{id}/dir/</c>).</summary>
public sealed record SeafDirEntry
{
    public SeafEntryType Type { get; init; }

    /// <summary>Object id (content hash). Directories that are empty share the all-zero id.</summary>
    public string Id { get; init; } = "";

    public string Name { get; init; } = "";

    /// <summary>The containing directory as reported by the server (may or may not have a trailing '/').</summary>
    public string ParentDir { get; init; } = "/";

    [JsonPropertyName("mtime")]
    public DateTimeOffset? Modified { get; init; }

    /// <summary>Size in bytes; only reported for files.</summary>
    public long? Size { get; init; }

    public string? Permission { get; init; }
    public bool Starred { get; init; }
    public string? ModifierEmail { get; init; }
    public string? ModifierName { get; init; }
    public bool IsLocked { get; init; }
    public string? LockOwner { get; init; }

    /// <summary>The library the entry belongs to (filled in by the client).</summary>
    [JsonIgnore]
    public string LibraryId { get; init; } = "";

    /// <summary>Normalized full path within the library, e.g. <c>/a/b/file.txt</c>.</summary>
    [JsonIgnore]
    public string Path => SeafPath.Combine(ParentDir, Name);

    [JsonIgnore] public bool IsFile => Type == SeafEntryType.File;
    [JsonIgnore] public bool IsDirectory => Type == SeafEntryType.Dir;
}

/// <summary>Directory details (<c>GET /api/v2.1/repos/{id}/dir/detail/</c>).</summary>
public sealed record SeafDirectory
{
    [JsonPropertyName("repo_id")]
    public string LibraryId { get; init; } = "";

    [JsonIgnore]
    private string _path = "/";

    /// <summary>Normalized path, e.g. <c>/a/b</c>.</summary>
    public string Path { get => _path; init => _path = SeafPath.Normalize(value); }

    public string Name { get; init; } = "";

    [JsonPropertyName("mtime")]
    public DateTimeOffset? Modified { get; init; }

    public string? Permission { get; init; }
}

/// <summary>File details (<c>GET /api2/repos/{id}/file/detail/</c>).</summary>
public sealed record SeafFile
{
    /// <summary>Object id (content hash).</summary>
    public string Id { get; init; } = "";

    public string Name { get; init; } = "";

    /// <summary>Size in bytes.</summary>
    public long Size { get; init; }

    [JsonPropertyName("mtime")]
    public DateTimeOffset? Modified { get; init; }

    public string? LastModifierEmail { get; init; }
    public string? LastModifierName { get; init; }
    public string? Permission { get; init; }
    public bool Starred { get; init; }
    public bool CanEdit { get; init; }
    public bool IsLocked { get; init; }

    /// <summary>The library the file belongs to (filled in by the client).</summary>
    [JsonIgnore]
    public string LibraryId { get; init; } = "";

    /// <summary>Normalized full path within the library (filled in by the client).</summary>
    [JsonIgnore]
    public string Path { get; init; } = "";

    [JsonIgnore]
    public string ParentDir => SeafPath.GetParent(Path);
}

/// <summary>Result of an upload: the stored name may differ from the requested one if Seafile auto-renamed it (e.g. <c>a (1).txt</c>).</summary>
public sealed record SeafUploadedFile
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public long Size { get; init; }

    [JsonIgnore] public string LibraryId { get; init; } = "";
    [JsonIgnore] public string Path { get; init; } = "";
}

/// <summary>An entry from a file's history (<c>GET /api/v2.1/repos/{id}/file/new_history/</c>).</summary>
public sealed record SeafFileRevision
{
    public string CommitId { get; init; } = "";
    public string RevFileId { get; init; } = "";
    public string Path { get; init; } = "";
    public string? OldPath { get; init; }
    public long Size { get; init; }

    /// <summary>e.g. <c>create</c>, <c>edit</c>, <c>rename</c>, <c>move</c>.</summary>
    public string? OpType { get; init; }

    [JsonPropertyName("ctime")]
    public DateTimeOffset? Created { get; init; }

    public string? CreatorEmail { get; init; }
    public string? CreatorName { get; init; }
}

/// <summary>A starred file or folder.</summary>
public sealed record SeafStarredItem
{
    [JsonPropertyName("repo_id")] public string LibraryId { get; init; } = "";
    [JsonPropertyName("repo_name")] public string LibraryName { get; init; } = "";
    public string Path { get; init; } = "";
    public string ObjName { get; init; } = "";
    public bool IsDir { get; init; }
    [JsonPropertyName("mtime")] public DateTimeOffset? Modified { get; init; }
    public bool Deleted { get; init; }
}

/// <summary>Options for <see cref="Api.FilesApi.UploadAsync"/>.</summary>
public sealed class SeafUploadOptions
{
    /// <summary>Overwrite an existing file with the same name. When false (default) Seafile stores the upload under a new name like <c>a (1).txt</c>.</summary>
    public bool Replace { get; set; }

    /// <summary>Create missing parent directories.</summary>
    public bool CreateParents { get; set; }

    /// <summary>Receives the number of bytes sent so far.</summary>
    public IProgress<long>? Progress { get; set; }

    /// <summary>Content type to send for the file part. Defaults to application/octet-stream.</summary>
    public string? ContentType { get; set; }
}

/// <summary>Internal shape of the v2.1 file/dir operation responses (rename/copy/move/create).</summary>
internal sealed record SeafOperationResult
{
    public string Type { get; init; } = "";
    public string RepoId { get; init; } = "";
    public string ParentDir { get; init; } = "/";
    public string ObjName { get; init; } = "";
    public string ObjId { get; init; } = "";
    public long Size { get; init; }
    public DateTimeOffset? Mtime { get; init; }
    public bool IsLocked { get; init; }
    public bool CanEdit { get; init; }

    public SeafFile ToFile() => new()
    {
        Id = ObjId, Name = ObjName, Size = Size, Modified = Mtime, IsLocked = IsLocked, CanEdit = CanEdit,
        LibraryId = RepoId, Path = SeafPath.Combine(ParentDir, ObjName),
    };

    public SeafDirectory ToDirectory() => new()
    {
        LibraryId = RepoId, Path = SeafPath.Combine(ParentDir, ObjName), Name = ObjName, Modified = Mtime,
    };
}
