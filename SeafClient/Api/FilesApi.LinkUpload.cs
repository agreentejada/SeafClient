using SeafClient.Models;

namespace SeafClient.Api;

// Exposes the multipart upload body to other API groups (e.g. uploads through a public upload link).
public sealed partial class FilesApi
{
    internal static Func<HttpContent> BuildUploadBody(IEnumerable<(string Name, string Value)> fields, string fileName, Stream content, SeafUploadOptions options)
    {
        var list = fields.ToList();
        return Replayable(content, () => BuildMultipart(list, fileName, content, options.ContentType, options.Progress));
    }
}
