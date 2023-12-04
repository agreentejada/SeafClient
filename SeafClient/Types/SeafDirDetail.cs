using Newtonsoft.Json;
using SeafClient.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace SeafClient.Types
{
    public class SeafDirDetail
    {
        [JsonProperty("repo_id")]
        public virtual string LibraryId { get; set; }

        [JsonProperty("name")]
        public virtual string Name { get; set; }

        [JsonConverter(typeof(SeafPermissionConverter))]
        public virtual SeafPermission Permission { get; set; }

        /// <summary>
        ///     Time of the last modification of this entry
        ///     (as UNIX timestamp)
        /// </summary>
        [JsonProperty("mtime")]
        public virtual DateTime Timestamp { get; set; }

        [JsonProperty("path")]
        public virtual string Path { get; set; }
    }

}


