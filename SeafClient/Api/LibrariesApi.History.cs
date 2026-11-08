using System.Globalization;
using SeafClient.Internal;
using SeafClient.Models;

namespace SeafClient.Api;

// Ownership, history, trash, snapshots and libraries shared with the current user.
public sealed partial class LibrariesApi
{
    // ---------------- search ----------------

    /// <summary>
    /// Finds visible libraries whose name contains <paramref name="text"/> (server-side, case-insensitive match).
    /// Duplicates (same library reachable several ways) are removed; owned entries win.
    /// </summary>
    /// <remarks><c>GET /api/v2.1/repos/</c> ignores a name filter, so the match is done with <c>GET /api2/repos/?nameContains=</c>.</remarks>
    public async Task<IReadOnlyList<SeafLibrary>> SearchByNameAsync(string text, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);
        var matches = await _t.GetAsync<List<Api2RepoId>>(Url.Of("api2/repos/").Q("nameContains", text), ct).ConfigureAwait(false);
        if (matches.Count == 0) return [];
        var ids = matches.Select(m => m.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return (await ListDistinctAsync(ct).ConfigureAwait(false)).Where(l => ids.Contains(l.Id)).ToList();
    }

    // ---------------- ownership ----------------

    /// <summary>Returns the owner (internal user id) of a library. Only the owner (or an admin of a department library) may call it.</summary>
    public async Task<string> GetOwnerAsync(string libraryId, CancellationToken ct = default)
        => (await _t.GetAsync<OwnerResponse>($"api2/repos/{Url.Seg(libraryId)}/owner/", ct).ConfigureAwait(false)).Owner;

    /// <summary>
    /// Transfers a library to another user. After the transfer the current user loses access unless
    /// <paramref name="keepShare"/> is true, in which case the library stays shared with them (rw).
    /// </summary>
    public Task TransferAsync(string libraryId, string newOwnerEmail, bool keepShare = false, CancellationToken ct = default)
        => _t.SendAsync(HttpMethod.Put, $"api2/repos/{Url.Seg(libraryId)}/owner/",
            SeafTransport.Form(("owner", newOwnerEmail), ("reshare", keepShare ? "true" : null)), ct);

    // ---------------- history ----------------

    /// <summary>Lists the commits of a library, newest first.</summary>
    public async Task<SeafHistoryPage> GetHistoryAsync(string libraryId, int page = 1, int perPage = 100, CancellationToken ct = default)
    {
        var r = await _t.GetAsync<HistoryResponse>(Url.Of($"api/v2.1/repos/{Url.Seg(libraryId)}/history/").Q("page", page).Q("per_page", perPage), ct).ConfigureAwait(false);
        return new SeafHistoryPage { Items = r.Data, HasMore = r.More };
    }

    /// <summary>Returns details and the list of changes of one commit.</summary>
    public Task<SeafCommitDetail> GetCommitAsync(string libraryId, string commitId, CancellationToken ct = default)
        => _t.GetAsync<SeafCommitDetail>($"api/v2.1/repos/{Url.Seg(libraryId)}/commits/{Url.Seg(commitId)}/", ct);

    /// <summary>Lists a directory as it was at <paramref name="commitId"/>.</summary>
    public async Task<IReadOnlyList<SeafSnapshotEntry>> ListSnapshotDirectoryAsync(string libraryId, string commitId, string path = "/", CancellationToken ct = default)
        => (await _t.GetAsync<SnapshotDirResponse>(Url.Of($"api/v2.1/repos/{Url.Seg(libraryId)}/commits/{Url.Seg(commitId)}/dir/")
            .Q("path", SeafPath.Normalize(path)), ct).ConfigureAwait(false)).DirentList;

    /// <summary>Reverts the whole library to the state of <paramref name="commitId"/> (creates a new commit; history is kept).</summary>
    public Task RevertToSnapshotAsync(string libraryId, string commitId, CancellationToken ct = default)
        => _t.SendAsync(HttpMethod.Post, $"api/v2.1/repos/{Url.Seg(libraryId)}/commits/{Url.Seg(commitId)}/revert/", null, ct);

    /// <summary>
    /// Returns how many days of history the library keeps: <c>-1</c> keeps all history, <c>0</c> keeps none.
    /// </summary>
    public async Task<int> GetHistoryLimitAsync(string libraryId, CancellationToken ct = default)
        => (await _t.GetAsync<HistoryLimitResponse>($"api2/repos/{Url.Seg(libraryId)}/history-limit/", ct).ConfigureAwait(false)).KeepDays;

    /// <summary>Sets the history retention in days (<c>-1</c> = keep all, <c>0</c> = none). Fails with 403 if the server disables user-configurable history.</summary>
    /// <returns>The limit now in effect.</returns>
    public async Task<int> SetHistoryLimitAsync(string libraryId, int keepDays, CancellationToken ct = default)
        => (await _t.SendAsync<HistoryLimitResponse>(HttpMethod.Put, $"api2/repos/{Url.Seg(libraryId)}/history-limit/",
            SeafTransport.Form(("keep_days", keepDays.ToString(CultureInfo.InvariantCulture))), ct).ConfigureAwait(false)).KeepDays;

    // ---------------- trash ----------------

    /// <summary>
    /// Lists deleted items under <paramref name="path"/>. The folder itself must still exist; items in a deleted folder
    /// are listed at the level where the folder was deleted.
    /// </summary>
    /// <param name="showDays">Only items deleted within this many days (0 = no limit).</param>
    /// <param name="scanStat">Cursor from a previous <see cref="SeafTrashPage.ScanStat"/> to continue scanning.</param>
    public async Task<SeafTrashPage> ListTrashAsync(string libraryId, string path = "/", int showDays = 0, string? scanStat = null, CancellationToken ct = default)
    {
        var r = await _t.GetAsync<TrashResponse>(Url.Of($"api/v2.1/repos/{Url.Seg(libraryId)}/trash/")
            .Q("path", SeafPath.Normalize(path)).Q("show_days", showDays == 0 ? null : showDays).Q("scan_stat", scanStat), ct).ConfigureAwait(false);
        return new SeafTrashPage { Items = r.Data, HasMore = r.More, ScanStat = r.ScanStat };
    }

    /// <summary>
    /// Permanently removes items from the library trash. With <paramref name="keepDays"/> &gt; 0, items deleted within
    /// the last <paramref name="keepDays"/> days are kept. Owner (or department admin) only; cannot be undone.
    /// </summary>
    public Task CleanTrashAsync(string libraryId, int keepDays = 0, CancellationToken ct = default)
        => _t.SendAsync(HttpMethod.Delete, $"api/v2.1/repos/{Url.Seg(libraryId)}/trash/",
            SeafTransport.Form(("keep_days", keepDays.ToString(CultureInfo.InvariantCulture))), ct);

    /// <summary>Restores deleted files/folders from the trash to their original location.</summary>
    /// <param name="commitId">The <see cref="SeafTrashItem.CommitId"/> of the items.</param>
    /// <param name="paths">Original full paths (<see cref="SeafTrashItem.Path"/>).</param>
    public Task<SeafRestoreResult> RestoreFromTrashAsync(string libraryId, string commitId, IEnumerable<string> paths, CancellationToken ct = default)
    {
        var fields = paths.Select(p => ("path", (string?)SeafPath.Normalize(p))).Append(("commit_id", commitId)).ToArray();
        return _t.SendAsync<SeafRestoreResult>(HttpMethod.Post, $"api/v2.1/repos/{Url.Seg(libraryId)}/trash/revert-dirents/", SeafTransport.Form(fields), ct);
    }

    /// <summary>Restores one trash item to its original location.</summary>
    public async Task RestoreFromTrashAsync(string libraryId, SeafTrashItem item, CancellationToken ct = default)
    {
        var r = await RestoreFromTrashAsync(libraryId, item.CommitId, [item.Path], ct).ConfigureAwait(false);
        if (r.Failed.Count > 0)
            throw new SeafException(System.Net.HttpStatusCode.BadRequest, string.Join("; ", r.Failed.Select(f => f.ErrorMsg)));
    }

    // ---------------- snapshot labels ----------------

    /// <summary>Lists all snapshot label names used in libraries visible to the current user.</summary>
    public async Task<IReadOnlyList<string>> ListSnapshotLabelNamesAsync(CancellationToken ct = default)
        => await _t.GetAsync<List<string>>("api/v2.1/revision-tags/tag-names/", ct).ConfigureAwait(false);

    /// <summary>
    /// Adds labels to a commit (default: the library's current head). Labels may contain letters, digits, '.', '-' and '_'.
    /// Labels are unique per library: adding an existing label moves it to this commit.
    /// </summary>
    public async Task<IReadOnlyList<SeafSnapshotLabel>> AddSnapshotLabelsAsync(string libraryId, IEnumerable<string> labels, string? commitId = null, CancellationToken ct = default)
        => (await _t.SendAsync<LabelsResponse>(HttpMethod.Post, "api/v2.1/revision-tags/tagged-items/",
            SeafTransport.Json(new { repo_id = libraryId, commit_id = commitId, tag_names = string.Join(',', labels) }), ct).ConfigureAwait(false)).RevisionTags;

    /// <summary>Replaces all labels of a commit (default: head) with <paramref name="labels"/>; an empty list removes them.</summary>
    public async Task<IReadOnlyList<SeafSnapshotLabel>> SetSnapshotLabelsAsync(string libraryId, IEnumerable<string> labels, string? commitId = null, CancellationToken ct = default)
        => (await _t.SendAsync<LabelsResponse>(HttpMethod.Put, "api/v2.1/revision-tags/tagged-items/",
            SeafTransport.Json(new { repo_id = libraryId, commit_id = commitId, tag_names = string.Join(',', labels) }), ct).ConfigureAwait(false)).RevisionTags;

    /// <summary>Removes a label from a library.</summary>
    public Task DeleteSnapshotLabelAsync(string libraryId, string label, CancellationToken ct = default)
        => _t.SendAsync(HttpMethod.Delete, Url.Of("api/v2.1/revision-tags/tagged-items/").Q("repo_id", libraryId).Q("tag_name", label), null, ct);

    // ---------------- shared with me ----------------

    /// <summary>Lists libraries (and sub-folders) other users shared with the current user, directly or via groups.</summary>
    public async Task<IReadOnlyList<SeafSharedLibrary>> ListSharedWithMeAsync(CancellationToken ct = default)
        => await _t.GetAsync<List<SeafSharedLibrary>>("api2/beshared-repos/", ct).ConfigureAwait(false);

    /// <summary>Removes a library shared with the current user from their list ("leave share").</summary>
    public Task LeaveShareAsync(SeafSharedLibrary share, CancellationToken ct = default)
        => LeaveShareAsync(share.Id, share.SharedBy, share.ShareType, share.ShareType == "group" ? share.GroupId : null, ct);

    /// <summary>Removes a library shared with the current user from their list ("leave share").</summary>
    /// <param name="sharedBy">The user who shared it (<see cref="SeafSharedLibrary.SharedBy"/>).</param>
    /// <param name="shareType"><c>personal</c>, <c>group</c> or <c>public</c>.</param>
    /// <param name="groupId">Required when <paramref name="shareType"/> is <c>group</c> (group admins only).</param>
    public Task LeaveShareAsync(string libraryId, string sharedBy, string shareType = "personal", int? groupId = null, CancellationToken ct = default)
        => _t.SendAsync(HttpMethod.Delete, Url.Of($"api2/beshared-repos/{Url.Seg(libraryId)}/")
            .Q("share_type", shareType).Q("from", sharedBy).Q("group_id", groupId), null, ct);

    private sealed record Api2RepoId { public string Id { get; init; } = ""; }
    private sealed record OwnerResponse { public string Owner { get; init; } = ""; }
    private sealed record HistoryResponse { public List<SeafCommit> Data { get; init; } = []; public bool More { get; init; } }
    private sealed record HistoryLimitResponse { public int KeepDays { get; init; } }
    private sealed record SnapshotDirResponse { public List<SeafSnapshotEntry> DirentList { get; init; } = []; }
    private sealed record TrashResponse { public List<SeafTrashItem> Data { get; init; } = []; public bool More { get; init; } public string? ScanStat { get; init; } }
    private sealed record LabelsResponse { [System.Text.Json.Serialization.JsonPropertyName("revisionTags")] public List<SeafSnapshotLabel> RevisionTags { get; init; } = []; }
}
