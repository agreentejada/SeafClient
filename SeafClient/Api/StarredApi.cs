using SeafClient.Internal;
using SeafClient.Models;

namespace SeafClient.Api;

/// <summary>Starred files and folders of the current user.</summary>
public sealed class StarredApi
{
    private readonly SeafTransport _t;
    internal StarredApi(SeafTransport t) => _t = t;

    public async Task<IReadOnlyList<SeafStarredItem>> ListAsync(CancellationToken ct = default)
        => (await _t.GetAsync<StarredResponse>("api/v2.1/starred-items/", ct).ConfigureAwait(false)).StarredItemList;

    public Task<SeafStarredItem> StarAsync(string libraryId, string path, CancellationToken ct = default)
        => _t.SendAsync<SeafStarredItem>(HttpMethod.Post, "api/v2.1/starred-items/",
            SeafTransport.Json(new { repo_id = libraryId, path = SeafPath.Normalize(path) }), ct);

    public Task UnstarAsync(string libraryId, string path, CancellationToken ct = default)
        => _t.SendAsync(HttpMethod.Delete, Url.Of("api/v2.1/starred-items/").Q("repo_id", libraryId).Q("path", SeafPath.Normalize(path)), null, ct);

    private sealed record StarredResponse { public List<SeafStarredItem> StarredItemList { get; init; } = []; }
}
