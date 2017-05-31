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
using Swashbuckle.AspNetCore.Swagger;

namespace DD.Research.GliderGun.Api
{
    /// <summary>
    ///     Configuration for the deployment API application.
    /// </summary>
    public class Startup
    {

        public Startup(IHostingEnvironment env)
        {           
            // Configuration = new ConfigurationBuilder()
            //     .SetBasePath(Directory.GetCurrentDirectory())
            //     .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            //     .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
            //     .AddEnvironmentVariables("GG_")
            //     .Build();
        }

        /// <summary>
        ///     The application configuration.
        /// </summary>
        static IConfiguration Configuration { get; set; }

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
           services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new Info { Title = "Glider Gun API", Version = "v1" });
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
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
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

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Glider Gun Api");
            });
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
            Configuration = LoadConfiguration(commandLineArguments);
            
            IWebHost host = new WebHostBuilder()
                .UseUrls("http://*:5050/")
                .UseConfiguration(Configuration)
                .UseStartup<Startup>()            
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
        static IConfiguration LoadConfiguration(string[] commandLineArguments)
        {
            return new ConfigurationBuilder()              
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddCommandLine(commandLineArguments)
                .AddEnvironmentVariables("GG_")
                .Build();
        }
    }
}
