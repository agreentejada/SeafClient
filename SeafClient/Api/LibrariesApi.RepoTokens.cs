using SeafClient.Internal;
using SeafClient.Models;

namespace SeafClient.Api;

public sealed partial class LibrariesApi
{
    private static string TokensUrl(string libraryId) => $"api/v2.1/repos/{Url.Seg(libraryId)}/repo-api-tokens/";

    /// <summary>Lists the repo API tokens of a library (library owner/admin only).</summary>
    public async Task<IReadOnlyList<SeafRepoApiToken>> ListApiTokensAsync(string libraryId, CancellationToken ct = default)
        => (await _t.GetAsync<TokenListResponse>(TokensUrl(libraryId), ct).ConfigureAwait(false)).RepoApiTokens;

    /// <summary>
    /// Generates a repo API token for an application. Use the returned <see cref="SeafRepoApiToken.ApiToken"/> with
    /// <see cref="SeafRepoTokenSession.FromToken"/>.
    /// </summary>
    /// <exception cref="SeafException">400 if <paramref name="appName"/> already has a token for this library.</exception>
    public Task<SeafRepoApiToken> CreateApiTokenAsync(string libraryId, string appName, SeafRepoTokenPermission permission = SeafRepoTokenPermission.Rw, CancellationToken ct = default)
        => _t.SendAsync<SeafRepoApiToken>(HttpMethod.Post, TokensUrl(libraryId),
            SeafTransport.Form(("app_name", appName), ("permission", TokenPerm(permission))), ct);

    /// <summary>Changes the permission of an existing token (the token value stays the same).</summary>
    public Task<SeafRepoApiToken> UpdateApiTokenAsync(string libraryId, string appName, SeafRepoTokenPermission permission, CancellationToken ct = default)
        => _t.SendAsync<SeafRepoApiToken>(HttpMethod.Put, TokensUrl(libraryId) + Url.Seg(appName) + "/",
            SeafTransport.Form(("permission", TokenPerm(permission))), ct);

    /// <summary>Revokes a token.</summary>
    public Task DeleteApiTokenAsync(string libraryId, string appName, CancellationToken ct = default)
        => _t.SendAsync(HttpMethod.Delete, TokensUrl(libraryId) + Url.Seg(appName) + "/", null, ct);

    private static string TokenPerm(SeafRepoTokenPermission p) => p == SeafRepoTokenPermission.R ? "r" : "rw";

    private sealed record TokenListResponse { public List<SeafRepoApiToken> RepoApiTokens { get; init; } = []; }
}
