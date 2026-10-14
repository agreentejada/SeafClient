using Newtonsoft.Json;

namespace SeafClient.Types
{
    public class SeafSmartLink
    {
        [JsonProperty("smart_link")]
        public string SmartLink { get; set; }

        [JsonProperty("smart_link_token")]
        public string SmartLinkToken { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
