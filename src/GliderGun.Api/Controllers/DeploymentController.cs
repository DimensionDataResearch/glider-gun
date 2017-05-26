using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DD.Research.GliderGun.Api.Controllers
{
    using Models;

    /// <summary>
    ///     The API controller for deployments.
    /// </summary>
    [Route("deployments")]
    public class DeploymentsController
        : ControllerBase
    {
        /// <summary>
        ///     The deployment service.
        /// </summary>
        readonly Deployer _deployer;

        /// <summary>
        ///     Create a new <see cref="DeploymentsController"/>.
        /// </summary>
        /// <param name="deployer">
        ///     The deployment service.
        /// </param>
        public DeploymentsController(Deployer deployer)
        {
            if (deployer == null)
                throw new ArgumentNullException(nameof(deployer));

            _deployer = deployer;
        }
 
        /// <summary>
        ///     List all deployments.
        /// </summary>
        /// <returns>
        ///     A list of deployments.
        /// </returns>
        [HttpGet("")]
        [ProducesResponseType(typeof(Deployment[]), 200)]
        public async Task<IActionResult> ListDeployments()
        {
            Deployment[] deployments = await _deployer.GetDeploymentsAsync();

            return Ok(deployments);
        }

        /// <summary>
        ///     Get a specific deployment.
        /// </summary>
        /// <param name="deploymentId">
        ///     The deployment Id.
        /// </param>
        /// <returns>
        ///     The deployment.
        /// </returns>
        [HttpGet("{deploymentId}")]
        [ProducesResponseType(typeof(Deployment), 200)]        
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> GetDeployment([FromQuery] string deploymentId)
        {
            Deployment deployment = await _deployer.GetDeploymentAsync(deploymentId);
            if (deployment == null)
            {
                Response.Headers.Add("ErrorCode", "DeploymentNotFound");

                return NotFound(new ErrorResponse
                {
                    ErrorCode = "DeploymentNotFound",
                    Message = $"No deployment was found with Id '{deploymentId}'."
                });
            }

            return Ok(deployment);
        }

        /// <summary>
        ///     Deploy a template.
        /// </summary>
        /// <returns>
        ///     The deployment result.
        /// </returns>
        [HttpPost("")]
        [ProducesResponseType(typeof(Deployment), 200)]     
        public async Task<IActionResult> DeployTemplate([FromBody] DeploymentConfiguration model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);            

            string deploymentId = HttpContext.TraceIdentifier;
            bool started = await _deployer.DeployAsync(deploymentId, model.ImageName, model.Parameters);

            return Ok(new Deployment
            {
                Action = "Deployment Requested",
                State = DeploymentState.Initiated,
                Id = deploymentId
            });
        }

        /// <summary>
        ///     Destroy a deployment.
        /// </summary>
        /// <returns>
        ///     The deployment destruction result.
        /// </returns>
        [HttpPost("{deploymentId}/destroy")]
        public async Task<IActionResult> DestroyDeployment(string deploymentId)
        {
            bool started = await _deployer.DestroyAsync(deploymentId);

            return Ok(new Deployment
            {
                Action = "Deployment Deletion Requested",
                State = DeploymentState.Deleted,
                Id = deploymentId
            });
        }
    }
}
