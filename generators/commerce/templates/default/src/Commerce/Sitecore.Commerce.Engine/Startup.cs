// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Startup.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2018
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Sitecore.Commerce.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.AspNetCore.DataProtection.XmlEncryption;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.OData.Builder;
    using Microsoft.AspNetCore.OData.Extensions;
    using Microsoft.AspNetCore.OData.Routing;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json.Serialization;
    using Serilog;
    using Serilog.Events;
    using Sitecore.Commerce.Core;
    using Sitecore.Commerce.Core.Commands;
    using Sitecore.Commerce.Core.Logging;
    using Sitecore.Commerce.Plugin.SQL;
    using Sitecore.Commerce.Provider.FileSystem;
    using Sitecore.Framework.Diagnostics;
    using Sitecore.Framework.Rules;

    /// <summary>
    /// Defines the commerce engine startup.
    /// </summary>
    public class Startup
    {
        public volatile NodeContext NodeContext;

        private readonly string nodeInstanceId = Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture);
        private readonly IServiceProvider serviceProvider;
        private readonly IHostingEnvironment hostEnv;
        private readonly TelemetryClient telemetryClient;

        private volatile CommerceEnvironment environment;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="hostEnv">The host env.</param>
        /// <param name="configuration">The configuration.</param>
        public Startup(
            IServiceProvider serviceProvider,
            IHostingEnvironment hostEnv,
            IConfiguration configuration)
        {
            this.hostEnv = hostEnv;
            this.serviceProvider = serviceProvider;

            this.Configuration = configuration;
            
            var appInsightsInstrumentationKey = this.Configuration.GetSection("ApplicationInsights:InstrumentationKey").Value;
            this.telemetryClient = !string.IsNullOrWhiteSpace(appInsightsInstrumentationKey) ? new TelemetryClient { InstrumentationKey = appInsightsInstrumentationKey } : new TelemetryClient();

            if (bool.TryParse(this.Configuration.GetSection("Logging:SerilogLoggingEnabled")?.Value, out var serilogEnabled))
            {
                if (serilogEnabled)
                {
                    if (!long.TryParse(this.Configuration.GetSection("Serilog:FileSizeLimitBytes").Value, out var fileSize))
                    {
                        fileSize = 100000000;
                    }

                    Log.Logger = new LoggerConfiguration()
                         .ReadFrom.Configuration(this.Configuration)
                         .Enrich.FromLogContext().Enrich.With(
                            new ScLogEnricher()).WriteTo.Async(
                                a => a.File(
                                    $@"{Path.Combine(this.hostEnv.WebRootPath, "logs")}\SCF.{DateTimeOffset.UtcNow:yyyyMMdd}.log.{this.nodeInstanceId}.txt",
                                    this.GetSerilogLogLevel(),
                                         "{ThreadId:D5} {Timestamp:HH:mm:ss} {ScLevel} {Message}{NewLine}{Exception}",
                                    fileSizeLimitBytes: fileSize,
                                    rollOnFileSizeLimit: true),
                            bufferSize: 500)
                        .CreateLogger();
                }
            }
        }

        /// <summary>
        /// Gets or sets the Initial Startup Environment. This will tell the Node how to behave
        /// This will be overloaded by the Environment stored in configuration.
        /// </summary>
        /// <value>
        /// The startup environment.
        /// </value>
        public CommerceEnvironment StartupEnvironment
        {
            get => this.environment ?? (this.environment = new CommerceEnvironment { Name = "Bootstrap" });
            set => this.environment = value;
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Configures the services.
        /// </summary>
        /// <param name="services">The services.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            var logger = services.BuildServiceProvider().GetService<ILogger<Startup>>();
            this.NodeContext = new NodeContext(logger, this.telemetryClient)
            {
                CorrelationId = this.nodeInstanceId,
                ConnectionId = "Node_Global",
                ContactId = "Node_Global",
                GlobalEnvironment = this.StartupEnvironment,
                Environment = this.StartupEnvironment,
                WebRootPath = this.hostEnv.WebRootPath,
                LoggingPath = this.hostEnv.WebRootPath + @"\logs\"
            };

            this.SetupDataProtection(services);

            var serializer = new EntitySerializerCommand(this.serviceProvider);
            this.StartupEnvironment = this.GetGlobalEnvironment(serializer);
            this.NodeContext.Environment = this.StartupEnvironment;

            services.AddSingleton(this.StartupEnvironment);
            services.AddSingleton(this.NodeContext);

            services.Configure<LoggingSettings>(options => this.Configuration.GetSection("Logging").Bind(options));
            services.AddApplicationInsightsTelemetry(this.Configuration);
            services.Configure<ApplicationInsightsSettings>(options => this.Configuration.GetSection("ApplicationInsights").Bind(options));
            services.Configure<CertificatesSettings>(this.Configuration.GetSection("Certificates"));
            services.Configure<List<string>>(this.Configuration.GetSection("AppSettings:AllowedOrigins"));

            services.AddSingleton(this.telemetryClient);

            Log.Information("Bootstrapping Application ...");
            services.Sitecore()
                .Eventing()
                .Caching(config => config
                    .AddMemoryStore("GlobalEnvironment")
                    .ConfigureCaches("GlobalEnvironment.*", "GlobalEnvironment"))
                .Rules();
            services.Add(new ServiceDescriptor(typeof(IRuleBuilderInit), typeof(RuleBuilder), ServiceLifetime.Transient));
            services.Sitecore()
                .BootstrapProduction(this.serviceProvider)
                .ConfigureCommercePipelines();

            services.AddOData();
            services.AddCors();
            services.AddMvcCore(options => options.InputFormatters.Add(new ODataFormInputFormatter())).AddJsonFormatters();
            services.AddHttpContextAccessor();
            services.AddWebEncoders();
            services.AddDistributedMemoryCache();
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddIdentityServerAuthentication(options =>
                {
                    options.Authority = this.Configuration.GetSection("AppSettings:SitecoreIdentityServerUrl").Value;
                    options.RequireHttpsMetadata = false;
                    options.EnableCaching = false;
                    options.ApiName = "EngineAPI";
                    options.ApiSecret = "secret";
                });

            this.NodeContext.CertificateHeaderName = this.Configuration.GetSection("Certificates:CertificateHeaderName").Value;

            services.AddAuthorization(options =>
            {
                options.AddPolicy("RoleRequirement", policy => policy.Requirements.Add(new RoleAuthorizationRequirement(this.NodeContext.CertificateHeaderName)));
            });

            var antiForgeryEnabledSetting = this.Configuration.GetSection("AppSettings:AntiForgeryEnabled").Value;
            this.NodeContext.AntiForgeryEnabled = !string.IsNullOrWhiteSpace(antiForgeryEnabledSetting) && Convert.ToBoolean(antiForgeryEnabledSetting, System.Globalization.CultureInfo.InvariantCulture);
            this.NodeContext.CommerceServicesHostPostfix = this.Configuration.GetSection("AppSettings:CommerceServicesHostPostfix").Value;
            if (string.IsNullOrEmpty(this.NodeContext.CommerceServicesHostPostfix))
            {
                if (this.NodeContext.AntiForgeryEnabled)
                {
                    services.AddAntiforgery(options => options.HeaderName = "X-XSRF-TOKEN");
                }
            }
            else
            {
                if (this.NodeContext.AntiForgeryEnabled)
                {
                    services.AddAntiforgery(
                        options =>
                            {
                            options.HeaderName = "X-XSRF-TOKEN";
                            options.Cookie.SameSite = SameSiteMode.Lax;
                            options.Cookie.Domain = string.Concat(".", this.NodeContext.CommerceServicesHostPostfix);
                            options.Cookie.HttpOnly = false;
                        });
                }
            }

            services.AddMvc()
                .AddJsonOptions(options => options.SerializerSettings.ContractResolver = new DefaultContractResolver());

            this.NodeContext.AddObject(services);
        }

        /// <summary>
        /// Configures the specified application.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <param name="configureServiceApiPipeline">The context pipeline.</param>
        /// <param name="startNodePipeline">The start node pipeline.</param>
        /// <param name="configureOpsServiceApiPipeline">The context ops service API pipeline.</param>
        /// <param name="startEnvironmentPipeline">The start environment pipeline.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="loggingSettings">The logging settings.</param>
        /// <param name="applicationInsightsSettings">The application insights settings.</param>
        /// <param name="certificatesSettings">The certificates settings.</param>
        /// <param name="allowedOriginsOptions"></param>
        /// <param name="getDatabaseVersionCommand">Command to get DB version</param>
        public void Configure(
            IApplicationBuilder app,
            IConfigureServiceApiPipeline configureServiceApiPipeline,
            IStartNodePipeline startNodePipeline,
            IConfigureOpsServiceApiPipeline configureOpsServiceApiPipeline,
            IStartEnvironmentPipeline startEnvironmentPipeline,
            ILoggerFactory loggerFactory,
            IOptions<LoggingSettings> loggingSettings,
            IOptions<ApplicationInsightsSettings> applicationInsightsSettings,
            IOptions<CertificatesSettings> certificatesSettings,
            IOptions<List<string>> allowedOriginsOptions,
            GetDatabaseVersionCommand getDatabaseVersionCommand)
        {
            // TODO: Check if we can move this code to a better place, this code checks Database version against Core required version
            // Get the core required database version from config policy
            var coreRequiredDbVersion = string.Empty;
            if (this.StartupEnvironment.HasPolicy<EntityStoreSqlPolicy>())
            {
                coreRequiredDbVersion = this.StartupEnvironment.GetPolicy<EntityStoreSqlPolicy>().Version;
            }

            // Get the db version
            var dbVersion = Task.Run(() => getDatabaseVersionCommand.Process(this.NodeContext)).Result;

            // Check versions
            if (string.IsNullOrEmpty(dbVersion) || string.IsNullOrEmpty(coreRequiredDbVersion) || !string.Equals(coreRequiredDbVersion, dbVersion, StringComparison.Ordinal))
            {
                throw new CommerceException($"Core required DB Version [{coreRequiredDbVersion}] and DB Version [{dbVersion}]");
            }

            Log.Information($"Core required DB Version [{coreRequiredDbVersion}] and DB Version [{dbVersion}]");

            app.UseDiagnostics();
            app.UseStaticFiles();

            // Set the error page
            if (this.hostEnv.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseStatusCodePages();
            }

            app.UseClientCertificateValidationMiddleware(certificatesSettings);

            app.UseCors(builder =>
                builder.WithOrigins(allowedOriginsOptions.Value.ToArray())
                    .AllowCredentials()
                    .AllowAnyHeader()
                    .AllowAnyMethod());

            app.UseAuthentication();
            
            Task.Run(() => startNodePipeline.Run(this.NodeContext, this.NodeContext.PipelineContextOptions)).Wait();

            var environmentName = this.Configuration.GetSection("AppSettings:EnvironmentName").Value;
            if (!string.IsNullOrEmpty(environmentName))
            {
                this.NodeContext.AddDataMessage("EnvironmentStartup", $"StartEnvironment={environmentName}");
                Task.Run(() => startEnvironmentPipeline.Run(environmentName, this.NodeContext.PipelineContextOptions)).Wait();
            }

            // Initialize plugins OData contexts
            app.InitializeODataBuilder();

            // Run the pipeline to configure the plugins OData context
            var contextResult = Task.Run(() => configureServiceApiPipeline.Run(new ODataConventionModelBuilder(), this.NodeContext.PipelineContextOptions)).Result;
            contextResult.Namespace = "Sitecore.Commerce.Engine";

            // Get the model and register the ODataRoute
            var apiModel = contextResult.GetEdmModel();
            app.UseRouter(new ODataRoute("Api", apiModel));

            // Register the bootstrap context for the engine
            var contextOpsResult = Task.Run(() => configureOpsServiceApiPipeline.Run(new ODataConventionModelBuilder(), this.NodeContext.PipelineContextOptions)).Result;
            contextOpsResult.Namespace = "Sitecore.Commerce.Engine";

            // Get the model and register the ODataRoute
            var opsModel = contextOpsResult.GetEdmModel();
            app.UseRouter(new ODataRoute("CommerceOps", opsModel));

            var appInsightsSettings = applicationInsightsSettings.Value;
            if (!(appInsightsSettings.TelemetryEnabled &&
                  !string.IsNullOrWhiteSpace(appInsightsSettings.InstrumentationKey)))
            {
                TelemetryConfiguration.Active.DisableTelemetry = true;
            }

            if (loggingSettings.Value != null && loggingSettings.Value.ApplicationInsightsLoggingEnabled)
            {
                loggerFactory.AddApplicationInsights(appInsightsSettings);
            }

            this.NodeContext.PipelineTraceLoggingEnabled = loggingSettings.Value.PipelineTraceLoggingEnabled;
        }

        /// <summary>
        /// Gets the serilog log level.
        /// </summary>
        /// <returns>A <see cref="LogEventLevel"/></returns>
        private LogEventLevel GetSerilogLogLevel()
        {
            var level = LogEventLevel.Verbose;
            var configuredLevel = this.Configuration.GetSection("Serilog:MinimumLevel:Default").Value;
            if (string.IsNullOrEmpty(configuredLevel))
            {
                return level;
            }

            if (configuredLevel.Equals(LogEventLevel.Debug.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                level = LogEventLevel.Debug;
            }
            else if (configuredLevel.Equals(LogEventLevel.Information.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                level = LogEventLevel.Information;
            }
            else if (configuredLevel.Equals(LogEventLevel.Warning.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                level = LogEventLevel.Warning;
            }
            else if (configuredLevel.Equals(LogEventLevel.Error.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                level = LogEventLevel.Error;
            }
            else if (configuredLevel.Equals(LogEventLevel.Fatal.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                level = LogEventLevel.Fatal;
            }

            return level;
        }

        /// <summary>
        /// Setups the data protection storage and encryption protection type
        /// </summary>
        /// <param name="services">The services.</param>
        private void SetupDataProtection(IServiceCollection services)
        {
            var builder = services.AddDataProtection();
            var pathToKeyStorage = this.Configuration.GetSection("AppSettings:EncryptionKeyStorageLocation").Value;

            // Persist keys to a specific directory (should be a network location in distributed application)
            builder.PersistKeysToFileSystem(new DirectoryInfo(pathToKeyStorage));

            var protectionType = this.Configuration.GetSection("AppSettings:EncryptionProtectionType").Value.ToUpperInvariant();

            switch (protectionType)
            {
                case "DPAPI-SID":
                    var storageSid = this.Configuration.GetSection("AppSettings:EncryptionSID").Value.ToUpperInvariant();
                    //// Uses the descriptor rule "SID=S-1-5-21-..." to encrypt with domain joined user
                    builder.ProtectKeysWithDpapiNG($"SID={storageSid}", flags: DpapiNGProtectionDescriptorFlags.None);
                    break;
                case "DPAPI-CERT":
                    var storageCertificateHash = this.Configuration.GetSection("AppSettings:EncryptionCertificateHash").Value.ToUpperInvariant();
                    //// Searches the cert store for the cert with this thumbprint
                    builder.ProtectKeysWithDpapiNG(
                        $"CERTIFICATE=HashId:{storageCertificateHash}",
                        DpapiNGProtectionDescriptorFlags.None);
                    break;
                case "LOCAL":
                    //// Only the local user account can decrypt the keys
                    builder.ProtectKeysWithDpapiNG();
                    break;
                case "MACHINE":
                    //// All user accounts on the machine can decrypt the keys
                    builder.ProtectKeysWithDpapi(true);
                    break;
                default:
                    //// All user accounts on the machine can decrypt the keys
                    builder.ProtectKeysWithDpapi(true);
                    break;
            }
        }

        /// <summary>
        /// Gets the global environment.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <returns>A <see cref="CommerceEnvironment"/></returns>
        private CommerceEnvironment GetGlobalEnvironment(EntitySerializerCommand serializer)
        {
            CommerceEnvironment environment;
            var bootstrapProviderFolderPath = string.Concat(Path.Combine(this.hostEnv.WebRootPath, "Bootstrap"), Path.DirectorySeparatorChar);

            Log.Information($"Loading Global Environment using Filesystem Provider from: {bootstrapProviderFolderPath}");

            // Use the default File System provider to setup the environment
            this.NodeContext.BootstrapProviderPath = bootstrapProviderFolderPath;
            var bootstrapProvider = new FileSystemEntityProvider(this.NodeContext.BootstrapProviderPath, serializer);

            var bootstrapFile = this.Configuration.GetSection("AppSettings:BootStrapFile").Value;

            if (!string.IsNullOrEmpty(bootstrapFile))
            {
                this.NodeContext.BootstrapEnvironmentPath = bootstrapFile;

                this.NodeContext.AddDataMessage("NodeStartup", $"GlobalEnvironmentFrom='Configuration: {bootstrapFile}'");
                environment = Task.Run(() => bootstrapProvider.Find<CommerceEnvironment>(this.NodeContext, bootstrapFile, false)).Result;
            }
            else
            {
                // Load the _nodeContext default
                bootstrapFile = "Global";
                this.NodeContext.BootstrapEnvironmentPath = bootstrapFile;
                this.NodeContext.AddDataMessage("NodeStartup", $"GlobalEnvironmentFrom='{bootstrapFile}.json'");
                environment = Task.Run(() => bootstrapProvider.Find<CommerceEnvironment>(this.NodeContext, bootstrapFile, false)).Result;
            }

            this.NodeContext.BootstrapEnvironmentPath = bootstrapFile;

            this.NodeContext.GlobalEnvironmentName = environment.Name;
            this.NodeContext.AddDataMessage("NodeStartup", $"Status='Started, GlobalEnvironmentName='{this.NodeContext.GlobalEnvironmentName}'");

            if (this.Configuration.GetSection("AppSettings:BootStrapFile").Value != null)
            {
                this.NodeContext.ContactId = this.Configuration.GetSection("AppSettings:NodeId").Value;
            }

            if (!string.IsNullOrEmpty(environment.GetPolicy<DeploymentPolicy>().DeploymentId))
            {
                this.NodeContext.ContactId = $"{environment.GetPolicy<DeploymentPolicy>().DeploymentId}_{this.nodeInstanceId}";
            }

            return environment;
        }
    }
}
