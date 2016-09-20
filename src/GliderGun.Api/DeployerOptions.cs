using System;

namespace DD.Research.GliderGun.Api
{
    public class DeployerOptions
    {
        public string   LocalStateDirectory { get; set; }
        public string   HostStateDirectory { get; set; }
        public Uri      DockerEndPoint { get; set; }
    }
}