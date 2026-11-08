using System.Text.Json.Serialization;

namespace SeafClient.Models;

/// <summary>A public upload link: lets anyone with the link (and password, if set) upload files into a folder.</summary>
public sealed record SeafUploadLink
{
    /// <summary>The link token; use it to fetch, update, delete or upload through the link.</summary>
    public string Token { get; init; } = "";

    /// <summary>The page URL to share, e.g. <c>https://host/u/d/abc123/</c>.</summary>
    public string Link { get; init; } = "";

    [JsonPropertyName("repo_id")] public string LibraryId { get; init; } = "";
    [JsonPropertyName("repo_name")] public string? LibraryName { get; init; }

    /// <summary>Target folder. Seafile returns it with a trailing '/'.</summary>
    public string Path { get; init; } = "";

    public string ObjName { get; init; } = "";
    public string? Username { get; init; }

    [JsonPropertyName("ctime")] public DateTimeOffset? Created { get; init; }

    /// <summary>Null when the link never expires.</summary>
    [JsonPropertyName("expire_date")] public DateTimeOffset? ExpiresAt { get; init; }

    public bool IsExpired { get; init; }

    [JsonPropertyName("view_cnt")] public int ViewCount { get; init; }

    /// <summary>The link password, returned to the link owner only. Empty when the link has none.</summary>
    public string? Password { get; init; }

    public string? Description { get; init; }

    [JsonIgnore]
    public bool IsExpiredNow => IsExpired || (ExpiresAt is { } e && e <= DateTimeOffset.UtcNow);

    [JsonIgnore]
    public bool HasPassword => !string.IsNullOrEmpty(Password);
}

/// <summary>Options for <see cref="Api.UploadLinksApi.CreateAsync"/>.</summary>
public sealed class SeafUploadLinkOptions
{
    /// <summary>Optional link password. Seafile enforces a minimum length (8 by default).</summary>
    public string? Password { get; set; }

    /// <summary>Absolute expiry. Takes precedence over <see cref="ExpireDays"/>.</summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>Expiry in whole days from now.</summary>
    public int? ExpireDays { get; set; }
}

/// <summary>Result of emailing a share or upload link.</summary>
public sealed record SeafSendLinkResult
{
    /// <summary>Addresses the email was sent to.</summary>
    public IReadOnlyList<string> Success { get; init; } = [];

    public IReadOnlyList<SeafSendLinkFailure> Failed { get; init; } = [];
}

public sealed record SeafSendLinkFailure
{
    public string Email { get; init; } = "";
    public string ErrorMsg { get; init; } = "";
}
