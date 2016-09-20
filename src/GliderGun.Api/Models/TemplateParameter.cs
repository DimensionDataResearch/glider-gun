using Newtonsoft.Json.Linq;

namespace DD.Research.GliderGun.Api.Models
{
    /// <summary>
    ///     Represents a parameter in a deployment template.
    /// </summary>
    public class TemplateParameter
    {
        /// <summary>
        ///     The parameter name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     The parameter data-type.
        /// </summary>
        public JTokenType Type { get; set; }

        /// <summary>
        ///     The parameter description.
        /// </summary>
        public string Description { get; set; }
    }
}
