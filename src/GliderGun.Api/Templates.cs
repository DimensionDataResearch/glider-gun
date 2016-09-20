using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace DD.Research.GliderGun.Api
{
    using Models;

    /// <summary>
    ///     Persistence for template data.
    /// </summary>
    public static class Templates
    {
        /// <summary>
        ///     The name of the template manifest file.
        /// </summary>
        public static readonly string TemplateManifestFileName = "templates.json";

        /// <summary>
        ///     Configuration for the manifest JSON serialiser.
        /// </summary>
        static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = 
            {
                new StringEnumConverter()
            }
        };

        /// <summary>
        ///     The name of the default template manifest file.
        /// </summary>
        public static FileInfo DefaultManifestFile => new FileInfo(Path.Combine(
            Directory.GetCurrentDirectory(),
            "Defaults",
            TemplateManifestFileName
        ));

        /// <summary>
        ///     Load template metadata from the specified template manifest file.
        /// </summary>
        /// <param name="templateManifestfile">
        ///     The template manifest file.
        /// </param>
        /// <returns>
        ///     The template metadata.
        /// </returns>
        public static Template[] Load(FileInfo templateManifestfile)
        {
            if (templateManifestfile == null)
                throw new ArgumentNullException(nameof(templateManifestfile));

            // Create an empty manifest, if one does not exist.
            if (!templateManifestfile.Exists)
            {
                if (!templateManifestfile.Directory.Exists)
                    templateManifestfile.Directory.Create();

                DefaultManifestFile.CopyTo(templateManifestfile.FullName,
                    overwrite: true
                );
            }

            using (StreamReader reader = templateManifestfile.OpenText())
            using (JsonTextReader jsonReader = new JsonTextReader(reader))
            {
                return JsonSerializer.Create(SerializerSettings)
                    .Deserialize<Template[]>(jsonReader);
            }
        }

        /// <summary>
        ///     Save template metadata from the specified template manifest file.
        /// </summary>
        /// <param name="templateManifestfile">
        ///     The template manifest file.
        /// </param>
        /// <param name="templates">
        ///     The template metadata.
        /// </param>
        public static void Save(FileInfo templateManifestfile, Template[] templates)
        {
            if (templateManifestfile == null)
                throw new ArgumentNullException(nameof(templateManifestfile));

            if (templateManifestfile.Exists)
                templateManifestfile.Delete();

            using (StreamWriter writer = templateManifestfile.CreateText())
            using (JsonTextWriter jsonWriter = new JsonTextWriter(writer))
            {
                JsonSerializer.Create(SerializerSettings)
                    .Serialize(jsonWriter, templates);
            }
        }
    }
}