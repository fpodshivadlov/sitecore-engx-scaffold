namespace <%= solutionX %>.Plugin.<%= pluginNameX %>.Tests.Pipelines.Blocks
{
    using System;

    using <%= solutionX %>.Commerce.Testing.Helpers;
    using <%= solutionX %>.Plugin.<%= pluginNameX %>.Pipelines.Arguments;
    using <%= solutionX %>.Plugin.<%= pluginNameX %>.Pipelines.Blocks;

    using Xunit;

    public class SampleBlockTests
    {
        [Fact]
        public async void Run_Pipeline_PipelineIsRunWithParameter()
        {
            using (var context = ContextHelpers.CreateCommercePipelineExecutionContext())
            {
                var sampleCommand = new SampleBlock();

                var argument = new SampleArgument(new object());
                var result = await sampleCommand.Run(argument, context);

                Assert.NotNull(result);
                Assert.True(Guid.TryParse(result.Id, out _));
            }
        }
    }
}
