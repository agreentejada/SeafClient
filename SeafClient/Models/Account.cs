using System.Text.Json.Serialization;

namespace SeafClient.Models;

/// <summary>GET /api2/server-info/</summary>
public sealed record SeafServerInfo
{
    public string Version { get; init; } = "";
    public int EncryptedLibraryVersion { get; init; }
    public IReadOnlyList<string> Features { get; init; } = [];

    /// <summary>Parsed <see cref="Version"/>, or <c>null</c> if it is not a plain dotted version.</summary>
    [JsonIgnore]
    public Version? ParsedVersion => System.Version.TryParse(Version, out var v) ? v : null;

    [JsonIgnore]
    public bool IsPro => Features.Contains("seafile-pro");
}

/// <summary>GET /api2/account/info/</summary>
public sealed record SeafAccountInfo
{
    public string Email { get; init; } = "";
    public string Name { get; init; } = "";
    public string? LoginId { get; init; }
    public string? ContactEmail { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Department { get; init; }
    public string? Institution { get; init; }

    /// <summary>Bytes used.</summary>
    public long Usage { get; init; }

    /// <summary>Quota in bytes; negative values mean unlimited (-2) or unknown.</summary>
    public long Total { get; init; }

    public string? SpaceUsage { get; init; }
    public bool IsStaff { get; init; }

    [JsonIgnore]
    public bool HasUnlimitedQuota => Total < 0;
}
