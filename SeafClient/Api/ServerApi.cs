using SeafClient.Internal;
using SeafClient.Models;

namespace SeafClient.Api;

/// <summary>Server-level and current-account endpoints.</summary>
public sealed partial class ServerApi
{
    private readonly SeafTransport _t;
    internal ServerApi(SeafTransport t) => _t = t;

    /// <summary>Returns true if the server answers <c>/api2/ping/</c> (no auth).</summary>
    public async Task<bool> PingAsync(CancellationToken ct = default)
        => await _t.GetAsync<string>("api2/ping/", ct).ConfigureAwait(false) == "pong";

    /// <summary>Returns true if the session token is valid (<c>/api2/auth/ping/</c>). Throws on other failures.</summary>
    public async Task<bool> AuthPingAsync(CancellationToken ct = default)
    {
        try
        {
            return await _t.GetAsync<string>("api2/auth/ping/", ct).ConfigureAwait(false) == "pong";
        }
        catch (SeafException e) when (e.IsUnauthorized)
        {
            return false;
        }
    }

    public Task<SeafServerInfo> GetInfoAsync(CancellationToken ct = default)
        => _t.GetAsync<SeafServerInfo>("api2/server-info/", ct);

    public Task<SeafAccountInfo> GetAccountInfoAsync(CancellationToken ct = default)
        => _t.GetAsync<SeafAccountInfo>("api2/account/info/", ct);

    /// <summary>Returns the avatar URL of the current user (or of <paramref name="email"/>).</summary>
    public async Task<string> GetAvatarUrlAsync(int size = 80, string? email = null, CancellationToken ct = default)
    {
        var url = email == null ? $"api2/avatars/user/{Url.Seg((await GetAccountInfoAsync(ct).ConfigureAwait(false)).Email)}/resized/{size}/"
                                : $"api2/avatars/user/{Url.Seg(email)}/resized/{size}/";
        var result = await _t.GetAsync<AvatarResponse>(url, ct).ConfigureAwait(false);
        return result.Url;
    }

    private sealed record AvatarResponse { public string Url { get; init; } = ""; }
}
