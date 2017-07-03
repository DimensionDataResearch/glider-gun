using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

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
        [Required]
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public List<DeploymentParameter> Parameters { get; } = new List<DeploymentParameter>();

        public IDictionary<string, string> GetTemplateParameters()
        {
            return Parameters.Where(parameter => !parameter.IsSensitive).ToDictionary(
                parameter => parameter.Name,
                parameter => parameter.Value
            );
        }

        public IDictionary<string, string> GetSensitiveTemplateParameters()
        {
            return Parameters.Where(parameter => parameter.IsSensitive).ToDictionary(
                parameter => parameter.Name,
                parameter => parameter.Value
            );
        }

        /// <summary>
        ///     Additional File mounts which could contain the template contents.
        ///     Mount point path, content
        /// </summary>
        [Required]
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public Dictionary<string, string> Files { get; } = new Dictionary<string, string>();
    }
}