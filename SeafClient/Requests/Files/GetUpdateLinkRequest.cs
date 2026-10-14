using System;
using System.Net;

namespace SeafClient.Requests.Files
{    

    /// <summary>
    /// Request a link which can be used to update an existing file
    /// </summary>
    public class GetUpdateLinkRequest : GetUploadLinkRequest
    {
        public string TargetDirectory { get; set; }

        public override string CommandUri
        {
            get { return String.Format("api2/repos/{0}/update-link/?p={1}", LibraryId, WebUtility.UrlEncode(TargetDirectory)); }
        }

        public GetUpdateLinkRequest(string authToken, string libraryId, string targetDirectory)
            : base(authToken, libraryId)
        {
            if (String.IsNullOrEmpty(targetDirectory))
                targetDirectory = "/";

            TargetDirectory = targetDirectory;
        }        
    }
}
