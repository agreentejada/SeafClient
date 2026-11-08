using System.Globalization;
using System.Text.Json;
using SeafClient.Internal;
using SeafClient.Models;

namespace SeafClient.Api;

// Updating links, multiple links per item, batch creation, emailing, browsing and per-link audiences.
public sealed partial class ShareLinksApi
{
    /// <summary>Changes a share link's expiry, permissions and/or audience. Returns the updated link.</summary>
    public Task<SeafShareLink> UpdateAsync(string token, SeafShareLinkUpdate update, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(update);
        var setsPermissions = update.CanEdit != null || update.CanDownload != null || update.CanUpload != null;
        return _t.SendAsync<SeafShareLink>(HttpMethod.Put, $"api/v2.1/share-links/{Url.Seg(token)}/", SeafTransport.Json(new
        {
            expiration_time = IsoTime(update.ExpiresAt),
            expire_days = update.ExpiresAt == null ? update.ExpireDays : null,
            permissions = setsPermissions
                ? new { can_edit = update.CanEdit ?? false, can_download = update.CanDownload ?? true, can_upload = update.CanUpload ?? false }
                : null,
            user_scope = ScopeValue(update.Scope),
        }), ct);
    }

    /// <summary>
    /// Creates an additional share link for an item. Unlike <see cref="CreateAsync"/>, which fails when the item already
    /// has a link, this always creates a new one, and can restrict the link to specific users or email addresses.
    /// </summary>
    /// <param name="scope">Audience; defaults to anyone with the link.</param>
    /// <param name="authorized">
    /// Seafile user ids for <see cref="SeafShareLinkScope.SpecificUsers"/> (unknown users are silently dropped), or
    /// email addresses for <see cref="SeafShareLinkScope.SpecificEmails"/> (each is sent an email).
    /// </param>
    public Task<SeafShareLink> CreateAdditionalAsync(string libraryId, string path, SeafShareLinkOptions? options = null,
        SeafShareLinkScope scope = SeafShareLinkScope.AllUsers, IEnumerable<string>? authorized = null, CancellationToken ct = default)
    {
        options ??= new SeafShareLinkOptions();
        var list = authorized?.ToList() ?? [];
        // The server reads specific_users as a JSON array but specific_emails as a comma-separated string.
        object? emails = scope switch
        {
            SeafShareLinkScope.SpecificUsers => list,
            SeafShareLinkScope.SpecificEmails => string.Join(',', list),
            _ => null,
        };
        return _t.SendAsync<SeafShareLink>(HttpMethod.Post, "api/v2.1/multi-share-links/", SeafTransport.Json(new
        {
            repo_id = libraryId,
            path = SeafPath.Normalize(path),
            password = options.Password,
            expiration_time = IsoTime(options.ExpiresAt),
            expire_days = options.ExpiresAt == null ? options.ExpireDays : null,
            permissions = new { can_edit = options.CanEdit, can_download = options.CanDownload, can_upload = options.CanUpload },
            user_scope = ScopeValue(scope),
            emails,
        }), ct);
    }

    /// <summary>
    /// Creates <paramref name="count"/> new share links for one item at once, e.g. to hand out a separate link per recipient.
    /// With <paramref name="autoGeneratePassword"/>, each link gets its own random password (returned in <see cref="SeafShareLink.Password"/>).
    /// </summary>
    public async Task<IReadOnlyList<SeafShareLink>> CreateBatchAsync(string libraryId, string path, int count, bool autoGeneratePassword,
        SeafShareLinkOptions? options = null, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        options ??= new SeafShareLinkOptions();
        var permissions = JsonSerializer.Serialize(new { can_edit = options.CanEdit, can_download = options.CanDownload, can_upload = options.CanUpload });
        return await _t.SendAsync<List<SeafShareLink>>(HttpMethod.Post, "api/v2.1/multi-share-links/batch/", SeafTransport.Form(
            ("repo_id", libraryId),
            ("path", SeafPath.Normalize(path)),
            ("number", count.ToString(CultureInfo.InvariantCulture)),
            ("auto_generate_password", autoGeneratePassword ? "true" : "false"),
            ("expiration_time", IsoTime(options.ExpiresAt)),
            ("expire_days", options.ExpiresAt == null ? options.ExpireDays?.ToString(CultureInfo.InvariantCulture) : null),
            ("permissions", permissions)), ct).ConfigureAwait(false);
    }

    /// <summary>Deletes several share links. Unknown tokens are ignored.</summary>
    public Task DeleteManyAsync(IEnumerable<string> tokens, CancellationToken ct = default)
        => _t.SendAsync(HttpMethod.Delete, "api/v2.1/share-links/", SeafTransport.Json(new { tokens = tokens.ToList() }), ct);

    /// <summary>Emails a share link to one or more addresses. Fails with 403 if the server has no email service configured.</summary>
    public Task<SeafSendLinkResult> SendAsync(string token, IEnumerable<string> emails, string? extraMessage = null, CancellationToken ct = default)
        => _t.SendAsync<SeafSendLinkResult>(HttpMethod.Post, "api2/send-share-link/", SeafTransport.Form(
            ("token", token), ("email", string.Join(',', emails)), ("extra_msg", extraMessage)), ct);

    /// <summary>
    /// Lists the entries of a folder share link, as a visitor sees them. <paramref name="path"/> is relative to the shared
    /// folder. Fails with 403 for password-protected links.
    /// </summary>
    public async Task<IReadOnlyList<SeafShareLinkDirent>> ListDirentsAsync(string token, string path = "/", CancellationToken ct = default)
        => (await _t.GetAsync<DirentsResponse>(Url.Of($"api/v2.1/share-links/{Url.Seg(token)}/dirents/").Q("path", SeafPath.Normalize(path)), ct).ConfigureAwait(false)).DirentList;

    // ---- audiences: specific users ----

    /// <summary>Lists users allowed to open a <see cref="SeafShareLinkScope.SpecificUsers"/> link.</summary>
    /// <param name="path">The link's path; looked up when omitted.</param>
    public async Task<IReadOnlyList<SeafShareLinkAuthorizedUser>> ListAuthorizedUsersAsync(string token, string? path = null, CancellationToken ct = default)
        => (await _t.GetAsync<AuthListResponse<SeafShareLinkAuthorizedUser>>(await AuthUrl(token, "user-auth", path, ct).ConfigureAwait(false), ct).ConfigureAwait(false)).AuthList;

    /// <summary>Allows more users to open a <see cref="SeafShareLinkScope.SpecificUsers"/> link.</summary>
    public async Task<SeafShareLinkAuthorizedUsersResult> AddAuthorizedUsersAsync(string token, IEnumerable<string> userEmails, string? path = null, CancellationToken ct = default)
        => await _t.SendAsync<SeafShareLinkAuthorizedUsersResult>(HttpMethod.Post, await AuthUrl(token, "user-auth", path, ct).ConfigureAwait(false),
            SeafTransport.Json(new { emails = userEmails.ToList() }), ct).ConfigureAwait(false);

    public async Task RemoveAuthorizedUsersAsync(string token, IEnumerable<string> userEmails, string? path = null, CancellationToken ct = default)
        => await _t.SendAsync(HttpMethod.Delete, await AuthUrl(token, "user-auth", path, ct).ConfigureAwait(false),
            SeafTransport.Json(new { emails = userEmails.ToList() }), ct).ConfigureAwait(false);

    // ---- audiences: specific emails (needs SMTP; otherwise the server answers 400 "feature is not enabled.") ----

    /// <summary>Lists email addresses allowed to open a <see cref="SeafShareLinkScope.SpecificEmails"/> link.</summary>
    public async Task<IReadOnlyList<string>> ListAuthorizedEmailsAsync(string token, string? path = null, CancellationToken ct = default)
        => (await _t.GetAsync<AuthListResponse<string>>(await AuthUrl(token, "email-auth", path, ct).ConfigureAwait(false), ct).ConfigureAwait(false)).AuthList;

    /// <summary>Allows more email addresses to open a <see cref="SeafShareLinkScope.SpecificEmails"/> link; each new address is emailed the link.</summary>
    public async Task<SeafShareLinkAuthorizedEmailsResult> AddAuthorizedEmailsAsync(string token, IEnumerable<string> emails, string? path = null, CancellationToken ct = default)
    {
        // Unlike user-auth, this endpoint expects a comma-separated string and reports successes as {"email": ...} objects.
        var r = await _t.SendAsync<EmailAuthResult>(HttpMethod.Post, await AuthUrl(token, "email-auth", path, ct).ConfigureAwait(false),
            SeafTransport.Json(new { emails = string.Join(',', emails) }), ct).ConfigureAwait(false);
        return new SeafShareLinkAuthorizedEmailsResult { Success = r.Success.Select(s => s.Email).ToList(), Failed = r.Failed };
    }

    public async Task RemoveAuthorizedEmailsAsync(string token, IEnumerable<string> emails, string? path = null, CancellationToken ct = default)
        => await _t.SendAsync(HttpMethod.Delete, await AuthUrl(token, "email-auth", path, ct).ConfigureAwait(false),
            SeafTransport.Json(new { emails = emails.ToList() }), ct).ConfigureAwait(false);

    private async Task<string> AuthUrl(string token, string kind, string? path, CancellationToken ct)
    {
        path ??= (await GetAsync(token, ct).ConfigureAwait(false)).Path;
        return Url.Of($"api/v2.1/share-links/{Url.Seg(token)}/{kind}/").Q("path", path);
    }

    private static string? IsoTime(DateTimeOffset? d) => d?.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);

    private static string? ScopeValue(SeafShareLinkScope? s) => s switch
    {
        SeafShareLinkScope.AllUsers => "all_users",
        SeafShareLinkScope.SpecificUsers => "specific_users",
        SeafShareLinkScope.SpecificEmails => "specific_emails",
        _ => null,
    };

    private sealed record DirentsResponse { public List<SeafShareLinkDirent> DirentList { get; init; } = []; }
    private sealed record AuthListResponse<T> { public List<T> AuthList { get; init; } = []; }
    private sealed record EmailAuthResult { public List<EmailEntry> Success { get; init; } = []; public List<SeafShareLinkAuthFailure> Failed { get; init; } = []; }
    private sealed record EmailEntry { public string Email { get; init; } = ""; }
}
