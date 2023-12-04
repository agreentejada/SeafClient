using Newtonsoft.Json;
using SeafClient.Requests;
using SeafClient.Types;
using System;
using System.Net;
using System.Net.Http;
using System.Text;

namespace SeafClient
{

    public class CreateShareLinkRequest : SessionRequest<SeafShareLink>
    {
        public string LibraryId { get; set; }

        public string Path { get; set; }

        public string Password { get; set; }

        public int? ExpiryDays { get; set; }

        public DateTime? ExpiryTime { get; set; }

        public bool CanEdit { get; set; }

        public bool CanDownload { get; set; }

        public bool CanUpload { get; set; }

        public override string CommandUri
        {
            get { return "api/v2.1/share-links/";}
        }

        public override HttpAccessMethod HttpAccessMethod
        {
            get { return HttpAccessMethod.Custom; }
        }

        public CreateShareLinkRequest(
            string authToken, 
            string libraryId,
            string path,
            string password = null,
            int? expirationDays = null,
            DateTime? expirationTime = null,
            bool canEdit = false,
            bool canDownload = true,
            bool canUpload = false) : base(authToken)
        {
            LibraryId = libraryId;
            Path = path;

            if (!Path.StartsWith("/"))
                Path = "/" + Path;

            Password = password;
            CanEdit = canEdit;
            CanDownload = canDownload;
            CanUpload = canUpload;

            if (expirationTime != null && expirationDays != null)
                throw new ArgumentException("Cannot set both arguments expire_days and expiration_time.");

            ExpiryTime = expirationTime;
            ExpiryDays = expirationDays;
        }

        public override SeafError GetSeafError(HttpResponseMessage msg)
        {
            switch (msg.StatusCode)
            {
                case HttpStatusCode.BadRequest:
                    return new SeafError(msg.StatusCode, SeafErrorCode.PathDoesNotExist);
                case HttpStatusCode.NotFound:
                    return new SeafError(msg.StatusCode, SeafErrorCode.FileNotFound);
                default:
                    return base.GetSeafError(msg);
            }
        }

        public override HttpRequestMessage GetCustomizedRequest(Uri serverUri)
        {

                Uri uri = new Uri(serverUri, CommandUri);

                HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, uri);

                message.Headers.Referrer = uri;
                foreach (var hi in GetAdditionalHeaders())
                    message.Headers.Add(hi.Key, hi.Value);

                var data = new SeafShareLinkRequest()
                {
                    LibraryId = LibraryId,
                    Path = Path,
                    ExpireDays = ExpiryDays,
                    ExpirationTime = ExpiryTime,
                    Password = Password,
                    Permission = new SeafShareLinkPermissions()
                    {
                        CanDownload = CanDownload,
                        CanEdit = CanEdit,
                        CanUpload = CanUpload
                    }

                };

                message.Content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                return message;
        }

    }

}