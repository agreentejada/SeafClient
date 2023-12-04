﻿using SeafClient.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

namespace SeafClient.Requests.Directories
{
    public class CreateDirectoryRequest : SessionRequest<bool>
    {
        public string LibraryId { get; set; }

        public string Path { get; set; }

        public bool CreateParents { get; set; }

        public override string CommandUri
        {
            get { return string.Format($"api2/repos/{LibraryId}/dir/?p={WebUtility.UrlEncode(Path)}"); }
        }

        public override HttpAccessMethod HttpAccessMethod
        {
            get { return HttpAccessMethod.Post; }
        }

        public CreateDirectoryRequest(string authToken, string libraryId, string path, bool createParents = false)
            : base(authToken)
        {
            LibraryId = libraryId;
            Path = path;
            CreateParents = createParents;

            if (!Path.StartsWith("/"))
                Path = "/" + Path;
        }

        public override IEnumerable<KeyValuePair<string, string>> GetBodyParameters()
        {
            foreach (var p in base.GetBodyParameters())
                yield return p;

            yield return new KeyValuePair<string, string>("operation", "mkdir");

            if (CreateParents)
                yield return new KeyValuePair<string, string>("create_parents", "true");
        }

        public override bool WasSuccessful(System.Net.Http.HttpResponseMessage msg)
        {
            return msg.StatusCode == HttpStatusCode.Created;
        }

        public override async System.Threading.Tasks.Task<bool> ParseResponseAsync(System.Net.Http.HttpResponseMessage msg)
        {
            string content = await msg.Content.ReadAsStringAsync();
            return content == "\"success\"";
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
