using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DD.Research.GliderGun.Api.Models
{
    /// <summary>
    ///     The represents a deployment.
    /// </summary>
    public class Image
    {
        public string Id { get; set; }

        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public List<string>  Tags  { get; } = new List<string>();

        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public List<string>  Digests  { get; } = new List<string>();
       
        public DateTime Created { get; set; }

        public string ClientId { get; set; }

        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public Dictionary<string, string> Labels { get; } = new Dictionary<string, string>();         
    }
}