using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using VaultSharp;
using VaultSharp.Backends.Authentication.Models.Token;

namespace DD.Research.GliderGun.Api
{
    using Models;
    using Utils;

    using FiltersDictionary = Dictionary<string, IDictionary<string, bool>>;
    using FilterDictionary = Dictionary<string, bool>;
    using VaultSharp.Backends.System.Models;

    /// <summary>
    ///     The executor for deployment jobs via Docker.
    /// </summary>
    public class Deployer
    {
        /// <summary>
        ///     HTTP client used to determine external (SNAT) IP address.
        /// </summary>
        static readonly HttpClient IPConfigClient = new HttpClient();

        private readonly DeployerOptions _deployerOptions;
        
        private readonly DockerRegistryOptions _dockerRegistryOptions;
        /// <summary>
        ///     Create a new <see cref="Deployer"/>.
        /// </summary>
        /// <param name="deployerOptions">
        ///     The deployer options.
        /// </summary>
        /// <param name="logger">
        ///     The deployer logger.
        /// </summary>
        public Deployer(IOptions<DeployerOptions> deployerOptions, IOptions<DockerRegistryOptions> dockerRegistryOptions, ILogger<Deployer> logger)
        {
            if (deployerOptions == null)
                throw new ArgumentNullException(nameof(deployerOptions));

            if (dockerRegistryOptions == null)
                throw new ArgumentNullException(nameof(dockerRegistryOptions));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            _deployerOptions = deployerOptions.Value;
            _dockerRegistryOptions = dockerRegistryOptions.Value;

            Log = logger;

            LocalStateDirectory = new DirectoryInfo(
                Path.Combine(Directory.GetCurrentDirectory(), _deployerOptions.LocalStateDirectory)
            );
            logger.LogInformation("Final value for LocalStateDirectory is '{LocalStateDirectory}'.",
                LocalStateDirectory.FullName
            );
            HostStateDirectory = new DirectoryInfo(
                Path.Combine(Directory.GetCurrentDirectory(), _deployerOptions.HostStateDirectory)
            );
            logger.LogInformation("Final value for HostStateDirectory is '{HostStateDirectory}'.",
                HostStateDirectory.FullName
            );

            DockerClient = new DockerClientConfiguration(_deployerOptions.DockerEndPoint).CreateClient();

            VaultClient = VaultClientFactory.CreateVaultClient(
                _deployerOptions.VaultEndPoint,
                new TokenAuthenticationInfo(_deployerOptions.VaultToken)
            );
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
        DockerClient DockerClient { get; }

        /// <summary>
        ///     The Vault API client.
        /// </summary>
        IVaultClient VaultClient { get; }

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
            IList<ContainerListResponse> containerListings = await DockerClient.Containers.ListContainersAsync(listParameters);

            deployments.AddRange(containerListings.Select(
                containerListing => ToDeploymentModel(containerListing)
            ));
            
            Log.LogInformation("Retrieved {DeploymentCount} deployments.", deployments.Count);

            return deployments.ToArray();
        }

        /// <summary>
        ///     Get all deployments.
        /// </summary>
        /// <returns>
        ///     A list of deployments.
        /// </returns>
        public async Task<Image[]> GetImagesAsync()
        {
            List<Image> images = new List<Image>();

            Log.LogInformation("Retrieving all deployments...");

            // Find all containers that have a "deployment.id" label.
            ImagesListParameters listParameters = new ImagesListParameters
            {
                All = false,
                Filters = new Dictionary<string, IDictionary<string, bool>>(){
                    ["label"] = new FilterDictionary
                    {
                        ["dimensiondata"] = true
                    }
                }
            };
            IList<ImagesListResponse> imageListings = await DockerClient.Images.ListImagesAsync(listParameters);

            images.AddRange(imageListings.Select(
                containerListing => ToImageModel(containerListing)
            ));
            
            Log.LogInformation("Retrieved {DeploymentCount} deployments.", images.Count);

            return images.ToArray();
        }

        // TODO: Remove this method (PullImageAsync).

        /// <summary>
        ///     Pulls an image from registry
        /// </summary>
        /// <returns>
        ///     Fully qualified image name
        /// </returns>
        public async Task<string> PullImageAsync(string templateImageName)
        {
            List<Image> images = new List<Image>();
            
            var fullyQualifiedTemplateImageTag = GetFullyQualifiedImageName(templateImageName);
           
            Log.LogInformation("Pulling an image ...{fullyQualifiedTemplateImageTag}", fullyQualifiedTemplateImageTag);

            // Attach the docker registry credentials to the request
            AuthConfig auth = GetDockerRegistryAuthenticationOption();          

            ImagesPullParameters imageParrameters = new ImagesPullParameters
            {
               Parent = fullyQualifiedTemplateImageTag              
            };

            try
            {
                var stream = await DockerClient.Images.PullImageAsync(imageParrameters, auth);

                using (StreamReader reader = new StreamReader(stream))
                {                                               
                    if (!reader.EndOfStream)
                    {
                        string log = await reader.ReadToEndAsync();   
                        
                        Log.LogInformation("Image pull complete. {fullyQualifiedTemplateImageTag}, details: {log}", fullyQualifiedTemplateImageTag, log);
                    }                    
                }
            }
            catch (Exception ePullImage)
            {
                  Log.LogError("Image pull failed. {fullyQualifiedTemplateImageTag}, details {ex}", fullyQualifiedTemplateImageTag, ePullImage);
                 
                  throw;
            }
            
            return fullyQualifiedTemplateImageTag;
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
            IList<ContainerListResponse> containerListings = await DockerClient.Containers.ListContainersAsync(listParameters);

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
            deployment.Logs.Add(
                await GetContainerLog(newestMatchingContainer.ID)
            );

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
        ///     The deployment state.
        /// </returns>
        public async Task<DeploymentState> DeployAsync(string deploymentId, string templateImageTag, IDictionary<string, string> templateParameters, IDictionary<string, string> sensitiveTemplateParameters)
        {
            if (String.IsNullOrWhiteSpace(templateImageTag))
                throw new ArgumentException("Must supply a valid template image name.", nameof(templateImageTag));

            if (templateParameters == null)
                throw new ArgumentNullException(nameof(templateParameters));

            if (sensitiveTemplateParameters == null)
                throw new ArgumentNullException(nameof(sensitiveTemplateParameters));

            // Pull the image from Registry explicitly before running it.
            await PullImageAsync(templateImageTag);
            
            string fullyQualifiedTemplateImageTag = GetFullyQualifiedImageName(templateImageTag);

            try
            {
                Log.LogInformation("Starting deployment '{DeploymentId}' using image '{ImageTag}'...", deploymentId, fullyQualifiedTemplateImageTag);

                Log.LogInformation("Determining deployer's external IP address...");
                IPAddress deployerIPAddress = await GetDeployerIPAddressAsync();
                Log.LogInformation("Deployer's external IP address is '{DeployerIPAddress}'.", deployerIPAddress);
                templateParameters["deployment_ip"] = deployerIPAddress.ToString();

                DirectoryInfo deploymentLocalStateDirectory = GetLocalStateDirectory(deploymentId);
                DirectoryInfo deploymentHostStateDirectory = GetHostStateDirectory(deploymentId);

                Log.LogInformation("Local state directory for deployment '{DeploymentId}' is '{LocalStateDirectory}'.", deploymentId, deploymentLocalStateDirectory.FullName);
                Log.LogInformation("Host state directory for deployment '{DeploymentId}' is '{LocalStateDirectory}'.", deploymentId, deploymentHostStateDirectory.FullName);
                
                WriteTemplateParameters(templateParameters, deploymentLocalStateDirectory);
                await CreateVaultSecrets(deploymentId, sensitiveTemplateParameters);

                var networks = await DockerClient.Networks.ListNetworksAsync();
                var targetNetwork = networks.FirstOrDefault(
                    network => network.Name == "glidergun_default"
                );
                if (targetNetwork == null)
                    throw new InvalidOperationException("Cannot find target network.");

                Log.LogInformation("Deployment container will be attached to network '{NetworkId}'.", targetNetwork.ID);

                CreateContainerParameters createParameters = new CreateContainerParameters
                {
                    Name = "deploy-" + deploymentId,
                    Image = fullyQualifiedTemplateImageTag,
                    AttachStdout = true,
                    AttachStderr = true,
                    Env = new List<string>
                    {
                        // Disable coloured output because escape sequences look weird in the log.
                        "ANSIBLE_NOCOLOR=1",

                        $"VAULT_PATH={_deployerOptions.VaultPath}/{deploymentId}",
                        $"VAULT_ADDR={_deployerOptions.VaultEndPoint}",
                        $"VAULT_TOKEN={_deployerOptions.VaultToken}"
                    },
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
                    Labels = new Dictionary<string, string>
                    {
                        ["task.type"] = "deployment",
                        ["deployment.id"] = deploymentId,
                        ["deployment.action"] = "Deploy",
                        ["deployment.image.deploy.tag"] = fullyQualifiedTemplateImageTag,
                        ["deployment.image.destroy.tag"] = GetDestroyerImageTag(fullyQualifiedTemplateImageTag)
                    }
                };

                await AddContainerLinks(createParameters);

                CreateContainerResponse newContainer = await DockerClient.Containers.CreateContainerAsync(createParameters);

                string containerId = newContainer.ID;
                Log.LogInformation("Created container '{ContainerId}'.", containerId);

                await DockerClient.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
                Log.LogInformation("Started container: '{ContainerId}'.", containerId);

                return DeploymentState.Initiated;
            }
            catch (Exception unexpectedError)
            {
                Log.LogError("Unexpected error while executing deployment '{DeploymentId}': {Error}", deploymentId, unexpectedError);

                return DeploymentState.Failed;
            }
        }

        /// <summary>
        ///     Destroy a deployment.
        /// </summary>
        /// <param name="deploymentId">
        ///     The deployment Id.
        /// </param>
        /// <returns>
        ///     The deployment state.
        /// </returns>
        public async Task<DeploymentState> DestroyAsync(string deploymentId)
        {
            if (String.IsNullOrWhiteSpace(deploymentId))
                throw new ArgumentException("Must supply a valid deployment Id.", nameof(deploymentId));

            Log.LogInformation("Destroy deployment '{DeploymentId}'.", deploymentId);

            try
            {
                ContainerListResponse deploymentContainer = await GetLatestDeploymentContainer(deploymentId);
                if (deploymentContainer == null)
                {
                    Log.LogError("Deployment {DeploymentId} not found.", deploymentId);

                    return DeploymentState.Notfound;
                }

                string destroyerImageTag = deploymentContainer.Labels["deployment.image.destroy.tag"];

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
                    Env = new List<string>
                    {
                        // Disable coloured output because escape sequences look weird in the log.
                        "ANSIBLE_NOCOLOR=1",

                        $"VAULT_PATH={_deployerOptions.VaultPath}/{deploymentId}",
                        $"VAULT_ADDR={_deployerOptions.VaultEndPoint}",
                        $"VAULT_TOKEN={_deployerOptions.VaultToken}"
                    },
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
                    Labels = new Dictionary<string, string>
                    {
                        ["task.type"] = "deployment",
                        ["deployment.id"] = deploymentId,
                        ["deployment.action"] = "Destroy",
                        ["deployment.image.destroy.tag"] = destroyerImageTag
                    }
                };

                await AddContainerLinks(createParameters);

                CreateContainerResponse newContainer = await DockerClient.Containers.CreateContainerAsync(createParameters);

                string containerId = newContainer.ID;
                Log.LogInformation("Created container '{ContainerId}'.", containerId);

                await DockerClient.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
                Log.LogInformation("Started container: '{ContainerId}'.", containerId);

                return DeploymentState.Running;
            }
            catch (Exception unexpectedError)
            {
                Log.LogError("Unexpected error while destroying deployment '{DeploymentId}': {Error}", deploymentId, unexpectedError);

                return DeploymentState.Failed;
            }
        }

        /// <summary>
        ///     Purage a deployment.
        /// </summary>
        /// <param name="deploymentId">
        ///     The deployment Id.
        /// </param>
        /// <returns>
        ///     The deployment state.
        /// </returns>
        public async Task<DeploymentState> PurgeAsync(string deploymentId)
        {
            if (String.IsNullOrWhiteSpace(deploymentId))
                throw new ArgumentException("Must supply a valid deployment Id.", nameof(deploymentId));

            Log.LogInformation("Purging deployment '{DeploymentId}'...", deploymentId);

            try
            {
                IList<ContainerListResponse> deploymentContainers = await GetDeploymentContainers(deploymentId);
                if (deploymentContainers.Count == 0)
                {
                    Log.LogError("Deployment {DeploymentId} not found.", deploymentId);

                    return DeploymentState.Notfound;
                }

                Log.LogInformation("Destroying Vault secrets for deployment {DeploymentId}.", deploymentId);
                await DestroyVaultSecrets(deploymentId);

                foreach (var deploymentContainer in deploymentContainers)
                {
                    Log.LogInformation("Deleting container {ContainerId} for deployment {DeploymentId}.",
                        deploymentContainer.ID, deploymentId
                    );
                    await DockerClient.Containers.RemoveContainerAsync(deploymentContainer.ID, new ContainerRemoveParameters
                    {
                        Force = true
                    });
                }

                return DeploymentState.Deleted;
            }
            catch (Exception unexpectedError)
            {
                Log.LogError("Unexpected error while purging deployment '{DeploymentId}': {Error}", deploymentId, unexpectedError);

                return DeploymentState.Failed;
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
        ///     Asynchronously create Vault secrets relating to the specified deployment.
        /// </summary>
        /// <param name="deploymentId">
        ///     The deployment Id.
        /// </param>
        /// <param name="sensitiveTemplateParameters">
        ///     The template parameters to store in Vault.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        async Task CreateVaultSecrets(string deploymentId, IDictionary<string, string> sensitiveTemplateParameters)
        {
            if (String.IsNullOrWhiteSpace(deploymentId))
                throw new ArgumentException("Invalid deployment Id.", nameof(deploymentId));

            if (sensitiveTemplateParameters == null)
                throw new ArgumentNullException(nameof(sensitiveTemplateParameters));

            string vaultPath = Path.Combine(_deployerOptions.VaultPath, deploymentId);
            var secretParameters = new Dictionary<string, object>();
            foreach (var sensitiveParameter in sensitiveTemplateParameters)
                secretParameters[sensitiveParameter.Key] = sensitiveParameter.Value;

            Log.LogInformation("Writing {TemplateParameterCount} parameters to Vault ('{VaultPath}')...",
                secretParameters.Count,
                vaultPath
            );

            await VaultClient.WriteSecretAsync(vaultPath,
                values: sensitiveTemplateParameters.ToDictionary(
                    item => item.Key,
                    item => (object)item.Value
                )
            );

            Log.LogInformation("Wrote {TemplateParameterCount} parameters to Vault ('{VaultPath}').",
                secretParameters.Count,
                vaultPath
            );
        }

        /// <summary>
        ///     Asynchronously destroy Vault secrets relating to the specified deployment.
        /// </summary>
        /// <param name="deploymentId">
        ///     The deployment Id.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        async Task DestroyVaultSecrets(string deploymentId)
        {
            if (String.IsNullOrWhiteSpace(deploymentId))
                throw new ArgumentException("Invalid deployment Id.", nameof(deploymentId));

            string vaultPath = Path.Combine(_deployerOptions.VaultPath, deploymentId);
            await VaultClient.DeleteSecretAsync(vaultPath);
        }

        /// <summary>
        ///     Add links (if any) for the specified container.
        /// </summary>
        /// <param name="createParameters">
        ///     The container-creation parameters.
        /// </param>
        async Task AddContainerLinks(CreateContainerParameters createParameters)
        {
            if (createParameters == null)
                throw new ArgumentNullException(nameof(createParameters));

            if (String.IsNullOrWhiteSpace(_deployerOptions.ContainerNetworkName))
            {
                Log.LogInformation("ContainerNetworkName has not been configured; no links will be configured for container named {ContainerName}.",
                    createParameters.Name
                );

                return;
            }

            var networks = await DockerClient.Networks.ListNetworksAsync();
            var targetNetwork = networks.FirstOrDefault(
                network => network.Name == _deployerOptions.ContainerNetworkName
            );
            if (targetNetwork == null)
                throw new InvalidOperationException($"Cannot find target network '{_deployerOptions.ContainerNetworkName}'.");

            Log.LogInformation("Deployment container will be attached to network '{NetworkName}' ('{NetworkId}').",
                targetNetwork.Name,
                targetNetwork.ID
            );

            string[] containerLinks = _deployerOptions.ContainerLinks
                .Trim()
                .Split(
                    new char[] { ';' },
                    StringSplitOptions.RemoveEmptyEntries
                );
            if (containerLinks.Length == 0)
                return;

            createParameters.NetworkingConfig = new NetworkingConfig
            {
                EndpointsConfig = new Dictionary<string, EndpointSettings>
                {
                    [targetNetwork.Name] = new EndpointSettings
                    {
                        Links = new List<string>(containerLinks),
                        NetworkID = targetNetwork.ID
                    }
                }
            };
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
                    {
                        if (deployment.Action == "Deploy")
                            deployment.State = DeploymentState.Deployed;
                        else if (deployment.Action == "Destroy")
                            deployment.State = DeploymentState.Destroyed;
                    }
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
        ///     Convert the Docker API model for an image to a Glider Gun <see cref="Image"/> model.
        /// </summary>
        /// <param name="imageListing">
        ///     The Docker API model to convert.
        /// </param>
        /// <returns>
        ///     The converted <see cref="Image"/> model.
        /// </returns>
        Image ToImageModel(ImagesListResponse imageListing)
        {
            if (imageListing == null)
                throw new ArgumentNullException(nameof(imageListing));

            Image image = new Image
            {
                Id = imageListing.ID,
                Created = imageListing.Created          
            };        
            
            if(imageListing.RepoTags != null)
                image.Tags.AddRange(imageListing.RepoTags);

            return image;
        }

        /// <summary>
        ///     Get a list of containers associated with the specified deployment.
        /// </summary>
        /// <param name="deploymentId">
        ///     The deployment Id.
        /// </param>
        /// <returns>
        ///     A list of <see cref="ContainerListResponse"/>s representing the containers.
        /// </returns>
        async Task<IList<ContainerListResponse>> GetDeploymentContainers(string deploymentId)
        {
            if (String.IsNullOrWhiteSpace(deploymentId))
                throw new ArgumentException("Must supply a valid deployment Id.", nameof(deploymentId));

            var matchingContainers = await DockerClient.Containers.ListContainersAsync(new ContainersListParameters{
                Filters = new FiltersDictionary
                {
                    ["label"] = new FilterDictionary
                    {
                        [$"deployment.id={deploymentId}"] = true
                    }
                }
            });

            return matchingContainers;
        }

        /// <summary>
        ///     Get the latest container associated with the specified deployment.
        /// </summary>
        /// <param name="deploymentId">
        ///     The deployment Id.
        /// </param>
        /// <returns>
        ///     A <see cref="ContainerListResponse"/> representing the container, or <c>null</c> if no matching containers were found.
        /// </returns>
        async Task<ContainerListResponse> GetLatestDeploymentContainer(string deploymentId)
        {
            if (String.IsNullOrWhiteSpace(deploymentId))
                throw new ArgumentException("Must supply a valid deployment Id.", nameof(deploymentId));

            var matchingContainers = await GetDeploymentContainers(deploymentId);

            return matchingContainers.OrderByDescending(container => container.Created).FirstOrDefault();
        }

        /// <summary>
        ///     Retrieve the STDOUT / STDERR log for the specified container.
        /// </summary>
        /// <param name="containerId">
        ///     The Id of the target container.
        /// </param>
        /// <returns>
        ///     A <see cref="DeploymentLog"/> representing the container log.
        /// </returns>
        async Task<DeploymentLog> GetContainerLog(string containerId)
        {
            if (String.IsNullOrWhiteSpace(containerId))
                throw new ArgumentException("Must supply a valid container Id.", nameof(containerId));

            DeploymentLog containerLog = new DeploymentLog
            {
                LogFile = "ContainerLog",
                LogContent = String.Empty
            };

            var logParameters = new ContainerLogsParameters
            {
                ShowStderr = true,
                ShowStdout = true
            };
            using (Stream logStream = await DockerClient.Containers.GetContainerLogsAsync(containerId, logParameters, CancellationToken.None))
            using (StreamReader logReader = new StreamReader(logStream))
            {
                containerLog.LogContent = await logReader.ReadToEndAsync();
            }

            return containerLog;
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

        AuthConfig GetDockerRegistryAuthenticationOption()
        {
            AuthConfig auth = null;
            if(_dockerRegistryOptions != null && !String.IsNullOrWhiteSpace(_dockerRegistryOptions.DockerImageRegistryAddress))
            {
                auth = new AuthConfig
                { 
                    Username = _dockerRegistryOptions.DockerImageRegistryUser, 
                    Password = _dockerRegistryOptions.DockerImageRegistryPassword, 
                    Email = " ", 
                    ServerAddress = _dockerRegistryOptions.DockerImageRegistryAddress
                };
            }
            return auth;
        }

        string GetFullyQualifiedImageName(string templateImageName)
        {
            if(_dockerRegistryOptions != null && !String.IsNullOrWhiteSpace(_dockerRegistryOptions.DockerImageRegistryAddress))
                templateImageName = _dockerRegistryOptions.DockerImageRegistryAddress + "/" + _deployerOptions.TemplateImageNamePrefix + templateImageName;
            
            return templateImageName;
        }
    }
}
