using System.Text.Json.Serialization;

namespace SeafClient.Models;

/// <summary>Who may open a share link.</summary>
public enum SeafShareLinkScope
{
    /// <summary>Anyone with the link (the default).</summary>
    AllUsers,

    /// <summary>Only the logged-in Seafile users listed with <see cref="Api.ShareLinksApi.AddAuthorizedUsersAsync"/>.</summary>
    SpecificUsers,

    /// <summary>Only people who verify one of the listed email addresses. Needs email (SMTP) configured on the server.</summary>
    SpecificEmails,
}

/// <summary>Changes for <see cref="Api.ShareLinksApi.UpdateAsync"/>; null properties are left unchanged.</summary>
public sealed class SeafShareLinkUpdate
{
    /// <summary>New absolute expiry. Takes precedence over <see cref="ExpireDays"/>.</summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>New expiry in whole days from now.</summary>
    public int? ExpireDays { get; set; }

    /// <summary>
    /// New permissions. When any of <see cref="CanEdit"/>, <see cref="CanDownload"/>, <see cref="CanUpload"/> is set,
    /// all three are sent, with unset ones taking Seafile's defaults (edit off, download on, upload off).
    /// </summary>
    public bool? CanEdit { get; set; }

    public bool? CanDownload { get; set; }

    /// <summary>Allow uploads through the link (directories only, needs rw permission on the folder).</summary>
    public bool? CanUpload { get; set; }

    /// <summary>
    /// New audience. Changing the scope clears the link's authorized user / email lists.
    /// </summary>
    public SeafShareLinkScope? Scope { get; set; }
}

/// <summary>A file or folder inside a shared folder link (<c>/api/v2.1/share-links/{token}/dirents/</c>).</summary>
public sealed record SeafShareLinkDirent
{
    public bool IsDir { get; init; }
    public long Size { get; init; }

    [JsonPropertyName("last_modified")] public DateTimeOffset? Modified { get; init; }

    public string? FileName { get; init; }
    public string? FilePath { get; init; }
    public string? FolderName { get; init; }
    public string? FolderPath { get; init; }

    /// <summary>Entry name.</summary>
    [JsonIgnore] public string Name => (IsDir ? FolderName : FileName) ?? "";

    /// <summary>Path relative to the shared folder (not the library), without a trailing '/'.</summary>
    [JsonIgnore] public string Path => SeafPath.Normalize((IsDir ? FolderPath : FilePath) ?? "/");
}

/// <summary>A Seafile user allowed to open a <see cref="SeafShareLinkScope.SpecificUsers"/> link.</summary>
public sealed record SeafShareLinkAuthorizedUser
{
    /// <summary>Internal user id.</summary>
    public string Username { get; init; } = "";

    public string Name { get; init; } = "";
    public string? ContactEmail { get; init; }
    public string? AvatarUrl { get; init; }
    public string? LinkToken { get; init; }
}

/// <summary>Result of adding authorized users to a share link.</summary>
public sealed record SeafShareLinkAuthorizedUsersResult
{
    public IReadOnlyList<SeafShareLinkAuthorizedUser> Success { get; init; } = [];
    public IReadOnlyList<SeafShareLinkAuthFailure> Failed { get; init; } = [];
}

/// <summary>Result of adding authorized emails to a share link.</summary>
public sealed record SeafShareLinkAuthorizedEmailsResult
{
    public IReadOnlyList<string> Success { get; init; } = [];
    public IReadOnlyList<SeafShareLinkAuthFailure> Failed { get; init; } = [];
}

public sealed record SeafShareLinkAuthFailure
{
    public string Email { get; init; } = "";
    public string? Name { get; init; }

    /// <summary>For example "User x does not exist." or "Email x already exists."</summary>
    public string ErrorMsg { get; init; } = "";
}
