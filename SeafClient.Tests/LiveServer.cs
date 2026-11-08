namespace SeafClient.Tests;

/// <summary>
/// Connection settings for the live integration tests, read from environment variables:
/// SEAFILE_TEST_URL, SEAFILE_TEST_USER, SEAFILE_TEST_PASSWORD. Tests are skipped when they are not set.
/// The account should be a system admin for the admin-API tests.
/// </summary>
public static class LiveServer
{
    public static string? Url => Environment.GetEnvironmentVariable("SEAFILE_TEST_URL");
    public static string? User => Environment.GetEnvironmentVariable("SEAFILE_TEST_USER");
    public static string? Password => Environment.GetEnvironmentVariable("SEAFILE_TEST_PASSWORD");
    public static bool IsConfigured => !string.IsNullOrEmpty(Url) && !string.IsNullOrEmpty(User) && Password != null;
}

/// <summary>A fact that only runs when a live Seafile server is configured.</summary>
public sealed class LiveFactAttribute : FactAttribute
{
    public LiveFactAttribute()
    {
        if (!LiveServer.IsConfigured)
            Skip = "Set SEAFILE_TEST_URL, SEAFILE_TEST_USER and SEAFILE_TEST_PASSWORD to run live Seafile tests.";
    }
}

/// <summary>
/// Shared session plus a throwaway library that is deleted when the test class finishes.
/// </summary>
public sealed class LiveFixture : IAsyncLifetime
{
    public SeafSession Session { get; private set; } = null!;
    public SeafLibrary Library { get; private set; } = null!;
    public string LibraryId => Library.Id;

    public async Task InitializeAsync()
    {
        if (!LiveServer.IsConfigured) return;
        Session = await SeafSession.LoginAsync(new Uri(LiveServer.Url!), LiveServer.User!, LiveServer.Password!);
        Library = await Session.Libraries.CreateAsync($"SeafClientTests-{Guid.NewGuid():N}"[..28]);
    }

    public async Task DisposeAsync()
    {
        if (Session == null) return;
        if (Library != null) await Session.Libraries.DeleteAsync(Library.Id);
        Session.Dispose();
    }
}

[CollectionDefinition(Name)]
public sealed class LiveCollection : ICollectionFixture<LiveFixture>
{
    public const string Name = "Live Seafile";
}
