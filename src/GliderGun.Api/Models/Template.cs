using System.Collections.Generic;
using Newtonsoft.Json;

namespace DD.Research.GliderGun.Api.Models
{
    /// <summary>
    ///     Represents a deployment template.
    /// </summary>
    public class Template
    {
        /// <summary>
        ///     The template Id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        ///     The template name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     The template description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        ///     The name of the underlying Docker image that implements the template.
        /// </summary>
        /// <remarks>
        ///     A "dta-template/" prefix will be prepended to the image name (to avoid clashes with any other types of image).
        /// </remarks>
        public string ImageName { get; set; }

        /// <summary>
        ///     The template's parameters (if any).
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public List<TemplateParameter> Parameters { get; } = new List<TemplateParameter>();
    }
}
