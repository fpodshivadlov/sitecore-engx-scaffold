namespace <%= solutionX %>.Plugin.<%= pluginNameX %>.Pipelines.Blocks
{
    using System;
    using System.Threading.Tasks;

    using Sitecore.Commerce.Core;
    using Sitecore.Framework.Conditions;
    using Sitecore.Framework.Pipelines;

    using <%= solutionX %>.Plugin.<%= pluginNameX %>.Entities;
    using <%= solutionX %>.Plugin.<%= pluginNameX %>.Pipelines.Arguments;

    [PipelineDisplayName("Sample.SampleBlock")]
    public class SampleBlock : PipelineBlock<SampleArgument, SampleEntity, CommercePipelineExecutionContext>
    {
        public override async Task<SampleEntity> Run(SampleArgument arg, CommercePipelineExecutionContext context)
        {
            Condition.Requires(arg).IsNotNull($"{this.Name}: The argument can not be null");
            var result = await Task.Run(() => new SampleEntity { Id = Guid.NewGuid().ToString() });
            return result;
        }
    }
}
