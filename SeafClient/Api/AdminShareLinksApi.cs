using SeafClient.Internal;
using SeafClient.Models;

namespace SeafClient.Api;

/// <summary>All share links on the server, via the admin API.</summary>
public sealed class AdminShareLinksApi
{
    private readonly SeafTransport _t;
    internal AdminShareLinksApi(SeafTransport t) => _t = t;

    private static string Base(string token) => $"api/v2.1/admin/share-links/{Url.Seg(token)}/";

    /// <summary>Lists share links (summary fields only; use <see cref="GetAsync"/> for details).</summary>
    /// <param name="orderBy"><c>ctime</c> or <c>view_cnt</c>; null for the server default.</param>
    /// <param name="descending">Sort direction when <paramref name="orderBy"/> is set.</param>
    public async Task<SeafPage<SeafAdminShareLink>> ListAsync(int page = 1, int perPage = 100, string? orderBy = null, bool descending = true, CancellationToken ct = default)
    {
        var r = await _t.GetAsync<ListResponse>(Url.Of("api/v2.1/admin/share-links/").Q("page", page).Q("per_page", perPage)
            .Q("order_by", orderBy).Q("direction", orderBy == null ? null : descending ? "desc" : "asc"), ct).ConfigureAwait(false);
        return new SeafPage<SeafAdminShareLink> { Items = r.ShareLinkList, TotalCount = r.Count, Page = page, PerPage = perPage };
    }

    public Task<SeafAdminShareLink> GetAsync(string token, CancellationToken ct = default)
        => _t.GetAsync<SeafAdminShareLink>(Base(token), ct);

    /// <summary>Lists the entries of a folder share link, at <paramref name="path"/> relative to the shared folder.</summary>
    public async Task<IReadOnlyList<SeafAdminShareLinkDirent>> ListDirentsAsync(string token, string path = "/", CancellationToken ct = default)
        => await _t.GetAsync<List<SeafAdminShareLinkDirent>>(Url.Of(Base(token) + "dirents/").Q("path", SeafPath.Normalize(path)), ct).ConfigureAwait(false);

    public Task DeleteAsync(string token, CancellationToken ct = default)
        => _t.SendAsync(HttpMethod.Delete, Base(token), null, ct);

    private sealed record ListResponse { public List<SeafAdminShareLink> ShareLinkList { get; init; } = []; public int Count { get; init; } }
}
