using SeafClient.Internal;

namespace SeafClient.Api;

public sealed partial class DirectoriesApi
{
    /// <summary>
    /// Moves the directory <paramref name="sourcePath"/> into <paramref name="destinationParentDir"/>, merging it with a
    /// same-named directory there instead of creating <c>name (1)</c>. Without a same-named directory it behaves like a move.
    /// </summary>
    public Task MoveMergeAsync(string sourceLibraryId, string sourcePath, string destinationLibraryId, string destinationParentDir, CancellationToken ct = default)
    {
        sourcePath = SeafPath.Normalize(sourcePath);
        if (sourcePath == "/") throw new ArgumentException("The library root cannot be moved.", nameof(sourcePath));
        return _t.SendAsync(HttpMethod.Post, "api/v2.1/move-folder-merge/", SeafTransport.Json(new
        {
            src_repo_id = sourceLibraryId,
            src_parent_dir = SeafPath.GetParent(sourcePath),
            src_dirent_name = SeafPath.GetName(sourcePath),
            dst_repo_id = destinationLibraryId,
            dst_parent_dir = SeafPath.Normalize(destinationParentDir),
        }), ct);
    }
}
