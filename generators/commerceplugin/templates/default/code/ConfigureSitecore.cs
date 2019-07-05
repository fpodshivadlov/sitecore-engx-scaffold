namespace <%= solutionX %>.Plugin.<%= pluginNameX %>
{
    using System.Reflection;

    using Microsoft.Extensions.DependencyInjection;

    using Sitecore.Commerce.Core;
    using Sitecore.Framework.Configuration;
    using Sitecore.Framework.Pipelines.Definitions.Extensions;

    using <%= solutionX %>.Plugin.<%= pluginNameX %>.Pipelines;
    using <%= solutionX %>.Plugin.<%= pluginNameX %>.Pipelines.Blocks;

    public class ConfigureSitecore : IConfigureSitecore
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();
            services.RegisterAllPipelineBlocks(assembly);

            services.Sitecore().Pipelines(config => config
                .AddPipeline<ISamplePipeline, SamplePipeline>(configure =>
                    {
                        configure.Add<SampleBlock>();
                    })
                .ConfigurePipeline<IConfigureServiceApiPipeline>(configure => configure.Add<ConfigureServiceApiBlock>()));

            services.RegisterAllCommands(assembly);
        }
    }
}
