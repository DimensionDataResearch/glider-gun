using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DD.Research.GliderGun.Api.Models
{
    /// <summary>
    ///     The represents a deployment.
    /// </summary>
    public class Deployment
    {
        /// <summary>
        ///     The deployment Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     The Id of the deployment's container.
        /// </summary>
        public string ContainerId { get; set; }

        /// <summary>
        ///     The current deployment state.
        /// </summary>
        public DeploymentState State { get; set; }

        /// <summary>
        ///     The action (Deploy or Destroy).
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        ///     Is the deployment complete?
        /// </summary>
        public bool IsComplete => State == DeploymentState.Successful || State == DeploymentState.Failed;

        /// <summary>
        ///     The deployment logs (once the deployment is complete).
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public List<DeploymentLog> Logs { get; } = new List<DeploymentLog>();
        
        /// <summary>
        ///     The deployment outputs (once the deployment is complete).
        /// </summary>
        public JObject Outputs { get; set; }
    }
}