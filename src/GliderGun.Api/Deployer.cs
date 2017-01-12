using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace DD.Research.GliderGun.Api
{
    using Models;
    using Utils;

    using FiltersDictionary = Dictionary<string, IDictionary<string, bool>>;
    using FilterDictionary = Dictionary<string, bool>;

    /// <summary>
    ///     The executor for deployment jobs via Docker.
    /// </summary>
    public class Deployer
    {
        /// <summary>
        ///     HTTP client used to determine external (SNAT) IP address.
        /// </summary>
        static readonly HttpClient IPConfigClient = new HttpClient();

        /// <summary>
        ///     Create a new <see cref="Deployer"/>.
        /// </summary>
        /// <param name="deployerOptions">
        ///     The deployer options.
        /// </summary>
        /// <param name="logger">
        ///     The deployer logger.
        /// </summary>
        public Deployer(IOptions<DeployerOptions> deployerOptions, ILogger<Deployer> logger)
        {
            if (deployerOptions == null)
                throw new ArgumentNullException(nameof(deployerOptions));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            DeployerOptions options = deployerOptions.Value;
            Log = logger;

            LocalStateDirectory = new DirectoryInfo(
                Path.Combine(Directory.GetCurrentDirectory(), options.LocalStateDirectory)
            );
            logger.LogInformation("Final value for LocalStateDirectory is '{LocalStateDirectory}'.",
                LocalStateDirectory.FullName
            );
            HostStateDirectory = new DirectoryInfo(
                Path.Combine(Directory.GetCurrentDirectory(), options.HostStateDirectory)
            );
            logger.LogInformation("Final value for HostStateDirectory is '{HostStateDirectory}'.",
                HostStateDirectory.FullName
            );

            Client =
                new DockerClientConfiguration(options.DockerEndPoint)
                    .CreateClient();
        }

        /// <summary>
        ///     The local directory whose state sub-directories represent the state for deployment containers.
        /// </summary>
        public DirectoryInfo LocalStateDirectory { get; }

        /// <summary>
        ///     The host directory corresponding to the local state directory.
        /// </summary>
        public DirectoryInfo HostStateDirectory { get; }

        /// <summary>
        ///     The executor logger.
        /// </summary>
        ILogger Log { get; }

        /// <summary>
        ///     The Docker API client.
        /// </summary>
        DockerClient Client { get; }

        /// <summary>
        ///     Get all deployments.
        /// </summary>
        /// <returns>
        ///     A list of deployments.
        /// </returns>
        public async Task<Deployment[]> GetDeploymentsAsync()
        {
            List<Deployment> deployments = new List<Deployment>();

            Log.LogInformation("Retrieving all deployments...");

            // Find all containers that have a "deployment.id" label.
            ContainersListParameters listParameters = new ContainersListParameters
            {
                All = true,
                Filters = new FiltersDictionary
                {
                    ["label"] = new FilterDictionary
                    {
                        ["deployment.id"] = true
                    }
                }
            };
            IList<ContainerListResponse> containerListings = await Client.Containers.ListContainersAsync(listParameters);

            deployments.AddRange(containerListings.Select(
                containerListing => ToDeploymentModel(containerListing)
            ));
            
            Log.LogInformation("Retrieved {DeploymentCount} deployments.", deployments.Count);

            return deployments.ToArray();
        }

        /// <summary>
        ///     Get a specific deployment by Id.
        /// </summary>
        /// <param name="deploymentId">
        ///     The deployment Id.
        /// </param>
        /// <returns>
        ///     The deployment, or <c>null</c> if one was not found with the specified Id.
        /// </returns>
        public async Task<Deployment> GetDeploymentAsync(string deploymentId)
        {
            if (String.IsNullOrWhiteSpace(deploymentId))
                throw new ArgumentException("Invalid deployment Id.", nameof(deploymentId));

            Log.LogInformation("Retrieving deployment '{DeploymentId}'...", deploymentId);

            // Find all containers that have a "deployment.id" label.
            ContainersListParameters listParameters = new ContainersListParameters
            {
                All = true,
                Filters = new FiltersDictionary
                {
                    ["label"] = new FilterDictionary
                    {
                        ["deployment.id=" + deploymentId] = true
                    }
                }
            };
            IList<ContainerListResponse> containerListings = await Client.Containers.ListContainersAsync(listParameters);

            ContainerListResponse newestMatchingContainer =
                containerListings
                    .OrderByDescending(container => container.Created)
                    .FirstOrDefault();
            if (newestMatchingContainer == null)
            {
                Log.LogInformation("Deployment '{DeploymentId}' not found.", deploymentId);

                return null;
            }
            
            Deployment deployment = ToDeploymentModel(newestMatchingContainer);

            Log.LogInformation("Retrieved deployment '{DeploymentId}'.", deploymentId);

            return deployment;
        }

        /// <summary>
        ///     Execute a deployment.
        /// </summary>
        /// <param name="deploymentId">
        ///     A unique identifier for the deployment.
        /// </param>
        /// <param name="templateImageTag">
        ///     The tag of the Docker image that implements the deployment template.
        /// </param>
        /// <param name="templateParameters">
        ///     A dictionary containing global template parameters to be written to the state directory.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the deployment was started; otherwise, <c>false</c>.
        /// 
        ///     TODO: Consider returning an enum instead.
        /// </returns>
        public async Task<bool> DeployAsync(string deploymentId, string templateImageTag, IDictionary<string, string> templateParameters)
        {
            if (String.IsNullOrWhiteSpace(templateImageTag))
                throw new ArgumentException("Must supply a valid template image name.", nameof(templateImageTag));

            if (templateParameters == null)
                throw new ArgumentNullException(nameof(templateParameters));

            try
            {
                Log.LogInformation("Starting deployment '{DeploymentId}' using image '{ImageTag}'...", deploymentId, templateImageTag);

                Log.LogInformation("Determining deployer's external IP address...");
                IPAddress deployerIPAddress = await GetDeployerIPAddressAsync();
                Log.LogInformation("Deployer's external IP address is '{DeployerIPAddress}'.", deployerIPAddress);
                templateParameters["deployment_ip"] = deployerIPAddress.ToString();

                DirectoryInfo deploymentLocalStateDirectory = GetLocalStateDirectory(deploymentId);
                DirectoryInfo deploymentHostStateDirectory = GetHostStateDirectory(deploymentId);

                Log.LogInformation("Local state directory for deployment '{DeploymentId}' is '{LocalStateDirectory}'.", deploymentId, deploymentLocalStateDirectory.FullName);
                Log.LogInformation("Host state directory for deployment '{DeploymentId}' is '{LocalStateDirectory}'.", deploymentId, deploymentHostStateDirectory.FullName);
                
                WriteTemplateParameters(templateParameters, deploymentLocalStateDirectory);

                CreateContainerParameters createParameters = new CreateContainerParameters
                {
                    Name = "deploy-" + deploymentId,
                    Image = templateImageTag,
                    AttachStdout = true,
                    AttachStderr = true,
                    HostConfig = new HostConfig
                    {
                        Binds = new List<string>
                        {
                            $"{deploymentHostStateDirectory.FullName}:/root/state"
                        },
                        LogConfig = new LogConfig
                        {
                            Type = "json-file",
                            Config = new Dictionary<string, string>()
                        }
                    },
                    Env = new List<string>
                    {
                        "ANSIBLE_NOCOLOR=1" // Disable coloured output because escape sequences look weird in the log.
                    },
                    Labels = new Dictionary<string, string>
                    {
                        ["task.type"] = "deployment",
                        ["deployment.id"] = deploymentId,
                        ["deployment.action"] = "Deploy",
                        ["deployment.image.deploy.tag"] = templateImageTag,
                        ["deployment.image.destroy.tag"] = GetDestroyerImageTag(templateImageTag)
                    }
                };

                CreateContainerResponse newContainer = await Client.Containers.CreateContainerAsync(createParameters);

                string containerId = newContainer.ID;
                Log.LogInformation("Created container '{ContainerId}'.", containerId);

                await Client.Containers.StartContainerAsync(containerId, new HostConfig());
                Log.LogInformation("Started container: '{ContainerId}'.", containerId);

                return true;
            }
            catch (Exception unexpectedError)
            {
                Log.LogError("Unexpected error while executing deployment '{DeploymentId}': {Error}", deploymentId, unexpectedError);

                return false;
            }
        }

        /// <summary>
        ///     Destroy a deployment.
        /// </summary>
        /// <param name="deploymentId">
        ///     The deployment Id.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the deployment was started; otherwise, <c>false</c>.
        /// 
        ///     TODO: Consider returning an enum instead.
        /// </returns>
        public async Task<bool> DestroyAsync(string deploymentId)
        {
            if (String.IsNullOrWhiteSpace(deploymentId))
                throw new ArgumentException("Must supply a valid deployment Id.", nameof(deploymentId));

            Log.LogInformation("Destroy deployment '{DeploymentId}'.", deploymentId);

            try
            {
                IList<ContainerListResponse> matchingContainers = await Client.Containers.ListContainersAsync(new ContainersListParameters
                {
                    All = true,
                    Filters = new FiltersDictionary
                    {
                        ["label"] = new FilterDictionary
                        {
                            ["deployment.id=" + deploymentId] = true
                        }
                    }
                });
                if (matchingContainers.Count == 0)
                {
                    Log.LogError("Deployment '{DeploymentId}' not found.");

                    return false;
                }

                string destroyerImageTag = matchingContainers[0].Labels["deployment.image.destroy.tag"];

                Log.LogInformation("Starting destruction of deployment '{DeploymentId}' using image '{DestroyerImageTag}'...", deploymentId, destroyerImageTag);

                DirectoryInfo deploymentLocalStateDirectory = GetLocalStateDirectory(deploymentId);
                DirectoryInfo deploymentHostStateDirectory = GetHostStateDirectory(deploymentId);

                Log.LogInformation("Local state directory for deployment '{DeploymentId}' is '{LocalStateDirectory}'.", deploymentId, deploymentLocalStateDirectory.FullName);
                Log.LogInformation("Host state directory for deployment '{DeploymentId}' is '{LocalStateDirectory}'.", deploymentId, deploymentHostStateDirectory.FullName);

                CreateContainerParameters createParameters = new CreateContainerParameters
                {
                    Name = "destroy-" + deploymentId,
                    Image = destroyerImageTag,
                    AttachStdout = true,
                    AttachStderr = true,
                    Tty = false,
                    HostConfig = new HostConfig
                    {
                        Binds = new List<string>
                        {
                            $"{deploymentHostStateDirectory.FullName}:/root/state"
                        },
                        LogConfig = new LogConfig
                        {
                            Type = "json-file",
                            Config = new Dictionary<string, string>()
                        }
                    },
                    Env = new List<string>
                    {
                        "ANSIBLE_NOCOLOR=1" // Disable coloured output because escape sequences look weird in the log.
                    },
                    Labels = new Dictionary<string, string>
                    {
                        ["task.type"] = "deployment",
                        ["deployment.id"] = deploymentId,
                        ["deployment.action"] = "Destroy",
                        ["deployment.image.destroy.tag"] = destroyerImageTag
                    }
                };

                CreateContainerResponse newContainer = await Client.Containers.CreateContainerAsync(createParameters);

                string containerId = newContainer.ID;
                Log.LogInformation("Created container '{ContainerId}'.", containerId);

                await Client.Containers.StartContainerAsync(containerId, new HostConfig());
                Log.LogInformation("Started container: '{ContainerId}'.", containerId);

                return true;
            }
            catch (Exception unexpectedError)
            {
                Log.LogError("Unexpected error while destroying deployment '{DeploymentId}': {Error}", deploymentId, unexpectedError);

                return false;
            }
        }

        /// <summary>
        ///     Get the local directory to hold state for the specified deployment.
        /// </summary>
        /// <param name="deploymentId">
        ///     The deployment Id.
        /// </param>
        /// <returns>
        ///     A <see cref="DirectoryInfo"/> representing the state directory.
        /// </returns>
        DirectoryInfo GetLocalStateDirectory(string deploymentId)
        {
            DirectoryInfo stateDirectory = LocalStateDirectory.Subdirectory(deploymentId);
            if (!stateDirectory.Exists)
                stateDirectory.Create();

            return stateDirectory;
        }

        /// <summary>
        ///     Get the host directory that holds state for the specified deployment.
        /// <param name="deploymentId">
        ///     The deployment Id.
        /// </param>
        /// </summary>
        /// <returns>
        ///     A <see cref="DirectoryInfo"/> representing the state directory.
        /// </returns>
        DirectoryInfo GetHostStateDirectory(string deploymentId)
        {
            DirectoryInfo stateDirectory = HostStateDirectory.Subdirectory(deploymentId);
            if (!stateDirectory.Exists)
                stateDirectory.Create();

            return stateDirectory;
        }

        /// <summary>
        ///     Write template parameters to tfvars.json in the specified state directory.
        /// </summary>
        /// <param name="variables">
        ///     A dictionary containing the template parameters to write.
        /// </param>
        /// <param name="stateDirectory">
        ///     The state directory.
        /// </param>
        void WriteTemplateParameters(IDictionary<string, string> variables, DirectoryInfo stateDirectory)
        {
            if (variables == null)
                throw new ArgumentNullException(nameof(variables));

            if (stateDirectory == null)
                throw new ArgumentNullException(nameof(stateDirectory));
            
            FileInfo variablesFile = GetTerraformVariableFile(stateDirectory);
            Log.LogInformation("Writing {TemplateParameterCount} parameters to '{TerraformVariableFile}'...",
                variables.Count,
                variablesFile.FullName
            );

            if (variablesFile.Exists)
                variablesFile.Delete();
            else if (!variablesFile.Directory.Exists)
                variablesFile.Directory.Create();

            using (StreamWriter writer = variablesFile.CreateText())
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(writer, variables);
            }

            Log.LogInformation("Wrote {TemplateParameterCount} parameters to '{TerraformVariableFile}'.",
                variables.Count,
                variablesFile.FullName
            );
        }

        /// <summary>
        ///     Read Terraform outputs from terraform.output.json (if present) in the specified state directory.
        /// </summary>
        /// <param name="variables">
        ///     A dictionary containing the variables to write.
        /// </param>
        /// <param name="stateDirectory">
        ///     The state directory.
        /// </param>
        /// <returns> 
        ///     A <see cref="JObject"/> representing the outputs, or <c>null</c>, if terraform.output.json does not exist in the state directory. 
        /// </returns>
        JObject ReadOutputs(DirectoryInfo stateDirectory)
        {
            if (stateDirectory == null)
                throw new ArgumentNullException(nameof(stateDirectory));
            
            FileInfo outputsFile = GetTerraformOutputFilePath(stateDirectory);
            Log.LogInformation("Reading Terraform outputs from '{TerraformOutputsFile}'...",
                outputsFile.FullName
            );
            
            if (!outputsFile.Exists)
            {
                Log.LogInformation("Terraform outputs file '{TerraformOutputsFile}' does not exist.", outputsFile.FullName);

                return new JObject();
            }

            JObject outputs;
            using (StreamReader reader = outputsFile.OpenText())
            using (JsonReader jsonReader = new JsonTextReader(reader))
            {
                JsonSerializer serializer = new JsonSerializer();
                
                outputs = serializer.Deserialize<JObject>(jsonReader);
            }

            Log.LogInformation("Read {TemplateParameterCount} Terraform outputs from '{TerraformVariableFile}'.",
                outputs.Count,
                outputsFile.FullName
            );

            return outputs;
        }

        /// <summary>
        ///     Get the Terraform variable file.
        /// </summary>
        /// <param name="stateDirectory">
        ///     The deployment state directory.
        /// </param>
        /// <returns>
        ///     The full path to the file.
        /// </returns>
        static FileInfo GetTerraformVariableFile(DirectoryInfo stateDirectory)
        {
            if (stateDirectory == null)
                throw new ArgumentNullException(nameof(stateDirectory));

            return stateDirectory.File("tfvars.json");
        }

        /// <summary>
        ///     Get the Terraform outputs file.
        /// </summary>
        /// <param name="stateDirectory">
        ///     The deployment state directory.
        /// </param>
        /// <returns>
        ///     The outputs file.
        /// </returns>
        static FileInfo GetTerraformOutputFilePath(DirectoryInfo stateDirectory)
        {
            if (stateDirectory == null)
                throw new ArgumentNullException(nameof(stateDirectory));

            return stateDirectory.File("terraform.output.json");
        }

        /// <summary>
        ///     Retrieve all deployment logs from the state directory.
        /// </summary>
        /// <param name="stateDirectory">
        ///     The state directory for a deployment.
        /// </param>
        /// <returns>
        ///     A sequence of <see cref="DeploymentLog"/>s.
        /// </returns>
        IEnumerable<DeploymentLog> ReadDeploymentLogs(DirectoryInfo stateDirectory)
        {
            if (stateDirectory == null)
                throw new ArgumentNullException(nameof(stateDirectory));

            DirectoryInfo logsDirectory = stateDirectory.Subdirectory("logs");
            if (!logsDirectory.Exists)
                yield break;

            // Get the log files in the order they were written.
            FileInfo[] logFilesByTimestamp =
                logsDirectory.EnumerateFiles("*.log")
                    .OrderBy(logFile => logFile.LastWriteTime)
                    .ToArray();

            foreach (FileInfo logFile in logFilesByTimestamp)
            {
                Log.LogInformation("Reading deployment log '{LogFile}'...", logFile.FullName);
                using (StreamReader logReader = logFile.OpenText())
                {
                    yield return new DeploymentLog
                    {
                        LogFile = logFile.Name,
                        LogContent = logReader.ReadToEnd()
                    };
                }
                Log.LogInformation("Read deployment log '{LogFile}'.", logFile.FullName);
            }
        }

        /// <summary>
        ///     Convert a <see cref="ContainerListResponse">container listing</see> to a <see cref="Deployment"/>.
        /// </summary>
        /// <param name="containerListing">
        ///     The <see cref="ContainerListResponse">container listing</see> to convert.
        /// </param>
        /// <returns>
        ///     The converted <see cref="Deployment"/>.
        /// </returns>
        Deployment ToDeploymentModel(ContainerListResponse containerListing)
        {
            if (containerListing == null)
                throw new ArgumentNullException(nameof(containerListing));

            string deploymentId = containerListing.Labels["deployment.id"];
            DirectoryInfo deploymentStateDirectory = GetLocalStateDirectory(deploymentId);

            Deployment deployment = new Deployment
            {
                Id = deploymentId,
                ContainerId = containerListing.ID,
                Action = containerListing.Labels["deployment.action"]
            };

            switch (containerListing.State)
            {
                case "running":
                {
                    deployment.State = DeploymentState.Running;

                    break;
                }
                case "exited":
                {
                    if (containerListing.Status != null && containerListing.Status.StartsWith("Exited (0)"))
                        deployment.State = DeploymentState.Successful;
                    else
                        deployment.State = DeploymentState.Failed;

                    deployment.Logs.AddRange(
                        ReadDeploymentLogs(deploymentStateDirectory)
                    );
                    deployment.Outputs = ReadOutputs(deploymentStateDirectory);

                    break;
                }
                default:
                {
                    Log.LogInformation("Unexpected container state '{State}'.", containerListing.State);

                    deployment.State = DeploymentState.Unknown;

                    break;
                }
            }

            return deployment;
        }

        /// <summary>
        ///     Get the destroyer image tag that corresponds to the specified deployer image tag.
        /// </summary>
        /// <param name="deployerImageTag">
        ///     The deployer image tag.
        /// </param>
        /// <returns>
        ///     The destroyer image tag.
        /// </returns>
        /// <remarks>
        ///     This is broken for now; the original prototype used a different mechanism for destroying deployments (Glider Gun uses a single entry-point and passes the action as a parameter).
        /// </remarks>
        static string GetDestroyerImageTag(string deployerImageTag)
        {
            if (String.IsNullOrWhiteSpace(deployerImageTag))
                throw new ArgumentException("Deployer image tag cannot be null or empty.", nameof(deployerImageTag));

            if (deployerImageTag.Contains(":destroy"))
                return deployerImageTag;

            string[] tagComponents = deployerImageTag.Split(
                new char[] {':'},
                count: 2
            );
            if (tagComponents.Length == 2)
            {
                return String.Format(
                    "{0}:{1}-destroy",
                    tagComponents[0],
                    tagComponents[1]
                );
            }
            
            return deployerImageTag + ":destroy";    
        }

        /// <summary>
        ///     Determine our public (S/NAT) IP address of the host running the deployer.
        /// </summary>
        /// <returns>
        ///     The IP address.
        /// </returns>
        async Task<IPAddress> GetDeployerIPAddressAsync()
        {
            string deployerIPAddress;
            using (HttpResponseMessage response = await IPConfigClient.GetAsync("http://ifconfig.co/json"))
            {
                response.EnsureSuccessStatusCode();

                JObject responseJson;
                using (Stream responseStream = await response.Content.ReadAsStreamAsync())
                using (StreamReader responseReader = new StreamReader(responseStream))
                using (JsonTextReader jsonReader = new JsonTextReader(responseReader))
                {
                    responseJson = new JsonSerializer().Deserialize<JObject>(jsonReader);
                }

                deployerIPAddress = responseJson.Value<string>("ip");
            }

            Log.LogInformation("deployerIPAddress = '{DeployerIPAddress}", deployerIPAddress);

            return IPAddress.Parse(deployerIPAddress);
        }
    }
}
