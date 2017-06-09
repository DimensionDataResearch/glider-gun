using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DD.Research.GliderGun.Api.Models
{
    /// <summary>
    ///     The configuration for a new deployment.
    /// </summary>
    public class DeploymentConfiguration
    {
        /// <summary>
        ///     The Id of the docker image to run
        /// </summary>
        [Required]
        public string ImageName { get; set; }

        /// <summary>
        ///     Values for the template's parameters (if any).
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        [Required]
        public Dictionary<string, string> Parameters { get; } = new Dictionary<string, string>();
    }
}