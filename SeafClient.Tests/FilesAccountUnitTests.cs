using System.Net;

namespace SeafClient.Tests;

public class FilesAccountUnitTests
{
    private sealed class Recorder(string body) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];
        public List<string> Bodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(request);
            Bodies.Add(request.Content == null ? "" : await request.Content.ReadAsStringAsync(ct));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };
        }
    }

    private static SeafSession Session(Recorder r) => SeafSession.FromToken(new Uri("http://seaf.test/"), "tok",
        new SeafClientOptions { HttpClient = new HttpClient(r), MaxRetries = 0 });

    [Fact]
    public async Task SearchFiles_BuildsQuery()
    {
        var r = new Recorder("""{"total":1,"has_more":false,"results":[{"repo_id":"r","name":"a.txt","fullpath":"/d/a.txt","is_dir":false,"size":3,"last_modified":1520836447}]}""");
        using var s = Session(r);
        var result = await s.Search.SearchFilesAsync(new SeafSearchQuery
        {
            Query = "tax return",
            LibraryId = "lib",
            Path = "d/",
            FileTypes = ["Document", "PDF"],
            Extensions = ["md", "txt"],
            SizeFrom = 10,
            ModifiedFrom = DateTimeOffset.FromUnixTimeSeconds(100),
            EntryType = SeafEntryType.File,
            FileNamesOnly = true,
        });

        var q = r.Requests[0].RequestUri!.Query;
        Assert.Contains("q=tax%20return", q);
        Assert.Contains("search_repo=lib", q);
        Assert.Contains("search_path=%2Fd", q);
        Assert.Contains("search_ftypes=custom", q);
        Assert.Contains("ftype=Document&ftype=PDF", q);
        Assert.Contains("input_fexts=md%2Ctxt", q);
        Assert.Contains("size_from=10", q);
        Assert.Contains("time_from=100", q);
        Assert.Contains("obj_type=file", q);
        Assert.Contains("search_filename_only=true", q);

        var hit = Assert.Single(result.Results);
        Assert.Equal("/d/a.txt", hit.Path);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1520836447), hit.Modified);
    }

    [Fact]
    public async Task SearchFiles_DefaultScope_HasNoCustomTypes()
    {
        var r = new Recorder("""{"total":0,"has_more":false,"results":[]}""");
        using var s = Session(r);
        await s.Search.SearchFilesAsync(new SeafSearchQuery { Query = "x", Scope = SeafSearchScope.Shared });
        var q = r.Requests[0].RequestUri!.Query;
        Assert.Contains("search_repo=shared", q);
        Assert.DoesNotContain("search_ftypes", q);
        Assert.DoesNotContain("search_filename_only", q);
    }

    [Fact]
    public async Task Zip_RepeatsDirentsAndBuildsDownloadUri()
    {
        var r = new Recorder("""{"zip_token":"tok-1"}""");
        using var s = Session(r);
        var task = await s.Items.CreateZipAsync("lib", "/a b", ["x", "y z"]);
        Assert.Equal("?parent_dir=%2Fa%20b&dirents=x&dirents=y%20z", r.Requests[0].RequestUri!.Query);
        Assert.Equal("http://seaf.test/seafhttp/zip/tok-1", task.DownloadUri.AbsoluteUri);
    }

    [Fact]
    public async Task AsyncTask_EmptyId_IsCompleted_AndWaitDoesNotPoll()
    {
        var r = new Recorder("""{"task_id":""}""");
        using var s = Session(r);
        var task = await s.Items.StartCopyAsync("a", "/", ["f"], "a", "/d");
        Assert.True(task.IsCompleted);
        Assert.True((await s.Items.WaitAsync(task)).Successful);
        Assert.Single(r.Requests);
        Assert.Contains("\"src_dirents\":[\"f\"]", r.Bodies[0]);
    }

    [Fact]
    public async Task GroupTransfer_SendsOwner()
    {
        var r = new Recorder("""{"id":7,"name":"g","owner":"new@x.y","admins":["new@x.y"]}""");
        using var s = Session(r);
        var g = await s.Groups.TransferAsync(7, "new@x.y");
        Assert.Equal(HttpMethod.Put, r.Requests[0].Method);
        Assert.Equal("/api/v2.1/groups/7/", r.Requests[0].RequestUri!.AbsolutePath);
        Assert.Equal("owner=new%40x.y", r.Bodies[0]);
        Assert.Equal("new@x.y", g.Owner);
    }

    [Fact]
    public async Task InvitationsBatch_RepeatsAccepter()
    {
        var r = new Recorder("""{"success":[],"failed":[{"email":"b@x.y","error_msg":"already invited"}]}""");
        using var s = Session(r);
        var result = await s.Invitations.CreateManyAsync(["a@x.y", "b@x.y"]);
        Assert.Equal("type=guest&accepter=a%40x.y&accepter=b%40x.y", r.Bodies[0]);
        Assert.Equal("already invited", Assert.Single(result.Failed).ErrorMsg);
    }
}
