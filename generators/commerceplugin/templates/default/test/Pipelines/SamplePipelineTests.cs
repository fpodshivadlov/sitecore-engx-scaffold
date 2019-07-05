namespace <%= solutionX %>.Plugin.<%= pluginNameX %>.Tests.Pipelines
{
    using System;

    using Microsoft.Extensions.Logging.Abstractions;

    using NSubstitute;

    using Sitecore.Commerce.Core;

    using <%= solutionX %>.Commerce.Testing.Helpers;
    using <%= solutionX %>.Plugin.<%= pluginNameX %>.Entities;
    using <%= solutionX %>.Plugin.<%= pluginNameX %>.Pipelines;
    using <%= solutionX %>.Plugin.<%= pluginNameX %>.Pipelines.Arguments;

    using Xunit;

    public class SamplePipelineTests
    {
        [Fact]
        public async void Run_Argument_Entity()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();

            var configuration = PipelineHelpers.CreatePipelineConfigurationWithBlock<ISamplePipeline, SampleArgument, SampleEntity, CommercePipelineExecutionContext>(
                serviceProvider,
                _ => new SampleEntity("ResultId"));

            using (var context = ContextHelpers.CreateCommercePipelineExecutionContext())
            {
                var pipeline = new SamplePipeline(configuration, NullLoggerFactory.Instance);

                var argument = new SampleArgument(new object());
                var entity = await pipeline.Run(argument, context);

                Assert.NotNull(entity);
                Assert.Equal("ResultId", entity.Id);
            }
        }
    }
}
