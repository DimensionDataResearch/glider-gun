using System;

namespace DD.Research.GliderGun.Api
{
    public class DeployerOptions
    {
        public string   LocalStateDirectory { get; set; }
        public string   HostStateDirectory { get; set; }
        public string   DockerImageRegistry { get; set; }
        public Uri      DockerEndPoint { get; set; }
        public string   TemplateImageNamePrefix { get; set; }
        public string   Links { get; set; }
        public Uri      VaultEndPoint { get; set; }
        public string   VaultPath { get; set; }
        public string   VaultToken { get; set; }
    }
}
