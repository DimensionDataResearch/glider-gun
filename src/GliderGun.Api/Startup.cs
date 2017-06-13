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
using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Newtonsoft.Json;
using System.Text;
using DD.Research.GliderGun.Api.Models;
using Microsoft.AspNetCore.Http.Extensions;

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
        static IConfiguration Configuration { get; set; }


        static string[] CommandLineArguments { get; set; }

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

            var dockerRegistryConfiguration = new ConfigurationBuilder()              
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(Configuration["DockerRegistrySettingsDirectory"] + "/dockerregistrysettings.json") 
                .AddCommandLine(CommandLineArguments)          
                .AddEnvironmentVariables("GG_")
                .Build();

            services.Configure<DockerRegistryOptions>(dockerRegistryConfiguration);

            services.AddMvc()
                .AddJsonOptions(json =>
				{
                    json.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
					json.SerializerSettings.Converters.Add(
						new StringEnumConverter()
					);
				});

            services.AddSwaggerGen(swagger =>
            {
                swagger.SwaggerDoc("v1", new Info
                {
                    Title = "Glider Gun API",
                    Version = "v1"
                });
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
             logger.LogInformation("DockerRegistrySettingsDirectory: '{DockerRegistrySettingsDirectory}'.",
                Configuration["DockerRegistrySettingsDirectory"]
            );

            if(env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWelcomePage("/");
            }
            
            // Dump out the Activity Id (if supplied).
            app.Use(next => async context =>
            {
                if (context.Request.Headers.ContainsKey(Constants.ActivityIdHeaderName))
                {
                    logger.LogInformation("Activity Id: '{ActivityId}'.", context.Request.Headers[Constants.ActivityIdHeaderName].FirstOrDefault());
                }
                
                await next(context);
            });

            // Error Logging Middle Ware
           app.UseExceptionHandler(
                builder =>
                {
                    builder.Run(
                    async context =>
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        context.Response.ContentType = "application/json";
                        var ex = context.Features.Get<IExceptionHandlerFeature>();
                        if (ex != null)
                        {
                            var err = JsonConvert.SerializeObject(new ErrorResponse
                            {
                                ErrorCode = "UnKnownError",
                                StackTrace = ex.Error.StackTrace,
                                Message = ex.Error.Message
                            });
                            await context.Response.Body.WriteAsync(Encoding.ASCII.GetBytes(err),0,err.Length).ConfigureAwait(false);
                        }
                    });
                }
            );

            app.UseMvc();

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(swaggerUI =>
            {
                swaggerUI.SwaggerEndpoint("/swagger/v1/swagger.json", "Glider Gun Api");
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

            CommandLineArguments = commandLineArguments;
            Configuration = LoadApplicationConfiguration(commandLineArguments);
            
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
        static IConfiguration LoadApplicationConfiguration(string[] commandLineArguments)
        {
            return new ConfigurationBuilder()              
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")           
                .AddEnvironmentVariables("GG_")
                .AddCommandLine(commandLineArguments)
                .Build();
        }
    }
}
