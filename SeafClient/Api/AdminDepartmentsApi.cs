using System.Globalization;
using SeafClient.Internal;
using SeafClient.Models;

namespace SeafClient.Api;

/// <summary>Departments (address-book groups), Seafile Pro. Departments are groups, so <see cref="AdminGroupsApi"/> also manages their members and quota.</summary>
public sealed class AdminDepartmentsApi
{
    private readonly SeafTransport _t;
    internal AdminDepartmentsApi(SeafTransport t) => _t = t;

    /// <summary>Lists top-level departments.</summary>
    public async Task<IReadOnlyList<SeafDepartment>> ListAsync(CancellationToken ct = default)
        => (await _t.GetAsync<ListResponse>("api/v2.1/admin/address-book/groups/", ct).ConfigureAwait(false)).Data;

    /// <summary>Gets a department with its child departments and members.</summary>
    public Task<SeafDepartmentDetail> GetAsync(int departmentId, bool includeAncestors = false, CancellationToken ct = default)
        => _t.GetAsync<SeafDepartmentDetail>(Url.Of($"api/v2.1/admin/address-book/groups/{departmentId}/").Q("return_ancestors", includeAncestors ? (bool?)true : null), ct);

    /// <summary>Creates a department, top-level unless <paramref name="parentId"/> is given.</summary>
    public Task<SeafDepartment> CreateAsync(string name, int? parentId = null, string? ownerEmail = null, CancellationToken ct = default)
        => _t.SendAsync<SeafDepartment>(HttpMethod.Post, "api/v2.1/admin/address-book/groups/", SeafTransport.Form(
            ("group_name", name),
            ("parent_group", parentId?.ToString(CultureInfo.InvariantCulture)),
            ("group_owner", ownerEmail)), ct);

    /// <summary>Deletes a department. The server refuses if it still has child departments or libraries.</summary>
    public Task DeleteAsync(int departmentId, CancellationToken ct = default)
        => _t.SendAsync(HttpMethod.Delete, $"api/v2.1/admin/address-book/groups/{departmentId}/", null, ct);

    private sealed record ListResponse { public List<SeafDepartment> Data { get; init; } = []; }
}
