using System.Net;
using System.Text.Json;
using SeafClient.Api;

namespace SeafClient.Tests;

public class MetadataUnitTests
{
    private sealed class RecordingHandler(Func<HttpRequestMessage, string> respond) : HttpMessageHandler
    {
        public List<(HttpMethod Method, string Url, string Body, string? Auth)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var body = request.Content == null ? "" : await request.Content.ReadAsStringAsync(ct);
            Requests.Add((request.Method, request.RequestUri!.PathAndQuery, body, request.Headers.Authorization?.ToString()));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(respond(request)) };
        }
    }

    private static SeafClientOptions Options(HttpMessageHandler h) => new() { HttpClient = new HttpClient(h), MaxRetries = 0 };

    [Fact]
    public void ParseQuery_ColumnsAndRecords()
    {
        var r = MetadataCore.ParseQuery("""
            {"metadata":[{"key":"_id","name":"_id","type":"text","data":null},{"key":"aB1c","name":"Client","type":"text"}],
             "results":[{"_id":"r1","_name":"a.txt","_parent_dir":"/","_is_dir":false,"Client":"Acme"}]}
            """);
        Assert.Equal(2, r.Columns.Count);
        Assert.Equal("aB1c", r.Columns[1].Key);
        var rec = Assert.Single(r.Records);
        Assert.Equal("r1", rec.Id);
        Assert.Equal("Acme", rec.GetString("Client"));
        Assert.False(rec.IsDir);

        // tags-files answers a bare [] when nothing matches.
        Assert.Empty(MetadataCore.ParseQuery("[]").Records);
    }

    [Fact]
    public async Task UpdateRecords_GroupsByIdenticalValues()
    {
        var h = new RecordingHandler(_ => """{"success":true}""");
        using var transport = new SeafClient.Internal.SeafTransport(new Uri("http://seaf.test/"), Options(h)) { Token = "tok" };
        var core = new MetadataCore(transport, "api/v2.1/repos/lib/metadata/");
        await core.UpdateRecordsAsync(
        [
            new SeafMetadataUpdate("r1", new Dictionary<string, object?> { ["_description"] = "x" }),
            new SeafMetadataUpdate("r2", new Dictionary<string, object?> { ["_description"] = "y" }),
            new SeafMetadataUpdate("r3", new Dictionary<string, object?> { ["_description"] = "x" }),
        ], maxBatchSize: 100, CancellationToken.None);

        // Records with different values never share a request (Seafile 14 mixes their values up).
        Assert.Equal(2, h.Requests.Count);
        Assert.All(h.Requests, r => Assert.Equal("/api/v2.1/repos/lib/metadata/records/", r.Url));
        var first = JsonDocument.Parse(h.Requests[0].Body).RootElement.GetProperty("records_data");
        Assert.Equal(["r1", "r3"], first.EnumerateArray().Select(e => e.GetProperty("record_id").GetString()));
        Assert.Equal("x", first[1].GetProperty("record").GetProperty("_description").GetString());
    }

    [Fact]
    public async Task RepoTokenSession_UsesViaRepoTokenPathsAndToken()
    {
        var h = new RecordingHandler(req => req.RequestUri!.AbsolutePath.EndsWith("repo-info/")
            ? """{"repo_id":"lib-1","repo_name":"L","size":3,"file_count":1,"last_modified":"2026-11-08T01:46:23+00:00"}"""
            : """{"dirent_list":[{"type":"file","id":"abc","name":"a.txt","mtime":"2026-11-08T01:46:23+00:00","parent_dir":"/d/","size":3}]}""");
        using var repo = SeafRepoTokenSession.FromToken(new Uri("http://seaf.test/"), "rt", Options(h));

        var entries = await repo.Directories.ListAsync("/d", SeafListFilter.FilesOnly);
        var e = Assert.Single(entries);
        Assert.Equal("/d/a.txt", e.Path);
        Assert.Equal("lib-1", e.LibraryId);
        Assert.Contains(h.Requests, r => r.Url == "/api/v2.1/via-repo-token/dir/?path=%2Fd&type=f");
        Assert.All(h.Requests, r => Assert.Equal("Token rt", r.Auth));

        await repo.Directories.ListAsync("/");
        Assert.Single(h.Requests, r => r.Url.Contains("repo-info")); // library id is cached
    }

    [Fact]
    public async Task RepoTokenFileTags_ParsesPerRecordResult()
    {
        var h = new RecordingHandler(_ => """{"success":["r1"],"fail":["r2"]}""");
        using var repo = SeafRepoTokenSession.FromToken(new Uri("http://seaf.test/"), "rt", Options(h));
        var r = await repo.Metadata.Tags.SetFileTagsAsync(new Dictionary<string, IEnumerable<string>> { ["r1"] = ["t"], ["r2"] = ["t"] });
        Assert.Equal(["r1"], r.Success);
        Assert.Equal(["r2"], r.Fail);
        Assert.Equal("/api/v2.1/via-repo-token/metadata/file-tags/", h.Requests[0].Url);
    }
}
