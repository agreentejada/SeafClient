
using System.Text.Json.Serialization;

namespace SeafClient.Models;

/// <summary>A commit in a library's history (<c>GET /api/v2.1/repos/{id}/history/</c>).</summary>
public sealed record SeafCommit
{
    public string CommitId { get; init; } = "";
    public string Description { get; init; } = "";

    /// <summary>Internal user id of the author.</summary>
    public string? Email { get; init; }

    public string? Name { get; init; }
    public string? ContactEmail { get; init; }
    public DateTimeOffset? Time { get; init; }
    public string? ClientVersion { get; init; }
    public string? DeviceName { get; init; }

    /// <summary>Set for merge commits (e.g. from desktop-client syncs).</summary>
    public string? SecondParentId { get; init; }

    /// <summary>Snapshot labels attached to this commit.</summary>
    /// <remarks>Seafile 14 always returns an empty list here even when labels exist; use the label APIs instead.</remarks>
    public IReadOnlyList<string> Tags { get; init; } = [];
}

/// <summary>A page of library history.</summary>
public sealed record SeafHistoryPage
{
    public IReadOnlyList<SeafCommit> Items { get; init; } = [];

    /// <summary>True when older commits exist beyond this page.</summary>
    public bool HasMore { get; init; }
}

/// <summary>Details of one commit, including what it changed.</summary>
public sealed record SeafCommitDetail
{
    [JsonPropertyName("commit_info")] public SeafCommitInfo Info { get; init; } = new();
    [JsonPropertyName("commit_diffs")] public IReadOnlyList<SeafCommitDiff> Changes { get; init; } = [];
}

public sealed record SeafCommitInfo
{
    public string Description { get; init; } = "";
    public string? CreatorEmail { get; init; }
    public string? CreatorName { get; init; }
    public string? CreatorContactEmail { get; init; }
    public DateTimeOffset? Time { get; init; }
    public string? DeviceName { get; init; }
}

/// <summary>One change made by a commit.</summary>
public sealed record SeafCommitDiff
{
    /// <summary><c>new</c>, <c>removed</c>, <c>renamed</c>, <c>modified</c>, <c>newdir</c> or <c>deldir</c>.</summary>
    public string OpType { get; init; } = "";

    public string Path { get; init; } = "";

    /// <summary>Destination of a rename/move; empty otherwise.</summary>
    public string? NewPath { get; init; }
}

/// <summary>An entry of a directory as it was at a given commit (snapshot).</summary>
public sealed record SeafSnapshotEntry
{
    public SeafEntryType Type { get; init; }
    public string Name { get; init; } = "";
    public string ParentDir { get; init; } = "/";

    /// <summary>Object id (content hash) at that commit.</summary>
    public string ObjId { get; init; } = "";

    /// <summary>Size in bytes; files only.</summary>
    public long? Size { get; init; }

    [JsonIgnore] public string Path => SeafPath.Combine(ParentDir, Name);
    [JsonIgnore] public bool IsFile => Type == SeafEntryType.File;
    [JsonIgnore] public bool IsDirectory => Type == SeafEntryType.Dir;
}

/// <summary>A deleted file or folder in a library's trash.</summary>
public sealed record SeafTrashItem
{
    public string ParentDir { get; init; } = "/";
    public string ObjName { get; init; } = "";
    public string? ObjId { get; init; }
    public bool IsDir { get; init; }

    /// <summary>Size in bytes; null for directories (the server sends an empty string).</summary>
    [JsonConverter(typeof(Internal.EmptyStringNullableLongConverter))]
    public long? Size { get; init; }

    [JsonPropertyName("deleted_time")] public DateTimeOffset? Deleted { get; init; }

    /// <summary>The commit in which the item still existed; pass it to <see cref="Api.LibrariesApi.RestoreFromTrashAsync(string, SeafTrashItem, CancellationToken)"/>.</summary>
    public string CommitId { get; init; } = "";

    /// <summary>Full path the item had before it was deleted.</summary>
    [JsonIgnore] public string Path => SeafPath.Combine(ParentDir, ObjName);
}

/// <summary>A batch of trash entries. The server scans history incrementally; pass <see cref="ScanStat"/> back to continue.</summary>
public sealed record SeafTrashPage
{
    public IReadOnlyList<SeafTrashItem> Items { get; init; } = [];
    public bool HasMore { get; init; }

    /// <summary>Opaque cursor for the next batch, when <see cref="HasMore"/> is true.</summary>
    public string? ScanStat { get; init; }
}

/// <summary>Result of restoring items from the trash; per-item failures are reported rather than thrown.</summary>
public sealed record SeafRestoreResult
{
    public IReadOnlyList<SeafRestoredItem> Success { get; init; } = [];
    public IReadOnlyList<SeafRestoreFailure> Failed { get; init; } = [];
}

public sealed record SeafRestoredItem
{
    public string Path { get; init; } = "";
    public bool IsDir { get; init; }
}

public sealed record SeafRestoreFailure
{
    public string Path { get; init; } = "";
    public string ErrorMsg { get; init; } = "";
}

/// <summary>A snapshot label ("revision tag") attached to a library commit.</summary>
public sealed record SeafSnapshotLabel
{
    [JsonPropertyName("tag")] public string Name { get; init; } = "";
    [JsonPropertyName("tag_creator")] public string? Creator { get; init; }
    public SeafSnapshotLabelRevision Revision { get; init; } = new();
}

public sealed record SeafSnapshotLabelRevision
{
    [JsonPropertyName("repo_id")] public string LibraryId { get; init; } = "";
    public string CommitId { get; init; } = "";
    public string? Email { get; init; }
    public string? Name { get; init; }
    public string? ContactEmail { get; init; }
    public DateTimeOffset? Time { get; init; }
    public string? Description { get; init; }

    /// <summary>Server-relative URL of the snapshot view page.</summary>
    public string? Link { get; init; }
}

/// <summary>A library someone else shared with the current user (<c>GET /api2/beshared-repos/</c>).</summary>
public sealed record SeafSharedLibrary
{
    [JsonPropertyName("repo_id")] public string Id { get; init; } = "";
    [JsonPropertyName("repo_name")] public string Name { get; init; } = "";
    [JsonPropertyName("repo_desc")] public string? Description { get; init; }

    /// <summary><c>personal</c> (shared to the user), <c>group</c> or <c>public</c>.</summary>
    public string ShareType { get; init; } = "";

    public string Permission { get; init; } = "";

    /// <summary>The user who shared the library (normally its owner).</summary>
    [JsonPropertyName("user")] public string SharedBy { get; init; } = "";

    /// <summary>Set when <see cref="ShareType"/> is <c>group</c>.</summary>
    public int GroupId { get; init; }

    public string? GroupName { get; init; }
    public DateTimeOffset? LastModified { get; init; }
    public long Size { get; init; }
    public bool Encrypted { get; init; }

    /// <summary>True for a shared sub-folder (a "virtual" library).</summary>
    public bool IsVirtual { get; init; }

    public string? OriginRepoId { get; init; }
    public string? OriginPath { get; init; }
}

