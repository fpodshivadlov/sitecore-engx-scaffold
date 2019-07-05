namespace <%= solutionX %>.Plugin.<%= pluginNameX %>.Pipelines
{
    using Sitecore.Commerce.Core;
    using Sitecore.Framework.Pipelines;

    using <%= solutionX %>.Plugin.<%= pluginNameX %>.Entities;
    using <%= solutionX %>.Plugin.<%= pluginNameX %>.Pipelines.Arguments;

    [PipelineDisplayName("SamplePipeline")]
    public interface ISamplePipeline : IPipeline<SampleArgument, SampleEntity, CommercePipelineExecutionContext>
    {
    }
}
