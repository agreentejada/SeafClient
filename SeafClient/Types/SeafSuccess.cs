using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SeafClient.Types
{
    public class SeafSuccess
    {
        [JsonProperty("success")]
        public virtual bool Success { get; set; }
    }

}
