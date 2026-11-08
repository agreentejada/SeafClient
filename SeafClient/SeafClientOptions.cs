namespace SeafClient;

/// <summary>
/// Options for a <see cref="SeafSession"/>.
/// </summary>
public sealed class SeafClientOptions
{
    /// <summary>
    /// An externally managed <see cref="System.Net.Http.HttpClient"/> (e.g. from <c>IHttpClientFactory</c>).
    /// Its <c>BaseAddress</c> is ignored and it is not disposed by the session.
    /// When <c>null</c>, the session creates and owns its own client.
    /// </summary>
    public HttpClient? HttpClient { get; set; }

    /// <summary>Timeout for the internally created HttpClient. Ignored when <see cref="HttpClient"/> is supplied.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// How often a request is retried after 429 Too Many Requests, or after 502/503/504 / network errors
    /// for idempotent requests (GET, HEAD, PUT, DELETE). Set to 0 to disable retries.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Base delay for exponential backoff (doubled each attempt) when the server sends no Retry-After.</summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Upper bound on any single retry delay, including server-provided Retry-After values.</summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// If set, absolute file-server URLs returned by Seafile (upload/download links) have their scheme, host and port
    /// replaced by the session's server URI. Useful when Seafile's SERVICE_URL / FILE_SERVER_ROOT points at an address
    /// that is not reachable from this client (typical for Docker setups).
    /// </summary>
    public bool RewriteFileServerUrls { get; set; }

    /// <summary>User-Agent sent with each request.</summary>
    public string UserAgent { get; set; } = "SeafClient/2.0";
}
