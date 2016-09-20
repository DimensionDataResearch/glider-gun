using Newtonsoft.Json;

namespace DD.Research.GliderGun.Api.Models
{
    /// <summary>
    ///     Represents a log file from a deployment.
    /// </summary>
    public class DeploymentLog
    {
        /// <summary>
        ///     The log file name.
        /// </summary>
        [JsonProperty("file")]
        public string LogFile { get; set; }

        /// <summary>
        ///     The log file content.
        /// </summary>
        [JsonProperty("content")]
        public string LogContent { get; set; }
    }
}