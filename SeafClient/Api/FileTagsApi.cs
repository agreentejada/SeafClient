using SeafClient.Internal;
using SeafClient.Models;

namespace SeafClient.Api;

/// <summary>
/// The legacy per-library tag system (<c>repo-tags</c> / <c>file-tags</c>). Seafile 13+ also has metadata-based tags,
/// which are separate and live under <see cref="MetadataApi"/>.
/// </summary>
public sealed class FileTagsApi
{
    private readonly SeafTransport _t;
    internal FileTagsApi(SeafTransport t) => _t = t;

    private static string Repo(string libraryId) => $"api/v2.1/repos/{Url.Seg(libraryId)}/";

    /// <summary>Lists the tags defined in a library, with the number of files carrying each.</summary>
    public async Task<IReadOnlyList<SeafRepoTag>> ListLibraryTagsAsync(string libraryId, CancellationToken ct = default)
        => (await _t.GetAsync<RepoTagList>(Repo(libraryId) + "repo-tags/", ct).ConfigureAwait(false)).RepoTags;

    /// <summary>Defines a tag in a library. <paramref name="color"/> is a hex color such as <c>#FF0000</c>.</summary>
    public async Task<SeafRepoTag> CreateLibraryTagAsync(string libraryId, string name, string color, CancellationToken ct = default)
        => (await _t.SendAsync<RepoTagResult>(HttpMethod.Post, Repo(libraryId) + "repo-tags/",
            SeafTransport.Form(("name", name), ("color", color)), ct).ConfigureAwait(false)).RepoTag;

    /// <summary>Defines several tags at once.</summary>
    public async Task<IReadOnlyList<SeafRepoTag>> CreateLibraryTagsAsync(string libraryId, IEnumerable<(string Name, string Color)> tags, CancellationToken ct = default)
        => (await _t.SendAsync<RepoTagList>(HttpMethod.Put, Repo(libraryId) + "repo-tags/",
            SeafTransport.Json(new { tags = tags.Select(t => new { name = t.Name, color = t.Color }).ToArray() }), ct).ConfigureAwait(false)).RepoTags;

    /// <summary>Renames and/or recolors a tag.</summary>
    public async Task<SeafRepoTag> UpdateLibraryTagAsync(string libraryId, int tagId, string name, string color, CancellationToken ct = default)
        => (await _t.SendAsync<RepoTagResult>(HttpMethod.Put, $"{Repo(libraryId)}repo-tags/{tagId}/",
            SeafTransport.Form(("name", name), ("color", color)), ct).ConfigureAwait(false)).RepoTag;

    /// <summary>Deletes a tag from the library and from every file carrying it.</summary>
    public Task DeleteLibraryTagAsync(string libraryId, int tagId, CancellationToken ct = default)
        => _t.SendAsync(HttpMethod.Delete, $"{Repo(libraryId)}repo-tags/{tagId}/", null, ct);

    /// <summary>Lists the tags attached to a file.</summary>
    public async Task<IReadOnlyList<SeafFileTag>> ListFileTagsAsync(string libraryId, string path, CancellationToken ct = default)
        => (await _t.GetAsync<FileTagList>(Url.Of(Repo(libraryId) + "file-tags/").Q("file_path", SeafPath.Normalize(path)), ct).ConfigureAwait(false)).FileTags;

    /// <summary>Attaches a library tag to a file and returns the link (its <see cref="SeafFileTag.FileTagId"/> removes it again).</summary>
    public async Task<SeafFileTag> TagFileAsync(string libraryId, string path, int tagId, CancellationToken ct = default)
        => (await _t.SendAsync<FileTagResult>(HttpMethod.Post, Repo(libraryId) + "file-tags/",
            SeafTransport.Form(("file_path", SeafPath.Normalize(path)), ("repo_tag_id", tagId.ToString(System.Globalization.CultureInfo.InvariantCulture))), ct).ConfigureAwait(false)).FileTag;

    /// <summary>Removes a tag from a file, by the file-tag link id.</summary>
    public Task UntagFileAsync(string libraryId, int fileTagId, CancellationToken ct = default)
        => _t.SendAsync(HttpMethod.Delete, $"{Repo(libraryId)}file-tags/{fileTagId}/", null, ct);

    /// <summary>Lists the files carrying a tag.</summary>
    public async Task<IReadOnlyList<SeafTaggedFile>> ListTaggedFilesAsync(string libraryId, int tagId, CancellationToken ct = default)
        => (await _t.GetAsync<TaggedFileList>($"{Repo(libraryId)}tagged-files/{tagId}/", ct).ConfigureAwait(false)).TaggedFiles;

    private sealed record RepoTagList { public List<SeafRepoTag> RepoTags { get; init; } = []; }
    private sealed record RepoTagResult { public SeafRepoTag RepoTag { get; init; } = new(); }
    private sealed record FileTagList { public List<SeafFileTag> FileTags { get; init; } = []; }
    private sealed record FileTagResult { public SeafFileTag FileTag { get; init; } = new(); }
    private sealed record TaggedFileList { public List<SeafTaggedFile> TaggedFiles { get; init; } = []; }
}
