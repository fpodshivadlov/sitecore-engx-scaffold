namespace <%= solutionX %>.Plugin.<%= pluginNameX %>.Tests.Commands
{
    using System;

    using NSubstitute;

    using Sitecore.Framework.Pipelines;

    using <%= solutionX %>.Commerce.Testing.Helpers;
    using <%= solutionX %>.Plugin.<%= pluginNameX %>.Commands;
    using <%= solutionX %>.Plugin.<%= pluginNameX %>.Entities;
    using <%= solutionX %>.Plugin.<%= pluginNameX %>.Pipelines;
    using <%= solutionX %>.Plugin.<%= pluginNameX %>.Pipelines.Arguments;

    using Xunit;

    public class SampleCommandTests
    {
        [Fact]
        public async void Process_Pipeline_PipelineIsRunWithParameter()
        {
            var pipeline = Substitute.For<ISamplePipeline>();
            pipeline
                .Run(Arg.Any<SampleArgument>(), Arg.Any<IPipelineExecutionContextOptions>())
                .Returns(new SampleEntity("ResultId"));

            var serviceProvider = Substitute.For<IServiceProvider>();

            using (var context = ContextHelpers.CreateCommercePipelineExecutionContext())
            {
                var command = new SampleCommand(pipeline, serviceProvider);

                var parameter = new object();
                var entity = await command.Process(context.CommerceContext, parameter);

                Assert.NotNull(entity);
                Assert.Equal("ResultId", entity.Id);
            }

            pipeline.Received(1);
        }
    }
}
