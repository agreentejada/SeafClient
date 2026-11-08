using System.Text;

namespace SeafClient;

/// <summary>
/// Helpers for Seafile library paths. Seafile paths are always absolute, '/'-separated and rooted at the library.
/// </summary>
public static class SeafPath
{
    public const string Root = "/";

    /// <summary>
    /// Normalizes a path: forces a leading '/', converts '\' to '/', collapses duplicate separators and
    /// removes any trailing '/' (except for the root itself). <c>null</c> or empty becomes <c>"/"</c>.
    /// </summary>
    public static string Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return Root;

        var sb = new StringBuilder(path.Length + 1);
        sb.Append('/');
        foreach (var c in path.Replace('\\', '/'))
        {
            if (c == '/' && sb[^1] == '/') continue;
            sb.Append(c);
        }

        if (sb.Length > 1 && sb[^1] == '/') sb.Length--;
        return sb.ToString();
    }

    /// <summary>Joins path segments with '/', then normalizes the result.</summary>
    public static string Combine(params string?[] segments)
        => Normalize(string.Join('/', segments.Where(s => !string.IsNullOrEmpty(s))));

    /// <summary>Returns the parent directory of <paramref name="path"/> ("/" for top-level items and the root).</summary>
    public static string GetParent(string path)
    {
        path = Normalize(path);
        var idx = path.LastIndexOf('/');
        return idx <= 0 ? Root : path[..idx];
    }

    /// <summary>Returns the last segment of <paramref name="path"/> (empty for the root).</summary>
    public static string GetName(string path)
    {
        path = Normalize(path);
        return path[(path.LastIndexOf('/') + 1)..];
    }

    /// <summary>Returns the path with a trailing '/' (Seafile expects this in a few places for directories).</summary>
    internal static string AsDirectory(string path)
    {
        path = Normalize(path);
        return path == Root ? path : path + "/";
    }
}
