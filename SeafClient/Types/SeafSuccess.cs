using Newtonsoft.Json;

namespace SeafClient.Types
{
    public class SeafSuccess
    {
        [JsonProperty("success")]
        public virtual bool Success { get; set; }
    }

}
