using System.IO.Compression;
using System.Net;
using System.Text;

namespace SeafClient.Tests;

[Collection(LiveCollection.Name)]
public class AsyncItemsAndZipTests(LiveFixture f)
{
    private string Root([System.Runtime.CompilerServices.CallerMemberName] string name = "") => "/items-" + name;

    private Task Put(string dir, string name, string text = "x")
        => f.Session.Files.UploadAsync(f.LibraryId, dir, name, new MemoryStream(Encoding.UTF8.GetBytes(text)), new SeafUploadOptions { CreateParents = true });

    [LiveFact]
    public async Task AsyncCopy_AcrossLibraries_And_AsyncMove_InLibrary()
    {
        var root = Root();
        await Put(root + "/src/folder", "a.txt");
        await Put(root + "/src", "b.txt");

        var other = await f.Session.Libraries.CreateAsync($"SeafClientTmp-{Guid.NewGuid():N}"[..24]);
        try
        {
            var copy = await f.Session.Items.StartCopyAsync(f.LibraryId, root + "/src", ["folder", "b.txt"], other.Id, "/");
            Assert.False(copy.IsCompleted);
            var progress = await f.Session.Items.WaitAsync(copy, TimeSpan.FromMilliseconds(200));
            Assert.True(progress.Successful);
            Assert.True(await f.Session.Files.ExistsAsync(other.Id, "/folder/a.txt"));
            Assert.True(await f.Session.Files.ExistsAsync(other.Id, "/b.txt"));

            // Canceling a task that already finished is accepted.
            await f.Session.Items.CancelAsync(copy.TaskId);
        }
        finally
        {
            await f.Session.Libraries.DeleteAsync(other.Id);
        }

        // Inside one library the server finishes before answering and returns no task id.
        await f.Session.Directories.EnsureAsync(f.LibraryId, root + "/dst");
        var move = await f.Session.Items.StartMoveAsync(f.LibraryId, root + "/src", ["b.txt"], f.LibraryId, root + "/dst");
        Assert.True(move.IsCompleted);
        Assert.True((await f.Session.Items.WaitAsync(move)).Successful);
        Assert.True(await f.Session.Files.ExistsAsync(f.LibraryId, root + "/dst/b.txt"));
        Assert.False(await f.Session.Files.ExistsAsync(f.LibraryId, root + "/src/b.txt"));
    }

    [LiveFact]
    public async Task MoveMerge_CombinesSameNamedFolders()
    {
        var root = Root();
        await Put(root + "/src/x", "from-src.txt");
        await Put(root + "/dst/x", "from-dst.txt");

        await f.Session.Directories.MoveMergeAsync(f.LibraryId, root + "/src/x", f.LibraryId, root + "/dst");

        var merged = await f.Session.Directories.ListAsync(f.LibraryId, root + "/dst/x");
        Assert.Equal(["from-dst.txt", "from-src.txt"], merged.Select(e => e.Name).Order());
        Assert.Single(await f.Session.Directories.ListAsync(f.LibraryId, root + "/dst"));
        Assert.False(await f.Session.Directories.ExistsAsync(f.LibraryId, root + "/src/x"));
    }

    [LiveFact]
    public async Task Zip_DownloadsFolderAndFiles()
    {
        var root = Root();
        await Put(root + "/folder/sub", "deep.txt", "deep");
        await Put(root, "top.txt", "top");

        var task = await f.Session.Items.CreateZipAsync(f.LibraryId, root, ["folder"]);
        Assert.False(string.IsNullOrEmpty(task.ZipToken));
        Assert.EndsWith(task.ZipToken, task.DownloadUri.AbsoluteUri);
        Assert.Null(await f.Session.Items.GetZipProgressAsync(task.ZipToken)); // Seafile 14 streams zips; nothing to track.

        using var ms = new MemoryStream();
        await f.Session.Items.DownloadZipAsync(f.LibraryId, root, ["folder", "top.txt"], ms);
        ms.Position = 0;
        using var zip = new ZipArchive(ms);
        var names = zip.Entries.Select(e => e.FullName).ToList();
        Assert.Contains("folder/sub/deep.txt", names);
        Assert.Contains("top.txt", names);
        using var reader = new StreamReader(zip.GetEntry("top.txt")!.Open());
        Assert.Equal("top", await reader.ReadToEndAsync());

        await Assert.ThrowsAsync<ArgumentException>(() => f.Session.Items.CreateZipAsync(f.LibraryId, root, []));
    }
}

[Collection(LiveCollection.Name)]
public class CommentsAndTagsTests(LiveFixture f)
{
    [LiveFact]
    public async Task FileComments_Lifecycle()
    {
        var file = (await f.Session.Files.UploadAsync(f.LibraryId, "/comments", "doc.txt", new MemoryStream("c"u8.ToArray()), new SeafUploadOptions { CreateParents = true })).Path;

        Assert.Empty(await f.Session.FileComments.ListAsync(f.LibraryId, file));
        var c = await f.Session.FileComments.CreateAsync(f.LibraryId, file, "first", detail: "{\"pos\":1}");
        Assert.Equal("first", c.Comment);
        Assert.Equal("{\"pos\":1}", c.Detail);
        Assert.Equal("doc.txt", c.ItemName);
        Assert.Equal(LiveServer.User, c.UserEmail, ignoreCase: true);
        Assert.NotNull(c.Created);

        var updated = await f.Session.FileComments.UpdateAsync(f.LibraryId, file, c.Id, comment: "edited", resolved: true);
        Assert.Equal("edited", updated.Comment);
        Assert.True(updated.Resolved);
        Assert.Equal("{\"pos\":1}", updated.Detail);

        var reply = await f.Session.FileComments.AddReplyAsync(f.LibraryId, file, c.Id, "a reply");
        Assert.Equal("a reply", reply.Reply);
        Assert.Equal(c.Id, reply.CommentId);
        Assert.Single(await f.Session.FileComments.ListRepliesAsync(f.LibraryId, file, c.Id));

        var uuid = await f.Session.FileComments.GetFileUuidAsync(f.LibraryId, file);
        var fetched = await f.Session.FileComments.GetByUuidAsync(f.LibraryId, uuid, c.Id);
        Assert.Equal("edited", fetched.Comment);
        var listed = Assert.Single(await f.Session.FileComments.ListAsync(f.LibraryId, file, resolved: true));
        Assert.Single(listed.Replies);
        Assert.Empty(await f.Session.FileComments.ListAsync(f.LibraryId, file, resolved: false));
        Assert.Equal(1, await f.Session.FileComments.CountAsync(f.LibraryId, file));

        await f.Session.FileComments.DeleteReplyAsync(f.LibraryId, file, c.Id, reply.Id);
        Assert.Empty(await f.Session.FileComments.ListRepliesAsync(f.LibraryId, file, c.Id));
        await f.Session.FileComments.DeleteAsync(f.LibraryId, file, c.Id);
        Assert.Equal(0, await f.Session.FileComments.CountAsync(f.LibraryId, file));
    }

    [LiveFact]
    public async Task LegacyFileTags_Lifecycle()
    {
        var file = (await f.Session.Files.UploadAsync(f.LibraryId, "/tags", "t.txt", new MemoryStream("t"u8.ToArray()), new SeafUploadOptions { CreateParents = true })).Path;
        var tags = f.Session.FileTags;

        var red = await tags.CreateLibraryTagAsync(f.LibraryId, "red", "#FF0000");
        Assert.Equal("red", red.Name);
        Assert.Equal("#FF0000", red.Color);
        var bulk = await tags.CreateLibraryTagsAsync(f.LibraryId, [("blue", "#0000FF"), ("green", "#00FF00")]);
        Assert.Equal(["blue", "green"], bulk.Select(t => t.Name));

        var renamed = await tags.UpdateLibraryTagAsync(f.LibraryId, red.Id, "crimson", "#DC143C");
        Assert.Equal("crimson", renamed.Name);

        var link = await tags.TagFileAsync(f.LibraryId, file, red.Id);
        Assert.Equal(red.Id, link.RepoTagId);
        var onFile = Assert.Single(await tags.ListFileTagsAsync(f.LibraryId, file));
        Assert.Equal("crimson", onFile.TagName);
        var tagged = Assert.Single(await tags.ListTaggedFilesAsync(f.LibraryId, red.Id));
        Assert.Equal(file, tagged.Path);
        Assert.Equal(1, (await tags.ListLibraryTagsAsync(f.LibraryId)).Single(t => t.Id == red.Id).FilesCount);

        await tags.UntagFileAsync(f.LibraryId, link.FileTagId);
        Assert.Empty(await tags.ListFileTagsAsync(f.LibraryId, file));

        foreach (var t in await tags.ListLibraryTagsAsync(f.LibraryId))
            await tags.DeleteLibraryTagAsync(f.LibraryId, t.Id);
        Assert.Empty(await tags.ListLibraryTagsAsync(f.LibraryId));
    }
}

[Collection(LiveCollection.Name)]
public class SearchAccountGroupTests(LiveFixture f)
{
    [LiveFact]
    public async Task Search_FileNames_Users_And_FullText()
    {
        var name = $"needle-{Guid.NewGuid():N}"[..15] + ".txt";
        await f.Session.Files.UploadAsync(f.LibraryId, "/search/deep", name, new MemoryStream("n"u8.ToArray()), new SeafUploadOptions { CreateParents = true });

        var hit = Assert.Single(await f.Session.Search.SearchFileNamesAsync(f.LibraryId, name[..12]));
        Assert.Equal("/search/deep/" + name, hit.Path);
        Assert.Equal(SeafEntryType.File, hit.Type);
        Assert.Equal(name, hit.Name);

        var me = await f.Session.Search.SearchUsersAsync(LiveServer.User!.Split('@')[0]);
        Assert.Contains(me, u => string.Equals(u.Email, LiveServer.User, StringComparison.OrdinalIgnoreCase));

        // Full-text search needs a search backend; either it answers or it says it is not supported.
        try
        {
            var result = await f.Session.Search.SearchFilesAsync(new SeafSearchQuery { Query = name, LibraryId = f.LibraryId, FileNamesOnly = true, Extensions = ["txt"] });
            Assert.True(result.Total >= result.Results.Count);
        }
        catch (SeafException e) when (e.IsNotFound)
        {
            Assert.Contains("not supported", e.ServerMessage, StringComparison.OrdinalIgnoreCase);
        }
    }

    [LiveFact]
    public async Task Account_Profile_Devices_ClientLogin_Activities_Avatar()
    {
        var acct = f.Session.Account;
        var profile = await acct.GetProfileAsync();
        Assert.Equal(LiveServer.User, profile.Email, ignoreCase: true);

        // Toggle a reversible field and restore it.
        try
        {
            var changed = await acct.UpdateProfileAsync(new SeafProfileUpdate { ListInAddressBook = !profile.ListInAddressBook });
            Assert.Equal(!profile.ListInAddressBook, changed.ListInAddressBook);
        }
        finally
        {
            await acct.UpdateProfileAsync(new SeafProfileUpdate { ListInAddressBook = profile.ListInAddressBook });
        }
        Assert.Equal(profile.ListInAddressBook, (await acct.GetProfileAsync()).ListInAddressBook);

        Assert.NotNull(await acct.ListDevicesAsync()); // read only: never unlink real devices

        var loginUrl = await acct.GetClientLoginUrlAsync();
        Assert.Contains("/client-login/?token=", loginUrl.AbsoluteUri);

        await f.Session.Directories.EnsureAsync(f.LibraryId, "/activity");
        var events = await acct.ListActivitiesAsync(1, 5);
        Assert.InRange(events.Count, 1, 5);
        Assert.All(events, e => Assert.False(string.IsNullOrEmpty(e.OpType)));

        // A disallowed extension is rejected by the server, proving the multipart request arrives without changing the avatar.
        var ex = await Assert.ThrowsAsync<SeafException>(() => acct.UploadAvatarAsync(new MemoryStream("not an image"u8.ToArray()), "avatar.txt"));
        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
        Assert.Contains(".txt", ex.ServerMessage);
    }

    [LiveFact]
    public async Task Invitations_AreRejectedOrWork()
    {
        try
        {
            var list = await f.Session.Invitations.ListAsync();
            var inv = await f.Session.Invitations.CreateAsync($"seafclient-{Guid.NewGuid():N}@example.invalid");
            try
            {
                Assert.Equal("guest", inv.Type);
                Assert.Equal(inv.Token, (await f.Session.Invitations.GetAsync(inv.Token)).Token);
            }
            finally
            {
                await f.Session.Invitations.DeleteAsync(inv.Token);
            }
        }
        catch (SeafException e) when (e.StatusCode == HttpStatusCode.Forbidden)
        {
            Assert.Contains("permission", e.ServerMessage, StringComparison.OrdinalIgnoreCase);
        }
    }

    [LiveFact]
    public async Task Groups_GetMember()
    {
        // TransferAsync is not exercised live: the server accepts any owner email, which would orphan the test group.
        var group = await f.Session.Groups.CreateAsync($"SeafClientC-{Guid.NewGuid():N}"[..22]);
        try
        {
            var me = await f.Session.Groups.GetMemberAsync(group.Id, LiveServer.User!);
            Assert.Equal("Owner", me.Role);
            Assert.Equal(group.Id, me.GroupId);
            Assert.Null(await f.Session.Groups.TryGetMemberAsync(group.Id, "nobody-" + Guid.NewGuid().ToString("N") + "@example.invalid"));
        }
        finally
        {
            await f.Session.Groups.DeleteAsync(group.Id);
        }
    }
}
