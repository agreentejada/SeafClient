using System.Text.Json.Serialization;

namespace SeafClient.Models;

public sealed record SeafGroup
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string? Owner { get; init; }
    public int ParentGroupId { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? Created { get; init; }

    public IReadOnlyList<string> Admins { get; init; } = [];
}

public sealed record SeafGroupMember
{
    public int GroupId { get; init; }
    public string Email { get; init; } = "";
    public string Name { get; init; } = "";
    public string? ContactEmail { get; init; }
    public string? LoginId { get; init; }
    public bool IsAdmin { get; init; }

    /// <summary><c>Owner</c>, <c>Admin</c> or <c>Member</c>.</summary>
    public string? Role { get; init; }
}

/// <summary>Result of adding several members at once.</summary>
public sealed record SeafBulkMemberResult
{
    public IReadOnlyList<SeafGroupMember> Success { get; init; } = [];
    public IReadOnlyList<SeafBulkMemberFailure> Failed { get; init; } = [];
}

public sealed record SeafBulkMemberFailure
{
    public string Email { get; init; } = "";
    public string ErrorMsg { get; init; } = "";
}
