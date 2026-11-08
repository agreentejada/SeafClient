using System.Text.Json;
using SeafClient.Internal;
using SeafClient.Models;

namespace SeafClient.Api;

/// <summary>
/// Metadata operations shared by the account-token API (<c>api/v2.1/repos/{id}/metadata/</c>) and the
/// repo-token API (<c>api/v2.1/via-repo-token/metadata/</c>); only the base path differs.
/// </summary>
internal sealed class MetadataCore(SeafTransport t, string basePath)
{
    private readonly SeafTransport _t = t;
    private readonly string _base = basePath.EndsWith('/') ? basePath : basePath + "/";

    // ---------------- records ----------------

    public async Task<SeafMetadataQueryResult> ListRecordsAsync(string viewId, int start, int limit, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(viewId);
        var json = await _t.SendForStringAsync(HttpMethod.Get, Url.Of(_base + "records/").Q("view_id", viewId).Q("start", start).Q("limit", limit), null, ct).ConfigureAwait(false);
        return ParseQuery(json);
    }

    /// <summary>
    /// Updates records in batches. Seafile 14 applies the values of the wrong record when one request carries
    /// <em>different</em> values for several records (it reuses a stale loop variable server-side), so updates are grouped
    /// by identical values and each group is sent separately.
    /// </summary>
    public async Task UpdateRecordsAsync(IEnumerable<SeafMetadataUpdate> updates, int maxBatchSize, CancellationToken ct)
    {
        var groups = updates.GroupBy(u => ValuesKey(u.Values), StringComparer.Ordinal);
        foreach (var group in groups)
        {
            foreach (var chunk in group.Chunk(maxBatchSize))
            {
                await _t.SendAsync(HttpMethod.Put, _base + "records/", SeafTransport.Json(new
                {
                    records_data = chunk.Select(u => new { record_id = u.RecordId, record = u.Values }),
                }), ct).ConfigureAwait(false);
            }
        }
    }

    private static string ValuesKey(IReadOnlyDictionary<string, object?> values)
        => string.Join('\u001f', values.OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => kv.Key + "=" + JsonSerializer.Serialize(kv.Value, SeafJson.Options)));

    // ---------------- views ----------------

    public Task<SeafMetadataViewList> ListViewsAsync(CancellationToken ct)
        => _t.GetAsync<SeafMetadataViewList>(_base + "views/", ct);

    public async Task<SeafMetadataView> GetViewAsync(string viewId, CancellationToken ct)
        => (await _t.GetAsync<ViewResponse>($"{_base}views/{Url.Seg(viewId)}/", ct).ConfigureAwait(false)).View
           ?? throw new SeafException(System.Net.HttpStatusCode.NotFound, $"Metadata view {viewId} not found.");

    public async Task<SeafMetadataView> CreateViewAsync(string name, string type, string? folderId, object? data, CancellationToken ct)
        => (await _t.SendAsync<ViewResponse>(HttpMethod.Post, _base + "views/",
            SeafTransport.Json(new { name, type, folder_id = folderId, data = data ?? new Dictionary<string, object>() }), ct).ConfigureAwait(false)).View!;

    public Task UpdateViewAsync(string viewId, object viewData, CancellationToken ct)
        => _t.SendAsync(HttpMethod.Put, _base + "views/", SeafTransport.Json(new { view_id = viewId, view_data = viewData }), ct);

    public Task DeleteViewAsync(string viewId, string? folderId, CancellationToken ct)
        => _t.SendAsync(HttpMethod.Delete, _base + "views/", SeafTransport.Json(new { view_id = viewId, folder_id = folderId }), ct);

    public async Task<SeafMetadataView> DuplicateViewAsync(string viewId, string? folderId, CancellationToken ct)
        => (await _t.SendAsync<ViewResponse>(HttpMethod.Post, _base + "duplicate-view/",
            SeafTransport.Json(new { view_id = viewId, folder_id = folderId }), ct).ConfigureAwait(false)).View!;

    public async Task<IReadOnlyList<SeafMetadataNavigationItem>> MoveViewAsync(string? sourceViewId, string? sourceFolderId, string? targetViewId, string? targetFolderId, bool isAboveFolder, CancellationToken ct)
        => (await _t.SendAsync<SeafMetadataViewList>(HttpMethod.Post, _base + "move-views/", SeafTransport.Json(new
        {
            source_view_id = sourceViewId,
            source_folder_id = sourceFolderId,
            target_view_id = targetViewId,
            target_folder_id = targetFolderId,
            is_above_folder = isAboveFolder,
        }), ct).ConfigureAwait(false)).Navigation;

    // ---------------- tags ----------------

    public Task SetTagsEnabledAsync(bool enabled, string lang, CancellationToken ct)
        => enabled
            ? _t.SendAsync(HttpMethod.Put, _base + "tags-status/", SeafTransport.Json(new { lang }), ct)
            : _t.SendAsync(HttpMethod.Delete, _base + "tags-status/", null, ct);

    public async Task<IReadOnlyList<SeafMetadataTag>> ListTagsAsync(int start, int limit, CancellationToken ct)
        => (await _t.GetAsync<TagRowsResponse>(Url.Of(_base + "tags/").Q("start", start).Q("limit", limit), ct).ConfigureAwait(false)).Results;

    public async Task<IReadOnlyList<SeafMetadataTag>> CreateTagsAsync(IEnumerable<SeafNewMetadataTag> tags, CancellationToken ct)
    {
        var data = tags.Select(tag =>
        {
            var row = new Dictionary<string, object?> { ["_tag_name"] = tag.Name };
            if (tag.Color != null) row["_tag_color"] = tag.Color;
            return row;
        }).ToList();
        if (data.Count == 0) return [];
        return (await _t.SendAsync<TagsResponse>(HttpMethod.Post, _base + "tags/", SeafTransport.Json(new { tags_data = data }), ct).ConfigureAwait(false)).Tags;
    }

    public Task UpdateTagsAsync(IEnumerable<SeafMetadataTagUpdate> updates, CancellationToken ct)
    {
        var data = updates.Select(u =>
        {
            var tag = new Dictionary<string, object?>();
            if (u.Name != null) tag["_tag_name"] = u.Name;
            if (u.Color != null) tag["_tag_color"] = u.Color;
            return new { tag_id = u.TagId, tag };
        }).Where(x => x.tag.Count > 0).ToList();
        return data.Count == 0 ? Task.CompletedTask
            : _t.SendAsync(HttpMethod.Put, _base + "tags/", SeafTransport.Json(new { tags_data = data }), ct);
    }

    public Task DeleteTagsAsync(IEnumerable<string> tagIds, CancellationToken ct)
    {
        var ids = tagIds.ToList();
        return ids.Count == 0 ? Task.CompletedTask
            : _t.SendAsync(HttpMethod.Delete, _base + "tags/", SeafTransport.Json(new { tag_ids = ids }), ct);
    }

    /// <summary>Replaces the tags of each record. Returns the raw response (its shape differs between the two APIs).</summary>
    public Task<string> SetFileTagsAsync(IReadOnlyDictionary<string, IEnumerable<string>> recordTags, CancellationToken ct)
        => _t.SendForStringAsync(HttpMethod.Put, _base + "file-tags/", SeafTransport.Json(new
        {
            file_tags_data = recordTags.Select(kv => new { record_id = kv.Key, tags = kv.Value.ToList() }),
        }), ct);

    public async Task<SeafMetadataQueryResult> ListTagFilesAsync(string tagId, CancellationToken ct)
        => ParseQuery(await _t.SendForStringAsync(HttpMethod.Get, $"{_base}tag-files/{Url.Seg(tagId)}/", null, ct).ConfigureAwait(false));

    public async Task<SeafMetadataQueryResult> ListTagsFilesAsync(IEnumerable<string> tagIds, CancellationToken ct)
    {
        var ids = tagIds.ToList();
        if (ids.Count == 0) return new SeafMetadataQueryResult();
        return ParseQuery(await _t.SendForStringAsync(HttpMethod.Post, _base + "tags-files/", SeafTransport.Json(new { tags_ids = ids }), ct).ConfigureAwait(false));
    }

    public Task ChangeTagLinksAsync(HttpMethod method, string linkColumnKey, IReadOnlyDictionary<string, IEnumerable<string>> rowIdMap, CancellationToken ct)
        => _t.SendAsync(method, _base + "tags-links/", SeafTransport.Json(new
        {
            link_column_key = linkColumnKey,
            row_id_map = rowIdMap.ToDictionary(kv => kv.Key, kv => kv.Value.ToList()),
        }), ct);

    public Task MergeTagsAsync(string targetTagId, IEnumerable<string> mergedTagIds, CancellationToken ct)
        => _t.SendAsync(HttpMethod.Post, _base + "merge-tags/", SeafTransport.Json(new { target_tag_id = targetTagId, merged_tags_ids = mergedTagIds.ToList() }), ct);

    // ---------------- parsing ----------------

    /// <summary>
    /// Parses <c>{"metadata": [columns], "results": [rows]}</c>. Some endpoints return a bare <c>[]</c> when nothing matches.
    /// </summary>
    internal static SeafMetadataQueryResult ParseQuery(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object) return new SeafMetadataQueryResult();

        var columns = root.TryGetProperty("metadata", out var m) && m.ValueKind == JsonValueKind.Array
            ? m.Deserialize<List<SeafMetadataColumn>>(SeafJson.Options) ?? []
            : [];
        var records = root.TryGetProperty("results", out var r) && r.ValueKind == JsonValueKind.Array
            ? r.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.Object)
                .Select(e => new SeafMetadataRecord(e.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone()))).ToList()
            : [];
        return new SeafMetadataQueryResult { Columns = columns, Records = records };
    }

    private sealed record ViewResponse { public SeafMetadataView? View { get; init; } }
    private sealed record TagRowsResponse { public List<SeafMetadataTag> Results { get; init; } = []; }
    private sealed record TagsResponse { public List<SeafMetadataTag> Tags { get; init; } = []; }
}
