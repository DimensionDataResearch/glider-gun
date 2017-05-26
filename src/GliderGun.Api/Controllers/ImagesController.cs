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
    [Route("images")]
    public class ImagesController
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
        public ImagesController(Deployer deployer)
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
        public async Task<IActionResult> ListImages()
        {
            Image[] deployments = await _deployer.GetImagesAsync();
            return Ok(deployments);
        }        
    }
}
