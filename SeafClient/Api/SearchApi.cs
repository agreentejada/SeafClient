using System.Globalization;
using SeafClient.Internal;
using SeafClient.Models;

namespace SeafClient.Api;

/// <summary>File and user search.</summary>
public sealed class SearchApi
{
    private readonly SeafTransport _t;
    internal SearchApi(SeafTransport t) => _t = t;

    /// <summary>
    /// Full-text search across libraries (Seafile Pro with a search backend such as SeaSearch or Elasticsearch).
    /// Results depend on the server's index, which lags behind recent changes.
    /// </summary>
    /// <exception cref="SeafException">404 "Search not supported." when the server has no search backend.</exception>
    public Task<SeafSearchResult> SearchFilesAsync(SeafSearchQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var custom = query.FileTypes is { Count: > 0 } || query.Extensions is { Count: > 0 };
        var url = Url.Of("api2/search/")
            .Q("q", query.Query)
            .Q("search_repo", query.LibraryId ?? query.Scope.ToString().ToLowerInvariant())
            .Q("search_path", query.Path == null ? null : SeafPath.Normalize(query.Path))
            .Q("search_filename_only", query.FileNamesOnly ? true : null)
            .Q("obj_type", query.EntryType?.ToString().ToLowerInvariant())
            .Q("search_ftypes", custom ? "custom" : null)
            .Q("input_fexts", query.Extensions is { Count: > 0 } ext ? string.Join(',', ext) : null)
            .Q("time_from", query.ModifiedFrom?.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture))
            .Q("time_to", query.ModifiedTo?.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture))
            .Q("size_from", query.SizeFrom?.ToString(CultureInfo.InvariantCulture))
            .Q("size_to", query.SizeTo?.ToString(CultureInfo.InvariantCulture))
            .Q("page", query.Page)
            .Q("per_page", query.PerPage);
        foreach (var type in query.FileTypes ?? []) url.Q("ftype", type);
        return _t.GetAsync<SeafSearchResult>(url, ct);
    }

    /// <summary>Finds files and folders in one library whose name contains <paramref name="query"/>. Needs no search backend.</summary>
    public async Task<IReadOnlyList<SeafFileSearchHit>> SearchFileNamesAsync(string libraryId, string query, CancellationToken ct = default)
        => (await _t.GetAsync<FileSearchResponse>(Url.Of("api/v2.1/search-file/").Q("repo_id", libraryId).Q("q", query), ct).ConfigureAwait(false)).Data;

    /// <summary>Searches users the current user may share with, by email, name or contact email.</summary>
    public async Task<IReadOnlyList<SeafUserSearchResult>> SearchUsersAsync(string query, CancellationToken ct = default)
        => (await _t.GetAsync<UserSearchResponse>(Url.Of("api2/search-user/").Q("q", query), ct).ConfigureAwait(false)).Users;

    private sealed record FileSearchResponse { public List<SeafFileSearchHit> Data { get; init; } = []; }
    private sealed record UserSearchResponse { public List<SeafUserSearchResult> Users { get; init; } = []; }
}
