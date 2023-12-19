using Newtonsoft.Json;
using SeafClient.Types;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SeafClient.Requests.Admin
{
    public class UpdateUserRequest: SessionRequest<SeafUser>
    {
        public string Email { get; set; }
        UpdateUserRequestDTO dto;

        public override string CommandUri => $"api/v2.1/admin/users/{Email}";

        public override HttpAccessMethod HttpAccessMethod => HttpAccessMethod.Put;

        public UpdateUserRequest(string authToken,
            string email, string password, bool isStaff,
            bool isActive, string role, string name,
            string loginId, string contactEmail, string referenceId,
            string department, long quotaTotal) : base(authToken)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentNullException(nameof(email), "An email is required");

            Email = email;
            dto = new UpdateUserRequestDTO()
            {
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
    }

    public class UpdateUserRequestDTO
    {
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
