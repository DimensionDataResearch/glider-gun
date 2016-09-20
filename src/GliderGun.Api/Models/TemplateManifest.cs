using Newtonsoft.Json;
using System.Collections.Generic;

namespace DD.Research.GliderGun.Api.Models
{
    /// <summary>
    ///     Represents a collection of templates.
    /// </summary>
    public class TemplateManifest
    {
        /// <summary>
        ///     The manifest schema version.
        /// </summary>
        public int ManifestVersion { get; set; } = 1;

        /// <summary>
        ///     The templates.
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public List<Template> Templates { get; } = new List<Template>();
    }
}