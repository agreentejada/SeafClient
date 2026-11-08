using SeafClient.Internal;
using SeafClient.Models;

namespace SeafClient.Api;

/// <summary>Comments and replies on files.</summary>
/// <remarks>
/// Seafile 14 addresses comments by the file's uuid, not its path (the older <c>/api2/repos/{id}/file/comments/</c>
/// routes are gone). Every method takes a path and looks the uuid up first; use <see cref="GetFileUuidAsync"/> and the
/// uuid overloads to save that request when making many calls on the same file. The uuid survives renames and moves.
/// </remarks>
public sealed class FileCommentsApi
{
    private readonly SeafTransport _t;
    internal FileCommentsApi(SeafTransport t) => _t = t;

    /// <summary>Returns the stable uuid Seafile uses to identify a file (the same token used by its smart link).</summary>
    public async Task<string> GetFileUuidAsync(string libraryId, string path, CancellationToken ct = default)
    {
        var link = await _t.GetAsync<SeafSmartLink>(Url.Of("api/v2.1/smart-link/")
            .Q("repo_id", libraryId).Q("path", SeafPath.Normalize(path)).Q("is_dir", false), ct).ConfigureAwait(false);
        return link.SmartLinkToken;
    }

    private static string Base(string libraryId, string fileUuid) => $"api/v2.1/repos/{Url.Seg(libraryId)}/file/{Url.Seg(fileUuid)}/comments/";

    /// <summary>Lists all comments on a file, with their replies. Filter by <paramref name="resolved"/> if given.</summary>
    public async Task<IReadOnlyList<SeafFileComment>> ListAsync(string libraryId, string path, bool? resolved = null, CancellationToken ct = default)
        => await ListByUuidAsync(libraryId, await GetFileUuidAsync(libraryId, path, ct).ConfigureAwait(false), resolved, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<SeafFileComment>> ListByUuidAsync(string libraryId, string fileUuid, bool? resolved = null, CancellationToken ct = default)
        => (await _t.GetAsync<CommentList>(Url.Of(Base(libraryId, fileUuid)).Q("resolved", resolved), ct).ConfigureAwait(false)).Comments;

    /// <summary>Returns the number of comments on a file.</summary>
    public async Task<int> CountAsync(string libraryId, string path, CancellationToken ct = default)
    {
        var uuid = await GetFileUuidAsync(libraryId, path, ct).ConfigureAwait(false);
        return (await _t.GetAsync<CommentList>(Url.Of(Base(libraryId, uuid)).Q("page", 1).Q("per_page", 1), ct).ConfigureAwait(false)).TotalCount;
    }

    /// <summary>Adds a comment. <paramref name="detail"/> is stored as-is (the web UI uses it for position markers).</summary>
    public async Task<SeafFileComment> CreateAsync(string libraryId, string path, string comment, string? detail = null, CancellationToken ct = default)
        => await CreateByUuidAsync(libraryId, await GetFileUuidAsync(libraryId, path, ct).ConfigureAwait(false), comment, detail, ct).ConfigureAwait(false);

    public Task<SeafFileComment> CreateByUuidAsync(string libraryId, string fileUuid, string comment, string? detail = null, CancellationToken ct = default)
        => _t.SendAsync<SeafFileComment>(HttpMethod.Post, Base(libraryId, fileUuid), SeafTransport.Json(new { comment, detail }), ct);

    public async Task<SeafFileComment> GetAsync(string libraryId, string path, int commentId, CancellationToken ct = default)
        => await GetByUuidAsync(libraryId, await GetFileUuidAsync(libraryId, path, ct).ConfigureAwait(false), commentId, ct).ConfigureAwait(false);

    public Task<SeafFileComment> GetByUuidAsync(string libraryId, string fileUuid, int commentId, CancellationToken ct = default)
        => _t.GetAsync<SeafFileComment>($"{Base(libraryId, fileUuid)}{commentId}/", ct);

    /// <summary>Edits a comment's text, resolved flag or detail; null arguments are left unchanged. Only the author may edit.</summary>
    public async Task<SeafFileComment> UpdateAsync(string libraryId, string path, int commentId, string? comment = null, bool? resolved = null, string? detail = null, CancellationToken ct = default)
        => await UpdateByUuidAsync(libraryId, await GetFileUuidAsync(libraryId, path, ct).ConfigureAwait(false), commentId, comment, resolved, detail, ct).ConfigureAwait(false);

    public Task<SeafFileComment> UpdateByUuidAsync(string libraryId, string fileUuid, int commentId, string? comment = null, bool? resolved = null, string? detail = null, CancellationToken ct = default)
        => _t.SendAsync<SeafFileComment>(HttpMethod.Put, $"{Base(libraryId, fileUuid)}{commentId}/", SeafTransport.Form(
            ("comment", comment), ("resolved", resolved == null ? null : resolved.Value ? "true" : "false"), ("detail", detail)), ct);

    public async Task DeleteAsync(string libraryId, string path, int commentId, CancellationToken ct = default)
        => await DeleteByUuidAsync(libraryId, await GetFileUuidAsync(libraryId, path, ct).ConfigureAwait(false), commentId, ct).ConfigureAwait(false);

    public Task DeleteByUuidAsync(string libraryId, string fileUuid, int commentId, CancellationToken ct = default)
        => _t.SendAsync(HttpMethod.Delete, $"{Base(libraryId, fileUuid)}{commentId}/", null, ct);

    // ---- replies ----

    public async Task<IReadOnlyList<SeafCommentReply>> ListRepliesAsync(string libraryId, string path, int commentId, CancellationToken ct = default)
    {
        var uuid = await GetFileUuidAsync(libraryId, path, ct).ConfigureAwait(false);
        return (await _t.GetAsync<ReplyList>($"{Base(libraryId, uuid)}{commentId}/replies/", ct).ConfigureAwait(false)).Replies;
    }

    public async Task<SeafCommentReply> AddReplyAsync(string libraryId, string path, int commentId, string reply, CancellationToken ct = default)
    {
        var uuid = await GetFileUuidAsync(libraryId, path, ct).ConfigureAwait(false);
        return await _t.SendAsync<SeafCommentReply>(HttpMethod.Post, $"{Base(libraryId, uuid)}{commentId}/replies/",
            SeafTransport.Form(("reply", reply)), ct).ConfigureAwait(false);
    }

    public async Task DeleteReplyAsync(string libraryId, string path, int commentId, int replyId, CancellationToken ct = default)
    {
        var uuid = await GetFileUuidAsync(libraryId, path, ct).ConfigureAwait(false);
        await _t.SendAsync(HttpMethod.Delete, $"{Base(libraryId, uuid)}{commentId}/replies/{replyId}/", null, ct).ConfigureAwait(false);
    }

    private sealed record CommentList { public List<SeafFileComment> Comments { get; init; } = []; public int TotalCount { get; init; } }
    private sealed record ReplyList { public List<SeafCommentReply> Replies { get; init; } = []; }
}
