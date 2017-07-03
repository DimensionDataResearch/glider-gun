using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DD.Research.GliderGun.Api.Controllers
{
    using System.Net;
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
        public async Task<IActionResult> GetDeployment(string deploymentId)
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
            string deploymentId = HttpContext.TraceIdentifier;
            DeploymentState deploymentState = await _deployer.DeployAsync(deploymentId, model.ImageName,
                model.GetTemplateParameters(),
                model.GetSensitiveTemplateParameters(),
                model.Files
            );

            Deployment deployment = new Deployment
            {
                Message = "Deployment Requested",
                State = deploymentState,
                Id = deploymentId
            };
            if (deployment.State == DeploymentState.Initiated)
                return Ok(deployment);

            Response.Headers.Add("ErrorCode", "DeploymentFailed");

            return Content(deployment, HttpStatusCode.InternalServerError);
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

            deployment.Logs.Clear();
            deployment.Message = "Deployment is being destroyed";
            deployment.State = await _deployer.DestroyAsync(deploymentId);
            if (deployment.State == DeploymentState.Running)
                return Ok(deployment);

            Response.Headers.Add("ErrorCode", "DeploymentFailed");

            return Content(deployment, HttpStatusCode.InternalServerError);
        }

        /// <summary>
        ///     Purge a deployment.
        /// </summary>
        /// <returns>
        ///     The deployment purge result.
        /// </returns>
        [HttpPost("{deploymentId}/purge")]
        public async Task<IActionResult> PurgeDeployment(string deploymentId)
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

            deployment.State = await _deployer.PurgeAsync(deploymentId);
            if (deployment.State == DeploymentState.Deleted)
            {
                deployment.Message = "Deployment purged.";

                return Ok(deployment);
            }
            
            deployment.Message = "Deployment purge failed.";

            Response.Headers.Add("ErrorCode", "DeploymentFailed");

            return Content(deployment, HttpStatusCode.InternalServerError);
        }

        /// <summary>
        ///     Create an action result from the specified content.
        /// </summary>
        /// <param name="content">
        ///     The response content.
        /// </param>
        /// <param name="statusCode">
        ///     An optional response status code.
        /// </param>
        /// <returns></returns>
        IActionResult Content(object content, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return new ObjectResult(content)
            {
                StatusCode = (int)statusCode
            };
        }
    }
}
