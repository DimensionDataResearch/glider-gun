using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.IO;
using System.Linq;
using System.Threading;

namespace DD.Research.GliderGun.Api
{
    /// <summary>
    ///     Configuration for the deployment API application.
    /// </summary>
    public class Startup
    {
        /// <summary>
        ///     The application configuration.
        /// </summary>
        static IConfiguration Configuration { get; } = LoadConfiguration();

        /// <summary>
        ///     Configure application services.
        /// </summary>
        /// <param name="services">
        ///     The service collection to configure.
        /// </param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();

            services.AddOptions();
            services.Configure<DeployerOptions>(Configuration);

            services.AddMvc()
                .AddJsonOptions(json =>
				{
                    json.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
					json.SerializerSettings.Converters.Add(
						new StringEnumConverter()
					);
				});

            services.AddTransient<Deployer>();
        }

        /// <summary>
        ///     Configure the application pipeline.
        /// </summary>
        /// <param name="app">
        ///     The application pipeline builder.
        /// </param>
        /// <param name="loggerFactory">
        ///     The logger factory.
        /// </param>
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(LogLevel.Trace, includeScopes: true);

            ILogger logger = loggerFactory.CreateLogger("DockerExecutorApi");
            logger.LogInformation("LocalStateDirectory: '{LocalStateDirectory}'.",
                Configuration["LocalStateDirectory"]
            );
            logger.LogInformation("HostStateDirectory: '{HostStateDirectory}'.",
                Configuration["HostStateDirectory"]
            );

            app.UseDeveloperExceptionPage();

            // Dump out the API key (if supplied).
            app.Use(next => async context =>
            {
                if (context.Request.Headers.ContainsKey("apikey"))
                {
                    logger.LogInformation("API Key: '{ApiKey}'.",
                        context.Request.Headers["apikey"].FirstOrDefault()
                    );
                }
                
                await next(context);
            });
            app.UseMvc();
        }

        /// <summary>
        ///     The main program entry-point.
        /// </summary>
        /// <param name="commandLineArguments">
        ///     Command-line arguments.
        /// </param>
        public static void Main(string[] commandLineArguments)
        {
            SynchronizationContext.SetSynchronizationContext(
                new SynchronizationContext()
            );

            IWebHost host = new WebHostBuilder()
                .UseConfiguration(Configuration)
                .UseStartup<Startup>()
                .UseUrls("http://*:5050/")
                .UseKestrel()
                .Build();

            using (host)
            {
                host.Run();
            }
        }

        /// <summary>
        ///     Load the application configuration.
        /// </summary>
        /// <returns>
        ///     The configuration.
        /// </returns>
        static IConfiguration LoadConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables("DOOZER_")
                .Build();
        }
    }
}
