using SeafClient.Internal;

namespace SeafClient.Api;

public sealed partial class DirectoriesApi
{
    /// <summary>
    /// Restores a directory (recursively) to its state at <paramref name="commitId"/>. If the directory no longer exists
    /// it is recreated. Creates a new commit; nothing is lost.
    /// </summary>
    public Task RevertAsync(string libraryId, string path, string commitId, CancellationToken ct = default)
        => _t.SendAsync(HttpMethod.Put, $"api2/repos/{Url.Seg(libraryId)}/dir/revert/",
            SeafTransport.Form(("p", SeafPath.Normalize(path)), ("commit_id", commitId)), ct);
}
