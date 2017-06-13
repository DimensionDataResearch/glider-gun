using Microsoft.AspNetCore.Mvc;
using System.Net;
using DD.Research.GliderGun.Api.Filters;

namespace DD.Research.GliderGun.Api.Controllers
{
    /// <summary>
    ///     The base class for executor API controllers.
    /// </summary>
    [ModelValidationFilter]
    public abstract class ControllerBase
        : Controller
    {
        /// <summary>
        ///     Initialise <see cref="ControllerBase"/>.
        /// </summary>
        protected ControllerBase()
        {
        }

        /// <summary>
        ///     Create a response with the specified status code.
        /// </summary>
        /// <param name="statusCode">
        ///     The response status code.
        /// </param>
        /// <returns>
        ///     An action result that renders the response.
        /// </returns>
        protected virtual IActionResult StatusCode(HttpStatusCode statusCode)
        {
            return base.StatusCode(
                (int)statusCode
            );
        }
    }
}