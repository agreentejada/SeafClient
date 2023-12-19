using Newtonsoft.Json;
using SeafClient.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

namespace SeafClient.Requests.Admin
{
    public class CreateUserRequest : SessionRequest<SeafUser>
    {
        CreateUserRequestDTO dto;

        public override string CommandUri
        {
            get { return string.Format($"api/v2.1/admin/users/"); }
        }

        public override HttpAccessMethod HttpAccessMethod
        {
            get { return HttpAccessMethod.Post; }
        }

        public CreateUserRequest(string authToken, 
            string email, string password, bool isStaff, 
            bool isActive, string role, string name, 
            string loginId, string contactEmail, string referenceId, 
            string department, long quotaTotal
        )
            : base(authToken)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentNullException(nameof(email), "An email is required");

            dto = new CreateUserRequestDTO()
            {
                Email = email,
                Password = password,
                IsStaff = isStaff,
                IsActive = isActive,
                Role = role,
                Name = name,
                LoginId = loginId,
                ContactEmail = contactEmail,
                ReferenceId = referenceId,
                Department = department,
                QuotaTotal = quotaTotal,
            };
        }

        public override HttpRequestMessage GetCustomizedRequest(Uri serverUri)
        {

            Uri uri = new Uri(serverUri, CommandUri);

            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, uri);

            message.Headers.Referrer = uri;
            foreach (var hi in GetAdditionalHeaders())
                message.Headers.Add(hi.Key, hi.Value);

            message.Content = new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json");
            return message;
        }
    }

    public class CreateUserRequestDTO
    {
        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("is_staff")]
        public bool IsStaff { get; set; }

        [JsonProperty("is_active")]
        public bool IsActive { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("login_id")]
        public string LoginId { get; set; }

        [JsonProperty("contact_email")]
        public string ContactEmail { get; set; }

        [JsonProperty("reference_id")]
        public string ReferenceId { get; set; }

        [JsonProperty("department")]
        public string Department { get; set; }

        [JsonProperty("quota_total")]
        public long QuotaTotal { get; set; }
    }
}
