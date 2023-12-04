using SeafClient.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

namespace SeafClient.Requests.Directories
{
    public class GetDirectoryDetailRequest : SessionRequest<SeafDirDetail>
    {
        public string LibraryId { get; set; }

        public string Path { get; set; }

        public override string CommandUri
        {
            get => $"api/v2.1/repos/{LibraryId}/dir/detail/?path={WebUtility.UrlEncode(Path)}";
        }

        public override HttpAccessMethod HttpAccessMethod
        {
            get { return HttpAccessMethod.Get; }
        }

        public GetDirectoryDetailRequest(string authToken, string libraryId, string path)
            : base(authToken)
        {
            LibraryId = libraryId;
            Path = path;

            if (!Path.StartsWith("/"))
                Path = "/" + Path;
        }

        public override bool WasSuccessful(System.Net.Http.HttpResponseMessage msg)
        {
            return msg.StatusCode == HttpStatusCode.OK;
        }

        public override SeafError GetSeafError(HttpResponseMessage msg)
        {
            switch (msg.StatusCode)
            {
                case HttpStatusCode.BadRequest:
                    return new SeafError(msg.StatusCode, SeafErrorCode.PathDoesNotExist);
                default:
                    return base.GetSeafError(msg);
            }
        }
    }
}
