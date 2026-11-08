using System.Globalization;
using SeafClient.Internal;
using SeafClient.Models;

namespace SeafClient.Api;

/// <summary>
/// Organizations (multi-tenancy). The server must have <c>MULTI_TENANCY</c> enabled; otherwise every call fails with
/// 403 "Feature is not enabled.".
/// </summary>
public sealed class AdminOrganizationsApi
{
    private readonly SeafTransport _t;
    internal AdminOrganizationsApi(SeafTransport t) => _t = t;

    private static string Base(int orgId) => $"api/v2.1/admin/organizations/{orgId}/";

    public async Task<SeafPage<SeafOrganization>> ListAsync(int page = 1, int perPage = 25, bool? isActive = null, CancellationToken ct = default)
    {
        var r = await _t.GetAsync<ListResponse>(Url.Of("api/v2.1/admin/organizations/").Q("page", page).Q("per_page", perPage)
            .Q("is_active", isActive), ct).ConfigureAwait(false);
        return new SeafPage<SeafOrganization> { Items = r.Organizations, TotalCount = r.TotalCount, Page = page, PerPage = perPage };
    }

    public async Task<IReadOnlyList<SeafOrganization>> SearchAsync(string query, CancellationToken ct = default)
        => (await _t.GetAsync<SearchResponse>(Url.Of("api/v2.1/admin/search-organization/").Q("query", query), ct).ConfigureAwait(false)).OrganizationList;

    public Task<SeafOrganization> GetAsync(int orgId, CancellationToken ct = default)
        => _t.GetAsync<SeafOrganization>(Base(orgId), ct);

    public Task<SeafOrganization> CreateAsync(SeafCreateOrganizationRequest request, CancellationToken ct = default)
        => _t.SendAsync<SeafOrganization>(HttpMethod.Post, "api/v2.1/admin/organizations/", SeafTransport.Form(
            ("org_name", request.OrgName),
            ("owner_email", request.OwnerEmail),
            ("owner_password", request.OwnerPassword),
            ("quota", request.QuotaMb?.ToString(CultureInfo.InvariantCulture)),
            ("member_limit", request.MemberLimit?.ToString(CultureInfo.InvariantCulture))), ct);

    public Task<SeafOrganization> UpdateAsync(int orgId, SeafUpdateOrganizationRequest request, CancellationToken ct = default)
        => _t.SendAsync<SeafOrganization>(HttpMethod.Put, Base(orgId), SeafTransport.Form(
            ("org_name", request.OrgName),
            ("max_user_number", request.MaxUserNumber?.ToString(CultureInfo.InvariantCulture)),
            ("quota", request.QuotaMb?.ToString(CultureInfo.InvariantCulture)),
            ("role", request.Role),
            ("is_active", request.IsActive == null ? null : request.IsActive.Value ? "true" : "false")), ct);

    /// <summary>Deletes an organization with all its users, groups and libraries.</summary>
    public Task DeleteAsync(int orgId, CancellationToken ct = default)
        => _t.SendAsync(HttpMethod.Delete, Base(orgId), null, ct);

    private sealed record ListResponse { public List<SeafOrganization> Organizations { get; init; } = []; public int TotalCount { get; init; } }
    private sealed record SearchResponse { public List<SeafOrganization> OrganizationList { get; init; } = []; }
}
