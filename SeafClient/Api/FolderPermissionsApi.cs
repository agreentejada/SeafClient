using System.Globalization;
using SeafClient.Internal;
using SeafClient.Models;

namespace SeafClient.Api;

/// <summary>
/// Sub-folder permissions (Seafile Pro): restrict or widen what a user or group that already has access to a library
/// (through a share) can do in one folder of it. Only <see cref="SeafSharePermission.R"/> and
/// <see cref="SeafSharePermission.Rw"/> are valid here. Requires library owner (or library admin) rights.
/// </summary>
public sealed partial class FolderPermissionsApi
{
    private readonly SeafTransport _t;
    internal FolderPermissionsApi(SeafTransport t) => _t = t;

    private static string UserUrl(string libraryId) => $"api2/repos/{Url.Seg(libraryId)}/user-folder-perm/";
    private static string GroupUrl(string libraryId) => $"api2/repos/{Url.Seg(libraryId)}/group-folder-perm/";

    // ---- users ----

    /// <summary>Lists user folder permissions of a library, optionally only those on <paramref name="folderPath"/>.</summary>
    public async Task<IReadOnlyList<SeafFolderPermission>> ListUserPermissionsAsync(string libraryId, string? folderPath = null, CancellationToken ct = default)
        => await _t.GetAsync<List<SeafFolderPermission>>(Url.Of(UserUrl(libraryId)).Q("folder_path", folderPath == null ? null : SeafPath.Normalize(folderPath)), ct).ConfigureAwait(false);

    /// <summary>Adds a folder permission for one user. Throws if it already exists or the user is unknown.</summary>
    public async Task<SeafFolderPermission> AddUserPermissionAsync(string libraryId, string folderPath, string userEmail, SeafSharePermission permission, CancellationToken ct = default)
        => Single(await AddUserPermissionsAsync(libraryId, folderPath, [userEmail], permission, ct).ConfigureAwait(false));

    /// <summary>Adds a folder permission for several users; per-user failures are reported in the result.</summary>
    public Task<SeafFolderPermissionResult> AddUserPermissionsAsync(string libraryId, string folderPath, IEnumerable<string> userEmails, SeafSharePermission permission, CancellationToken ct = default)
    {
        var fields = new List<(string, string?)> { ("folder_path", SeafPath.Normalize(folderPath)), ("permission", Value(permission)) };
        fields.AddRange(userEmails.Select(e => ("user_email", (string?)e)));
        return _t.SendAsync<SeafFolderPermissionResult>(HttpMethod.Post, UserUrl(libraryId), SeafTransport.Form([.. fields]), ct);
    }

    /// <summary>Changes an existing user folder permission.</summary>
    public Task<SeafFolderPermission> UpdateUserPermissionAsync(string libraryId, string folderPath, string userEmail, SeafSharePermission permission, CancellationToken ct = default)
        => _t.SendAsync<SeafFolderPermission>(HttpMethod.Put, UserUrl(libraryId), SeafTransport.Form(
            ("folder_path", SeafPath.Normalize(folderPath)), ("user_email", userEmail), ("permission", Value(permission))), ct);

    /// <summary>Removes a user folder permission. Succeeds if it did not exist.</summary>
    public Task RemoveUserPermissionAsync(string libraryId, string folderPath, string userEmail, CancellationToken ct = default)
        => _t.SendAsync(HttpMethod.Delete, UserUrl(libraryId), SeafTransport.Form(
            ("folder_path", SeafPath.Normalize(folderPath)), ("user_email", userEmail)), ct);

    // ---- groups ----

    /// <summary>Lists group folder permissions of a library, optionally only those on <paramref name="folderPath"/>.</summary>
    public async Task<IReadOnlyList<SeafFolderPermission>> ListGroupPermissionsAsync(string libraryId, string? folderPath = null, CancellationToken ct = default)
        => await _t.GetAsync<List<SeafFolderPermission>>(Url.Of(GroupUrl(libraryId)).Q("folder_path", folderPath == null ? null : SeafPath.Normalize(folderPath)), ct).ConfigureAwait(false);

    /// <summary>Adds a folder permission for one group. Throws if it already exists or the group is unknown.</summary>
    public async Task<SeafFolderPermission> AddGroupPermissionAsync(string libraryId, string folderPath, int groupId, SeafSharePermission permission, CancellationToken ct = default)
        => Single(await AddGroupPermissionsAsync(libraryId, folderPath, [groupId], permission, ct).ConfigureAwait(false));

    /// <summary>Adds a folder permission for several groups; per-group failures are reported in the result.</summary>
    public Task<SeafFolderPermissionResult> AddGroupPermissionsAsync(string libraryId, string folderPath, IEnumerable<int> groupIds, SeafSharePermission permission, CancellationToken ct = default)
    {
        var fields = new List<(string, string?)> { ("folder_path", SeafPath.Normalize(folderPath)), ("permission", Value(permission)) };
        fields.AddRange(groupIds.Select(g => ("group_id", (string?)g.ToString(CultureInfo.InvariantCulture))));
        return _t.SendAsync<SeafFolderPermissionResult>(HttpMethod.Post, GroupUrl(libraryId), SeafTransport.Form([.. fields]), ct);
    }

    /// <summary>Changes an existing group folder permission.</summary>
    public Task<SeafFolderPermission> UpdateGroupPermissionAsync(string libraryId, string folderPath, int groupId, SeafSharePermission permission, CancellationToken ct = default)
        => _t.SendAsync<SeafFolderPermission>(HttpMethod.Put, GroupUrl(libraryId), SeafTransport.Form(
            ("folder_path", SeafPath.Normalize(folderPath)), ("group_id", groupId.ToString(CultureInfo.InvariantCulture)), ("permission", Value(permission))), ct);

    /// <summary>Removes a group folder permission. Succeeds if it did not exist.</summary>
    public Task RemoveGroupPermissionAsync(string libraryId, string folderPath, int groupId, CancellationToken ct = default)
        => _t.SendAsync(HttpMethod.Delete, GroupUrl(libraryId), SeafTransport.Form(
            ("folder_path", SeafPath.Normalize(folderPath)), ("group_id", groupId.ToString(CultureInfo.InvariantCulture))), ct);

    private static string Value(SeafSharePermission p) => p == SeafSharePermission.Admin
        ? throw new ArgumentException("Folder permissions can only be R or Rw.", nameof(p))
        : p.ToString().ToLowerInvariant();

    private static SeafFolderPermission Single(SeafFolderPermissionResult r)
    {
        if (r.Failed.Count > 0)
            throw new SeafException(System.Net.HttpStatusCode.BadRequest, string.Join("; ", r.Failed.Select(f => f.ErrorMsg)));
        return r.Success.FirstOrDefault() ?? throw new SeafException(System.Net.HttpStatusCode.InternalServerError, "Server returned no folder permission.");
    }
}
