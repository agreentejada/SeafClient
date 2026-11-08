using System.Text.Json.Serialization;

namespace SeafClient.Models;

/// <summary>A folder (not a whole library) that the current user has shared with a user or group (<c>/api/v2.1/shared-folders/</c>).</summary>
public sealed record SeafSharedFolder
{
    [JsonPropertyName("repo_id")] public string LibraryId { get; init; } = "";
    [JsonPropertyName("repo_name")] public string LibraryName { get; init; } = "";

    /// <summary>Path of the shared folder inside the library.</summary>
    public string Path { get; init; } = "";

    public string FolderName { get; init; } = "";

    /// <summary><c>personal</c> (shared with a user) or <c>group</c>.</summary>
    public string ShareType { get; init; } = "";

    /// <summary><c>r</c>, <c>rw</c>, <c>admin</c> or a custom permission id.</summary>
    public string SharePermission { get; init; } = "";

    /// <summary>Display name of a custom permission; empty for built-in permissions.</summary>
    public string? SharePermissionName { get; init; }

    /// <summary>Display name of the user (personal shares).</summary>
    public string? UserName { get; init; }

    /// <summary>Internal user id (personal shares).</summary>
    public string? UserEmail { get; init; }

    public string? ContactEmail { get; init; }
    public int? GroupId { get; init; }
    public string? GroupName { get; init; }

    [JsonIgnore] public bool IsGroupShare => ShareType == "group";
}

/// <summary>Result of sharing a library or folder with several users or groups at once.</summary>
public sealed record SeafShareResult
{
    public IReadOnlyList<SeafSharedItem> Success { get; init; } = [];
    public IReadOnlyList<SeafShareFailure> Failed { get; init; } = [];

    /// <summary>
    /// When a sub-folder is shared, Seafile creates a virtual library for it; this is its id. Null for whole-library shares.
    /// </summary>
    [JsonPropertyName("virtual_repo_id")] public string? VirtualLibraryId { get; init; }
}

/// <summary>One recipient that could not be shared with.</summary>
public sealed record SeafShareFailure
{
    /// <summary>The user, for user shares.</summary>
    public string? Email { get; init; }

    /// <summary>The group name, for group shares.</summary>
    public string? GroupName { get; init; }

    public string ErrorMsg { get; init; } = "";
}

/// <summary>Result of sharing / unsharing several libraries at once (<c>/api/v2.1/repos/batch/</c>).</summary>
public sealed record SeafBatchShareResult
{
    public IReadOnlyList<SeafBatchShareSuccess> Success { get; init; } = [];
    public IReadOnlyList<SeafBatchShareFailure> Failed { get; init; } = [];
}

public sealed record SeafBatchShareSuccess
{
    [JsonPropertyName("repo_id")] public string LibraryId { get; init; } = "";

    /// <summary>The user, for user shares.</summary>
    public string? Username { get; init; }

    public int? GroupId { get; init; }
    public string? GroupName { get; init; }

    /// <summary>The granted permission (share operations only).</summary>
    public string? Permission { get; init; }
}

public sealed record SeafBatchShareFailure
{
    [JsonPropertyName("repo_id")] public string LibraryId { get; init; } = "";
    public string ErrorMsg { get; init; } = "";
}

/// <summary>
/// A sub-folder permission (Seafile Pro): overrides the permission a user or group has on one folder of a library
/// they can access through a share.
/// </summary>
public sealed record SeafFolderPermission
{
    [JsonPropertyName("repo_id")] public string LibraryId { get; init; } = "";
    public string FolderPath { get; init; } = "";
    public string FolderName { get; init; } = "";

    /// <summary><c>r</c> or <c>rw</c> (or a custom permission id).</summary>
    public string Permission { get; init; } = "";

    /// <summary>Set for user permissions.</summary>
    public string? UserEmail { get; init; }

    /// <summary>Display name, for user permissions.</summary>
    public string? UserName { get; init; }

    /// <summary>Set for group permissions.</summary>
    public int? GroupId { get; init; }

    public string? GroupName { get; init; }
}

/// <summary>Result of adding a folder permission for several users or groups.</summary>
public sealed record SeafFolderPermissionResult
{
    public IReadOnlyList<SeafFolderPermission> Success { get; init; } = [];
    public IReadOnlyList<SeafFolderPermissionFailure> Failed { get; init; } = [];
}

public sealed record SeafFolderPermissionFailure
{
    public string? UserEmail { get; init; }
    public int? GroupId { get; init; }

    /// <summary>For example "Permission already exists." or "User ... not found."</summary>
    public string ErrorMsg { get; init; } = "";
}
