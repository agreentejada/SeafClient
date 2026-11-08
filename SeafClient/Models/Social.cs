using System.Text.Json.Serialization;

namespace SeafClient.Models;

/// <summary>A comment on a file (<c>/api/v2.1/repos/{id}/file/{uuid}/comments/</c>).</summary>
public sealed record SeafFileComment
{
    public int Id { get; init; }
    [JsonPropertyName("repo_id")] public string LibraryId { get; init; } = "";
    public string ParentPath { get; init; } = "";
    public string ItemName { get; init; } = "";
    public string Comment { get; init; } = "";

    /// <summary>Free-form data stored with the comment (the web UI keeps a JSON position marker here).</summary>
    public string? Detail { get; init; }

    public bool Resolved { get; init; }
    [JsonPropertyName("created_at")] public DateTimeOffset? Created { get; init; }
    [JsonPropertyName("updated_at")] public DateTimeOffset? Updated { get; init; }
    public string UserEmail { get; init; } = "";
    public string? UserName { get; init; }
    public string? UserContactEmail { get; init; }
    public string? AvatarUrl { get; init; }
    public IReadOnlyList<SeafCommentReply> Replies { get; init; } = [];
}

/// <summary>A reply to a file comment.</summary>
public sealed record SeafCommentReply
{
    public int Id { get; init; }
    public int CommentId { get; init; }

    /// <summary>The reply text.</summary>
    public string Reply { get; init; } = "";

    /// <summary><c>reply</c>, or <c>comment</c> for status changes recorded as replies.</summary>
    public string? Type { get; init; }

    public string Author { get; init; } = "";
    public string? UserName { get; init; }
    public string? UserContactEmail { get; init; }
    public string? AvatarUrl { get; init; }
    [JsonPropertyName("created_at")] public DateTimeOffset? Created { get; init; }
    [JsonPropertyName("updated_at")] public DateTimeOffset? Updated { get; init; }
}

/// <summary>A library-level tag of the legacy tag system (<c>/api/v2.1/repos/{id}/repo-tags/</c>).</summary>
public sealed record SeafRepoTag
{
    [JsonPropertyName("repo_tag_id")] public int Id { get; init; }
    [JsonPropertyName("repo_id")] public string LibraryId { get; init; } = "";
    [JsonPropertyName("tag_name")] public string Name { get; init; } = "";

    /// <summary>Hex color such as <c>#FF0000</c>.</summary>
    [JsonPropertyName("tag_color")] public string Color { get; init; } = "";

    /// <summary>Number of tagged files (only returned by the list call).</summary>
    public int? FilesCount { get; init; }
}

/// <summary>A tag attached to a file.</summary>
public sealed record SeafFileTag
{
    /// <summary>Id of the file-tag link; use it to remove the tag from the file.</summary>
    public int FileTagId { get; init; }
    public int RepoTagId { get; init; }
    public string? TagName { get; init; }
    public string? TagColor { get; init; }
}

/// <summary>A file carrying a given tag.</summary>
public sealed record SeafTaggedFile
{
    public int FileTagId { get; init; }
    public string ParentPath { get; init; } = "/";

    [JsonPropertyName("filename")] public string Name { get; init; } = "";
    public long Size { get; init; }
    [JsonPropertyName("mtime")] public DateTimeOffset? Modified { get; init; }
    public string? ModifierEmail { get; init; }
    public string? ModifierName { get; init; }
    public string? ModifierContactEmail { get; init; }

    [JsonIgnore] public string Path => SeafPath.Combine(ParentPath, Name);
}

/// <summary>A guest invitation (<c>/api/v2.1/invitations/</c>).</summary>
public sealed record SeafInvitation
{
    public int Id { get; init; }
    public string Token { get; init; } = "";
    public string Inviter { get; init; } = "";

    /// <summary>The invited email address.</summary>
    public string Accepter { get; init; } = "";

    /// <summary>Always <c>guest</c>.</summary>
    public string Type { get; init; } = "";

    [JsonPropertyName("invite_time")] public DateTimeOffset? Invited { get; init; }
    [JsonPropertyName("accept_time")] public DateTimeOffset? Accepted { get; init; }
    [JsonPropertyName("expire_time")] public DateTimeOffset? Expires { get; init; }
}

/// <summary>Result of inviting several people at once.</summary>
public sealed record SeafBulkInvitationResult
{
    public IReadOnlyList<SeafInvitation> Success { get; init; } = [];
    public IReadOnlyList<SeafBulkMemberFailure> Failed { get; init; } = [];
}
