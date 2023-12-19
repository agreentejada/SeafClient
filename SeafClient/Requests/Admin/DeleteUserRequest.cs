using SeafClient.Types;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SeafClient.Requests.Admin
{
    public class DeleteUserRequest: SessionRequest<bool>
    {
        public string Email { get; set; }

        public override string CommandUri => $"api/v2.1/admin/users/{Email}";

        public override HttpAccessMethod HttpAccessMethod => HttpAccessMethod.Delete;

        public DeleteUserRequest(string authToken, string email): base(authToken)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentNullException(nameof(email), "An email is required");

            Email = email;
        }

        public override async Task<bool> ParseResponseAsync(System.Net.Http.HttpResponseMessage msg)
        {
            string content = await msg.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<SeafSuccess>(content).Success;
        }
    }
}
