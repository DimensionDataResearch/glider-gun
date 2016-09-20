using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;

namespace DD.Research.GliderGun.Api.Controllers
{
    /// <summary>
    ///     The API controller for deployment templates.
    /// </summary>
    [Route("templates")]
    public class TemplatesController
        : Controller
    {
        /// <summary>
        ///     The deployment service.
        /// </summary>
        Deployer _deployer;

        /// <summary>
        ///     Create a new templates API controller.
        /// </summary>
        /// <param name="deployer">
        ///     The deployment service.
        /// </param>
        public TemplatesController(Deployer deployer)
        {
            if (deployer == null)
                throw new ArgumentNullException(nameof(deployer));

            _deployer = deployer;
        }

        /// <summary>
        ///     The template manifest file.
        /// </summary>
        FileInfo TemplateManifestFile => new FileInfo(Path.Combine(
            _deployer.LocalStateDirectory.FullName, Templates.TemplateManifestFileName
        ));

        /// <summary>
        ///     Retrieve a list of deployment templates.
        /// </summary>
        /// <returns>
        ///     A list of deployment templates.
        /// </returns>
        [HttpGet("")]
        public IActionResult ListTemplates()
        {
            return Ok(
                Templates.Load(TemplateManifestFile)
            );
        }
    }
}
