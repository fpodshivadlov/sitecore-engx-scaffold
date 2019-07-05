namespace <%= solutionX %>.Plugin.<%= pluginNameX %>.Pipelines
{
    using Microsoft.Extensions.Logging;

    using Sitecore.Commerce.Core;
    using Sitecore.Framework.Pipelines;

    using <%= solutionX %>.Plugin.<%= pluginNameX %>.Entities;
    using <%= solutionX %>.Plugin.<%= pluginNameX %>.Pipelines.Arguments;

    public class SamplePipeline : CommercePipeline<SampleArgument, SampleEntity>, ISamplePipeline
    {
        public SamplePipeline(IPipelineConfiguration<ISamplePipeline> configuration, ILoggerFactory loggerFactory)
            : base(configuration, loggerFactory)
        {
        }
    }
}
