using SeafClient.Types;
using System.Collections.Generic;

namespace SeafClient.Requests.Libraries
{
    /// <summary>
    /// Request to list all libraries of the user owning the given authorization token
    /// </summary>
    public class ListLibrariesRequest : SessionRequest<IList<SeafLibrary>>
    {
        public override string CommandUri
        {
            get { return "api2/repos/"; }
        }

        public ListLibrariesRequest(string authToken)
            : base(authToken)
        {
            // --
        }
    }
}
