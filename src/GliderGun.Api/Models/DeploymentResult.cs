using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DD.Research.GliderGun.Api.Models
{
    /// <summary>
    ///     Represents the result of a deployment.
    /// </summary>
    public class DeploymentResult
    {
        /// <summary>
        ///     A unique identifier for the deployment.
        /// </summary>
        public string DeploymentId { get; set; }

        /// <summary>
        ///     Was the deployment successful?
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        ///     Logs (if any) collected during the deployment. 
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public List<DeploymentLog> Logs { get; } = new List<DeploymentLog>();
        
        /// <summary>
        ///     Terraform outputs (if any) from the deployment.
        /// </summary>
        public JObject Outputs { get; set; }
    }
}
