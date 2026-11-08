using SeafClient.Internal;
using SeafClient.Models;

namespace SeafClient.Api;

public sealed partial class AdminUsersApi
{
    /// <summary>Lists the libraries a user has shared with other users and groups.</summary>
    public async Task<IReadOnlyList<SeafUserSharedRepo>> ListSharedLibrariesAsync(string email, CancellationToken ct = default)
        => await _t.GetAsync<List<SeafUserSharedRepo>>($"api/v2.1/admin/users/{Url.Seg(email)}/shared-repos/", ct).ConfigureAwait(false);

    /// <summary>Lists the folders (inside libraries) a user has shared with other users and groups.</summary>
    public async Task<IReadOnlyList<SeafUserSharedFolder>> ListSharedFoldersAsync(string email, CancellationToken ct = default)
        => await _t.GetAsync<List<SeafUserSharedFolder>>($"api/v2.1/admin/users/{Url.Seg(email)}/shared-folders/", ct).ConfigureAwait(false);
}
