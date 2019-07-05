namespace <%= solutionX %>.Plugin.<%= pluginNameX %>.Commands
{
    using System;
    using System.Threading.Tasks;

    using Sitecore.Commerce.Core;
    using Sitecore.Commerce.Core.Commands;

    using <%= solutionX %>.Plugin.<%= pluginNameX %>.Entities;
    using <%= solutionX %>.Plugin.<%= pluginNameX %>.Pipelines;
    using <%= solutionX %>.Plugin.<%= pluginNameX %>.Pipelines.Arguments;

    public class SampleCommand : CommerceCommand
    {
        private readonly ISamplePipeline pipeline;

        public SampleCommand(ISamplePipeline pipeline, IServiceProvider serviceProvider) : base(serviceProvider)
        {
            this.pipeline = pipeline;
        }

        public async Task<SampleEntity> Process(CommerceContext commerceContext, object parameter)
        {
            using (var activity = CommandActivity.Start(commerceContext, this))
            {
                var arg = new SampleArgument(parameter);
                var result = await this.pipeline.Run(arg, new CommercePipelineExecutionContextOptions(commerceContext));

                return result;
            }
        }
    }
}
