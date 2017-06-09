using Newtonsoft.Json;

namespace DD.Research.GliderGun.Api.Models
{
    /// <summary>
    ///     A parameter for a template deployment.
    /// </summary>
    public class DeploymentParameter
    {
        /// <summary>
        ///     The parameter name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     The parameter value.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        ///     Is the parameter value sensitive?
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsSensitive { get; set; }
    }
}