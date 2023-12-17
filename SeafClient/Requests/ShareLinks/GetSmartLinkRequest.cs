using Newtonsoft.Json;
using SeafClient.Types;
using System.Net;
using System.Threading.Tasks;

namespace SeafClient.Requests.ShareLinks
{
    public class GetSmartLinkRequest : SessionRequest<SeafSmartLink>
    {
        public string LibraryId { get; set; }
        public string Path { get; set; }
        public bool IsFolder { get; set; }

        public override string CommandUri
        {
            get {
                string path = WebUtility.UrlEncode(Path);
                string is_dir = IsFolder ? "true" : "false";
                return $"api/v2.1/smart-link/?repo_id={LibraryId}&path={path}&is_dir={is_dir}";
            }
        }

        public override HttpAccessMethod HttpAccessMethod
        {
            get { return HttpAccessMethod.Get; }
        }

        public GetSmartLinkRequest(string authToken, string libraryId, string path, bool isFolder)
        : base(authToken)
        {
            LibraryId = libraryId;
            Path = path;
            if (!Path.StartsWith("/")) { Path = "/" + Path; }

            IsFolder = isFolder;
        }
    }
}