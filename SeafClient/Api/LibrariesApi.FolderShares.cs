using System.Globalization;
using SeafClient.Internal;
using SeafClient.Models;

namespace SeafClient.Api;

// Folder shares, permission changes and batch library shares.
public sealed partial class LibrariesApi
{
    /// <summary>
    /// Shares a library, or a folder in it, with several users at once. Unlike <see cref="ShareWithUserAsync"/>,
    /// recipients that fail (unknown user, already shared, ...) are reported in the result instead of thrown.
    /// </summary>
    public Task<SeafShareResult> ShareWithUsersAsync(string libraryId, IEnumerable<string> userEmails, SeafSharePermission permission = SeafSharePermission.Rw, string path = "/", CancellationToken ct = default)
        => ShareWithManyCore(libraryId, path, "user", userEmails.Select(e => ("username", (string?)e)), permission, ct);

    /// <summary>Shares a library, or a folder in it, with several groups at once. Failures are reported in the result.</summary>
    public Task<SeafShareResult> ShareWithGroupsAsync(string libraryId, IEnumerable<int> groupIds, SeafSharePermission permission = SeafSharePermission.Rw, string path = "/", CancellationToken ct = default)
        => ShareWithManyCore(libraryId, path, "group", groupIds.Select(g => ("group_id", (string?)g.ToString(CultureInfo.InvariantCulture))), permission, ct);

    /// <summary>Changes the permission of an existing share of a library (or folder) with a user.</summary>
    public Task UpdateUserSharePermissionAsync(string libraryId, string userEmail, SeafSharePermission permission, string path = "/", CancellationToken ct = default)
        => _t.SendAsync(HttpMethod.Post, Url.Of($"api2/repos/{Url.Seg(libraryId)}/dir/shared_items/")
            .Q("p", SeafPath.Normalize(path)).Q("share_type", "user").Q("username", userEmail),
            SeafTransport.Form(("permission", SharePermValue(permission))), ct);

    /// <summary>Changes the permission of an existing share of a library (or folder) with a group.</summary>
    public Task UpdateGroupSharePermissionAsync(string libraryId, int groupId, SeafSharePermission permission, string path = "/", CancellationToken ct = default)
        => _t.SendAsync(HttpMethod.Post, Url.Of($"api2/repos/{Url.Seg(libraryId)}/dir/shared_items/")
            .Q("p", SeafPath.Normalize(path)).Q("share_type", "group").Q("group_id", groupId),
            SeafTransport.Form(("permission", SharePermValue(permission))), ct);

    /// <summary>Lists sub-folders (not whole libraries) the current user has shared with users or groups.</summary>
    public async Task<IReadOnlyList<SeafSharedFolder>> ListSharedFoldersAsync(CancellationToken ct = default)
        => await _t.GetAsync<List<SeafSharedFolder>>("api/v2.1/shared-folders/", ct).ConfigureAwait(false);

    // ---- batch: several libraries, one recipient ----

    /// <summary>Shares several whole libraries with one user. Per-library failures are reported in the result.</summary>
    public Task<SeafBatchShareResult> BatchShareWithUserAsync(IEnumerable<string> libraryIds, string userEmail, SeafSharePermission permission = SeafSharePermission.Rw, CancellationToken ct = default)
        => BatchShareCore("share", libraryIds, ("share_type", "user"), ("username", userEmail), permission, ct);

    /// <summary>Shares several whole libraries with one group. Per-library failures are reported in the result.</summary>
    public Task<SeafBatchShareResult> BatchShareWithGroupAsync(IEnumerable<string> libraryIds, int groupId, SeafSharePermission permission = SeafSharePermission.Rw, CancellationToken ct = default)
        => BatchShareCore("share", libraryIds, ("share_type", "group"), ("group_id", groupId.ToString(CultureInfo.InvariantCulture)), permission, ct);

    /// <summary>Removes the shares of several libraries with one user.</summary>
    public Task<SeafBatchShareResult> BatchUnshareFromUserAsync(IEnumerable<string> libraryIds, string userEmail, CancellationToken ct = default)
        => BatchShareCore("unshare", libraryIds, ("share_type", "user"), ("username", userEmail), null, ct);

    /// <summary>Removes the shares of several libraries with one group.</summary>
    public Task<SeafBatchShareResult> BatchUnshareFromGroupAsync(IEnumerable<string> libraryIds, int groupId, CancellationToken ct = default)
        => BatchShareCore("unshare", libraryIds, ("share_type", "group"), ("group_id", groupId.ToString(CultureInfo.InvariantCulture)), null, ct);

    private Task<SeafShareResult> ShareWithManyCore(string libraryId, string path, string shareType, IEnumerable<(string, string?)> targets, SeafSharePermission permission, CancellationToken ct)
    {
        var fields = new List<(string, string?)> { ("share_type", shareType), ("permission", SharePermValue(permission)) };
        fields.AddRange(targets);
        return _t.SendAsync<SeafShareResult>(HttpMethod.Put, Url.Of($"api2/repos/{Url.Seg(libraryId)}/dir/shared_items/").Q("p", SeafPath.Normalize(path)),
            SeafTransport.Form([.. fields]), ct);
    }

    private Task<SeafBatchShareResult> BatchShareCore(string operation, IEnumerable<string> libraryIds, (string, string?) type, (string, string?) target, SeafSharePermission? permission, CancellationToken ct)
    {
        var fields = new List<(string, string?)> { ("operation", operation), type, target, ("permission", permission == null ? null : SharePermValue(permission.Value)) };
        fields.AddRange(libraryIds.Select(id => ("repo_id", (string?)id)));
        return _t.SendAsync<SeafBatchShareResult>(HttpMethod.Post, "api/v2.1/repos/batch/", SeafTransport.Form([.. fields]), ct);
    }

    private static string SharePermValue(SeafSharePermission p) => p.ToString().ToLowerInvariant();
}
