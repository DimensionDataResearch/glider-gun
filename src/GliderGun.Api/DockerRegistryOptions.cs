using System;

namespace DD.Research.GliderGun.Api
{
    public class DockerRegistryOptions
    {
        public string   DockerImageRegistryAddress { get; set; }
        public string   DockerImageRegistryUser { get; set; }
        public string   DockerImageRegistryPassword { get; set; }
    }
}