using SeafClient.Internal;
using SeafClient.Models;

namespace SeafClient.Api;

/// <summary>User notifications across the server, via the admin API.</summary>
public sealed class AdminNotificationsApi
{
    private readonly SeafTransport _t;
    internal AdminNotificationsApi(SeafTransport t) => _t = t;

    /// <summary>Lists notifications, newest first, optionally only those sent to one user.</summary>
    public async Task<SeafPage<SeafAdminNotification>> ListAsync(int page = 1, int perPage = 100, string? userEmail = null, CancellationToken ct = default)
    {
        var r = await _t.GetAsync<ListResponse>(Url.Of("api/v2.1/admin/notifications/").Q("page", page).Q("per_page", perPage).Q("username", userEmail), ct).ConfigureAwait(false);
        return new SeafPage<SeafAdminNotification> { Items = r.NotificationList, TotalCount = r.Count, Page = page, PerPage = perPage };
    }

    private sealed record ListResponse { public List<SeafAdminNotification> NotificationList { get; init; } = []; public int Count { get; init; } }
}
