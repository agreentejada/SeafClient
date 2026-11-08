using SeafClient.Internal;
using SeafClient.Models;

namespace SeafClient.Api;

/// <summary>
/// Guest invitations. Requires a Seafile Pro server with guest invitations enabled (<c>ENABLE_GUEST_INVITATION</c>) and a
/// user role that may invite guests; otherwise every call fails with 403.
/// </summary>
public sealed class InvitationsApi
{
    private readonly SeafTransport _t;
    internal InvitationsApi(SeafTransport t) => _t = t;

    /// <summary>Lists invitations sent by the current user.</summary>
    public async Task<IReadOnlyList<SeafInvitation>> ListAsync(CancellationToken ct = default)
        => await _t.GetAsync<List<SeafInvitation>>("api/v2.1/invitations/", ct).ConfigureAwait(false);

    /// <summary>Invites <paramref name="email"/> as a guest; the server emails them a sign-up link.</summary>
    public Task<SeafInvitation> CreateAsync(string email, CancellationToken ct = default)
        => _t.SendAsync<SeafInvitation>(HttpMethod.Post, "api/v2.1/invitations/", SeafTransport.Form(("type", "guest"), ("accepter", email)), ct);

    /// <summary>Invites several guests; per-address failures are reported in the result rather than thrown.</summary>
    public Task<SeafBulkInvitationResult> CreateManyAsync(IEnumerable<string> emails, CancellationToken ct = default)
        => _t.SendAsync<SeafBulkInvitationResult>(HttpMethod.Post, "api/v2.1/invitations/batch/",
            SeafTransport.Form([("type", "guest"), .. emails.Select(e => ("accepter", (string?)e))]), ct);

    public Task<SeafInvitation> GetAsync(string token, CancellationToken ct = default)
        => _t.GetAsync<SeafInvitation>($"api/v2.1/invitations/{Url.Seg(token)}/", ct);

    /// <summary>Deletes an invitation that has not been accepted yet.</summary>
    public Task DeleteAsync(string token, CancellationToken ct = default)
        => _t.SendAsync(HttpMethod.Delete, $"api/v2.1/invitations/{Url.Seg(token)}/", null, ct);

    /// <summary>Revokes an accepted invitation: the guest account is deactivated.</summary>
    public Task RevokeAsync(string token, CancellationToken ct = default)
        => _t.SendAsync(HttpMethod.Post, $"api/v2.1/invitations/{Url.Seg(token)}/revoke/", null, ct);
}
