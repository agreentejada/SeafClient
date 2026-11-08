using SeafClient.Internal;

namespace SeafClient.Api;

/// <summary>Batch operations on several files/directories that share a parent directory.</summary>
public sealed class ItemsApi
{
    private readonly SeafTransport _t;
    internal ItemsApi(SeafTransport t) => _t = t;

    /// <summary>Copies <paramref name="names"/> (files or directories inside <paramref name="sourceDir"/>) into <paramref name="destinationDir"/>. Blocks until done.</summary>
    public Task CopyAsync(string sourceLibraryId, string sourceDir, IEnumerable<string> names, string destinationLibraryId, string destinationDir, CancellationToken ct = default)
        => Batch("api/v2.1/repos/sync-batch-copy-item/", sourceLibraryId, sourceDir, names, destinationLibraryId, destinationDir, ct);

    /// <summary>Moves <paramref name="names"/> (files or directories inside <paramref name="sourceDir"/>) into <paramref name="destinationDir"/>. Blocks until done.</summary>
    public Task MoveAsync(string sourceLibraryId, string sourceDir, IEnumerable<string> names, string destinationLibraryId, string destinationDir, CancellationToken ct = default)
        => Batch("api/v2.1/repos/sync-batch-move-item/", sourceLibraryId, sourceDir, names, destinationLibraryId, destinationDir, ct);

    /// <summary>Deletes <paramref name="names"/> inside <paramref name="parentDir"/>. Names that do not exist are ignored by the server.</summary>
    public Task DeleteAsync(string libraryId, string parentDir, IEnumerable<string> names, CancellationToken ct = default)
        => _t.SendAsync(HttpMethod.Delete, "api/v2.1/repos/batch-delete-item/", SeafTransport.Json(new
        {
            repo_id = libraryId,
            parent_dir = SeafPath.Normalize(parentDir),
            dirents = names.ToArray(),
        }), ct);

    private Task Batch(string url, string srcRepo, string srcDir, IEnumerable<string> names, string dstRepo, string dstDir, CancellationToken ct)
    {
        var list = names.ToArray();
        if (list.Length == 0) return Task.CompletedTask;
        return _t.SendAsync(HttpMethod.Post, url, SeafTransport.Json(new
        {
            src_repo_id = srcRepo,
            src_parent_dir = SeafPath.Normalize(srcDir),
            src_dirents = list,
            dst_repo_id = dstRepo,
            dst_parent_dir = SeafPath.Normalize(dstDir),
        }), ct);
    }
}
