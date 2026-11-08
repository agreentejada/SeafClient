using SeafClient.Internal;
using SeafClient.Models;

namespace SeafClient.Api;

public sealed partial class GroupsApi
{
    /// <summary>Gets one member of a group.</summary>
    public Task<SeafGroupMember> GetMemberAsync(int groupId, string userEmail, CancellationToken ct = default)
        => _t.GetAsync<SeafGroupMember>($"api/v2.1/groups/{groupId}/members/{Url.Seg(userEmail)}/", ct);

    /// <summary>Gets one member of a group, or null if the user is not a member (or does not exist).</summary>
    public async Task<SeafGroupMember?> TryGetMemberAsync(int groupId, string userEmail, CancellationToken ct = default)
    {
        try { return await GetMemberAsync(groupId, userEmail, ct).ConfigureAwait(false); }
        // Seafile answers 404 for a non-member and 400 "Email ... invalid." for an unknown user.
        catch (SeafException e) when (e.IsNotFound || e.StatusCode == System.Net.HttpStatusCode.BadRequest) { return null; }
    }

    /// <summary>Transfers ownership of a group to another user. Only the owner may do this.</summary>
    /// <remarks>
    /// Seafile 14 does not check that <paramref name="newOwnerEmail"/> is an existing user: transferring to a typo succeeds
    /// and leaves the group ownerless for everyone but system admins. Verify the user first.
    /// </remarks>
    public Task<SeafGroup> TransferAsync(int groupId, string newOwnerEmail, CancellationToken ct = default)
        => _t.SendAsync<SeafGroup>(HttpMethod.Put, $"api/v2.1/groups/{groupId}/", SeafTransport.Form(("owner", newOwnerEmail)), ct);
}
