using Newtonsoft.Json;

using System.Collections.Generic;

namespace NuKeeper.BitBucketLocal.Models
{
    public class Links
    {
        [JsonProperty("self")]
        public List<Link> Self { get; set; }

        [JsonProperty("clone")]
        public List<Link> Clone { get; set; }
    }
}
