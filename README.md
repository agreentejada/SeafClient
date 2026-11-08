# SeafClient

A strongly-typed, async .NET client for the [Seafile](https://www.seafile.com) Web API.

Version 2 is a ground-up rewrite targeting **Seafile 11–14** (tested against Seafile 14.0.9 Pro):

- `net8.0` / `net10.0`, `System.Text.Json`, no third-party dependencies
- Resource-grouped API: `session.Files`, `session.Directories`, `session.ShareLinks`, ...
- Uses the current `api/v2.1` endpoints, falling back to `api2` only where v2.1 has no equivalent
- Typed errors (`SeafException` with `StatusCode`, `IsNotFound`, ...), automatic retries on 429 and transient 5xx
- Every call takes a `CancellationToken`; works with `IHttpClientFactory`

## Quick start

```csharp
using SeafClient;
using SeafClient.Models;

using var seaf = await SeafSession.LoginAsync(new Uri("https://cloud.example.com"), "me@example.com", password);

var lib = await seaf.Libraries.FindByNameAsync("TaxData")
          ?? await seaf.Libraries.CreateAsync("TaxData");

// Idempotent: never produces "2026 (1)".
await seaf.Directories.EnsureAsync(lib.Id, "/clients/42/2026");

await using var pdf = File.OpenRead("return.pdf");
var uploaded = await seaf.Files.UploadAsync(lib.Id, "/clients/42/2026", "return.pdf", pdf,
    new SeafUploadOptions { Replace = true, Progress = new Progress<long>(b => Console.WriteLine(b)) });

foreach (var entry in await seaf.Directories.ListAsync(lib.Id, "/clients/42", recursive: true))
    Console.WriteLine($"{entry.Type} {entry.Path} {entry.Size} {entry.Modified}");

var (link, created) = await seaf.ShareLinks.GetOrCreateAsync(lib.Id, uploaded.Path,
    new SeafShareLinkOptions { ExpiresAt = DateTimeOffset.Now.AddDays(7), Password = "s3cret-pass" });
```

An existing API token can be used directly: `SeafSession.FromToken(serverUri, token)`.

## API overview

| Group | Highlights |
|---|---|
| `Server` | `PingAsync`, `AuthPingAsync`, `GetInfoAsync`, `GetAccountInfoAsync`, `GetAvatarUrlAsync` |
| `Libraries` | `ListAsync(type)`, `ListDistinctAsync`, `FindByNameAsync`, `GetAsync`, `CreateAsync`, `RenameAsync`, `DeleteAsync`, `DecryptAsync`, default library, `ShareWithGroupAsync` / `ShareWithUserAsync` / `ListSharesAsync` / `Unshare*` |
| `Directories` | `ListAsync` (filter, recursive), `GetAsync` / `TryGetAsync` / `ExistsAsync`, `CreateAsync`, `EnsureAsync`, `RenameAsync`, `CopyAsync`, `MoveAsync`, `DeleteAsync` / `DeleteIfExistsAsync` |
| `Files` | `GetAsync` / `TryGetAsync` / `ExistsAsync`, `UploadAsync` / `UploadFileAsync`, `UpdateAsync`, `OpenReadAsync`, `DownloadAsync`, `ReadAllBytesAsync`, `ReadAllTextAsync`, download/upload links, `RenameAsync`, `CopyAsync`, `MoveAsync`, `DeleteAsync`, `CreateAsync`, `GetHistoryAsync`, `LockAsync` / `UnlockAsync`, `GetThumbnailAsync` |
| `Items` | Batch `CopyAsync`, `MoveAsync`, `DeleteAsync` of several entries in one directory |
| `ShareLinks` | `ListAsync`, `GetAsync`, `CreateAsync`, `GetOrCreateAsync`, `DeleteAsync`, `GetSmartLinkAsync` |
| `Starred` | `ListAsync`, `StarAsync`, `UnstarAsync` |
| `Groups` | CRUD, members (single and bulk), `SetAdminAsync`, `ListLibrariesAsync` |
| `Metadata` | `GetStatusAsync`, `SetEnabledAsync`, `GetRecordAsync`, `UpdateRecordsAsync` (auto-batched) |
| `AdminUsers` | `ListAsync` / `ListAllAsync`, `SearchAsync`, `GetAsync` / `TryGetAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync` (staff only) |

`SeafPath` normalizes and combines library paths (`SeafPath.Combine("42", "Uploads/", "/2026")` → `/42/Uploads/2026`).
All returned paths are normalized (leading `/`, no trailing `/`).

## Seafile behaviours worth knowing

- **mkdir never fails on an existing name**; Seafile silently creates `name (1)`. `Directories.CreateAsync` checks first and throws a
  409 `SeafException`; `Directories.EnsureAsync` is the idempotent variant.
- **Uploads auto-rename** unless `Replace = true`. `SeafUploadedFile.Name` / `.Path` hold the name actually stored.
- **File delete is idempotent** on the server; directory delete returns 404 for a missing folder (use `DeleteIfExistsAsync`).
- **File history and metadata records are indexed asynchronously**; they can take a few seconds to show up.
- **Downloads are decoded as UTF-8** by `ReadAllTextAsync` because the file server often declares a wrong charset.
- If Seafile's `SERVICE_URL` / `FILE_SERVER_ROOT` is not reachable from the client (common with Docker), set
  `SeafClientOptions.RewriteFileServerUrls = true` to rewrite upload/download links to the session's host.

## Options

```csharp
var options = new SeafClientOptions
{
    HttpClient = httpClientFactory.CreateClient("seafile"), // optional; not disposed by the session
    MaxRetries = 3,                                          // 429 always; 502/503/504 + network errors for GET/PUT/DELETE
    RetryBaseDelay = TimeSpan.FromSeconds(1),                // exponential backoff unless the server sends Retry-After
    Timeout = TimeSpan.FromMinutes(5),                       // only for the internally created HttpClient
};
```

## Tests

`SeafClient.Tests` contains unit tests and live integration tests. The live tests create (and delete) a temporary library
and group, and are skipped unless these environment variables are set:

```bash
export SEAFILE_TEST_URL=http://localhost:4500
export SEAFILE_TEST_USER=me@example.com
export SEAFILE_TEST_PASSWORD=...
dotnet test
```

The account should be a system admin for the admin-API tests.

## Migrating from 1.x

| 1.x | 2.x |
|---|---|
| `SeafSession.Establish(uri, user, char[] pwd)` | `SeafSession.LoginAsync(uri, user, pwd)` |
| `session.Ping()` | `session.Server.PingAsync()` |
| `ListLibraries()` + `ListSharedLibraries()` | `Libraries.ListAsync(type)` / `ListDistinctAsync()` / `FindByNameAsync()` |
| `ListDirectory(lib, path)` / `SendRequest(new ListDirectoryEntriesRequest(...))` | `Directories.ListAsync(lib, path, filter, recursive)` |
| `GetDirectoryDetail` / `CreateDirectory` / `DeleteDirectory` | `Directories.GetAsync` / `CreateAsync` or `EnsureAsync` / `DeleteAsync` |
| `GetFileDetail` (returned `SeafDirEntry`) | `Files.GetAsync` (returns `SeafFile`) |
| `UploadSingle(lib, dir, name, stream, createParents)` | `Files.UploadAsync(lib, dir, name, stream, new SeafUploadOptions { CreateParents = ... })` |
| `GetFileDownloadLink` | `Files.GetDownloadLinkAsync`, or `OpenReadAsync` / `ReadAllTextAsync` |
| `CopyFile` / `MoveFile` | `Files.CopyAsync` / `Files.MoveAsync` (directory destination) |
| `ListSharedLinks` / `CreateShareLink` / `DeleteShareLink(link.Id)` | `ShareLinks.ListAsync` / `CreateAsync` / `DeleteAsync(link.Token)` |
| `GetSmartLink` | `ShareLinks.GetSmartLinkAsync` |
| `SeafDirEntry.Timestamp` (`DateTime`, local) | `SeafDirEntry.Modified` (`DateTimeOffset?`) |
| `SeafException.SeafError.HttpStatusCode` | `SeafException.StatusCode` / `IsNotFound` |
| namespaces `SeafClient.Types`, `.Requests.*`, `.Exceptions` | `SeafClient.Models`; exceptions live in `SeafClient` |

## License

MIT. Originally created by René Bergelt.
