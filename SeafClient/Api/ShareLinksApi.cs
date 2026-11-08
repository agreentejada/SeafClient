using SeafClient.Internal;
using SeafClient.Models;

namespace SeafClient.Api;

/// <summary>Public share links and internal smart links.</summary>
public sealed partial class ShareLinksApi
{
    private readonly SeafTransport _t;
    internal ShareLinksApi(SeafTransport t) => _t = t;

    /// <summary>
    /// Lists the current user's share links, optionally filtered to a library and a path in it.
    /// The server matches <paramref name="path"/> exactly (directories with or without a trailing '/').
    /// </summary>
    public async Task<IReadOnlyList<SeafShareLink>> ListAsync(string? libraryId = null, string? path = null, CancellationToken ct = default)
    {
        if (path != null && libraryId == null) throw new ArgumentException("A path filter requires a library id.", nameof(path));
        return await _t.GetAsync<List<SeafShareLink>>(Url.Of("api/v2.1/share-links/")
            .Q("repo_id", libraryId).Q("path", path == null ? null : SeafPath.Normalize(path)), ct).ConfigureAwait(false);
    }

    public Task<SeafShareLink> GetAsync(string token, CancellationToken ct = default)
        => _t.GetAsync<SeafShareLink>($"api/v2.1/share-links/{Url.Seg(token)}/", ct);

    /// <summary>Creates a share link for a file or directory.</summary>
    public Task<SeafShareLink> CreateAsync(string libraryId, string path, SeafShareLinkOptions? options = null, CancellationToken ct = default)
    {
        options ??= new SeafShareLinkOptions();
        return _t.SendAsync<SeafShareLink>(HttpMethod.Post, "api/v2.1/share-links/", SeafTransport.Json(new
        {
            repo_id = libraryId,
            path = SeafPath.Normalize(path),
            password = options.Password,
            expiration_time = options.ExpiresAt?.ToString("yyyy-MM-dd'T'HH:mm:sszzz", System.Globalization.CultureInfo.InvariantCulture),
            expire_days = options.ExpiresAt == null ? options.ExpireDays : null,
            permissions = new { can_edit = options.CanEdit, can_download = options.CanDownload, can_upload = options.CanUpload },
        }), ct);
    }

    /// <summary>
    /// Returns an unexpired share link for the item if one exists, otherwise creates one. Expired links for the item are deleted.
    /// </summary>
    /// <returns>The link and whether it was newly created.</returns>
    public async Task<(SeafShareLink Link, bool Created)> GetOrCreateAsync(string libraryId, string path, SeafShareLinkOptions? options = null, CancellationToken ct = default)
    {
        var existing = await ListAsync(libraryId, path, ct).ConfigureAwait(false);
        foreach (var expired in existing.Where(l => l.IsExpiredNow))
            await DeleteAsync(expired.Token, ct).ConfigureAwait(false);

        var valid = existing.Where(l => !l.IsExpiredNow).OrderByDescending(l => l.Created).FirstOrDefault();
        if (valid != null) return (valid, false);
        return (await CreateAsync(libraryId, path, options, ct).ConfigureAwait(false), true);
    }

    public Task DeleteAsync(string token, CancellationToken ct = default)
        => _t.SendAsync(HttpMethod.Delete, $"api/v2.1/share-links/{Url.Seg(token)}/", null, ct);

    /// <summary>Returns an internal link to a file or directory that only works for users with access to it, and keeps working after renames.</summary>
    public Task<SeafSmartLink> GetSmartLinkAsync(string libraryId, string path, bool isDirectory, CancellationToken ct = default)
        => _t.GetAsync<SeafSmartLink>(Url.Of("api/v2.1/smart-link/")
            .Q("repo_id", libraryId).Q("path", SeafPath.Normalize(path)).Q("is_dir", isDirectory), ct);
}
