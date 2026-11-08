using System.Text.Json.Serialization;

namespace SeafClient.Models;

/// <summary>A started asynchronous copy/move (<c>/api/v2.1/repos/async-batch-copy-item/</c>).</summary>
public sealed record SeafAsyncTask
{
    /// <summary>
    /// The task id. Empty when the server finished the operation synchronously, which Seafile does for copies and
    /// moves inside the same library.
    /// </summary>
    public string TaskId { get; init; } = "";

    /// <summary>True when the operation already finished and there is nothing to poll.</summary>
    [JsonIgnore]
    public bool IsCompleted => string.IsNullOrEmpty(TaskId);
}

/// <summary>Progress of an asynchronous copy/move (<c>/api/v2.1/query-copy-move-progress/</c>).</summary>
public sealed record SeafTaskProgress
{
    /// <summary>Number of objects processed so far.</summary>
    public long Done { get; init; }

    /// <summary>Total number of objects (0 until the server has counted them, and on failure).</summary>
    public long Total { get; init; }

    public bool Canceled { get; init; }
    public bool Failed { get; init; }
    public bool Successful { get; init; }

    /// <summary>True once the task succeeded, failed or was canceled.</summary>
    [JsonIgnore]
    public bool IsFinished => Successful || Failed || Canceled;

    internal static SeafTaskProgress CompletedSynchronously { get; } = new() { Successful = true };
}

/// <summary>A server-side zip of one or more entries, ready to download from <see cref="DownloadUri"/>.</summary>
public sealed record SeafZipTask
{
    /// <summary>The zip token. Seafile accepts it for a single download only.</summary>
    public string ZipToken { get; init; } = "";

    /// <summary>The file-server URL that streams the zip (<c>/seafhttp/zip/{token}</c>). Needs no authentication.</summary>
    [JsonIgnore]
    public Uri DownloadUri { get; init; } = null!;
}

/// <summary>Progress of a zip task (<c>/api/v2.1/query-zip-progress/</c>).</summary>
public sealed record SeafZipProgress
{
    public long Zipped { get; init; }
    public long Total { get; init; }
    public long Failed { get; init; }
    public string? FailedReason { get; init; }
    public long Canceled { get; init; }

    [JsonIgnore]
    public bool IsFinished => Failed > 0 || Canceled > 0 || (Total > 0 && Zipped >= Total);
}
