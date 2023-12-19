using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace SeafClient.Types
{
    public class SeafUser
    {
        [JsonProperty("login_id")]
        public string LoginId { get; set; }

        [JsonProperty("quota_usage")]
        public long QuotaUsage { get; set; }

        [JsonProperty("last_login")]
        public DateTime LastLogin { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("has_default_device")]
        public bool HasDefaultDevice { get; set; }

        [JsonProperty("create_time")]
        public DateTime CreateTime { get; set; }

        [JsonProperty("is_active")]
        public bool IsActive { get; set; }

        [JsonProperty("is_force_2fa")]
        public bool IsForce2FA { get; set; }

        [JsonProperty("is_staff")]
        public bool IsStaff { get; set; }

        [JsonProperty("contact_email")]
        public string ContactEmail { get; set; }

        [JsonProperty("reference_id")]
        public string ReferenceId { get; set; }

        [JsonProperty("department")]
        public string Department { get; set; }

        [JsonProperty("quota_total")]
        public long QuotaTotal { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }
    }
}
