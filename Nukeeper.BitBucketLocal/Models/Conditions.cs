using Newtonsoft.Json;

using System.Collections.Generic;

namespace NuKeeper.BitBucketLocal.Models
{
    public class Conditions
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("reviewers")]
        public List<Reviewer> Reviewers { get; set; }
    }
}