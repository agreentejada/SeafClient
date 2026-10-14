using SeafClient.Types;

namespace SeafClient.Requests.Groups
{
    public class ListGroupsRequest : SessionRequest<SeafGroupList>
    {
        public override string CommandUri => "api2/groups/";

        public ListGroupsRequest(string authToken)
            : base(authToken)
        {
        }
    }
}
