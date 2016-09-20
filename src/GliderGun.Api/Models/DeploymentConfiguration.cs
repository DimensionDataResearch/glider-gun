using Newtonsoft.Json;
using System.Collections.Generic;

namespace DD.Research.GliderGun.Api.Models
{
    /// <summary>
    ///     The configuration for a new deployment.
    /// </summary>
    public class DeploymentConfiguration
    {
        /// <summary>
        ///     The Id of the template to deploy.
        /// </summary>
        public int TemplateId { get; set; }

        /// <summary>
        ///     Values for the template's parameters (if any).
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public Dictionary<string, string> Parameters { get; } = new Dictionary<string, string>();
    }
}