namespace <%= solutionX %>.Commerce.Testing.Helpers
{
    using System;

    using NSubstitute;
    using NSubstitute.Core;

    using Sitecore.Framework.Pipelines;
    using Sitecore.Framework.Pipelines.Definitions;

    public static class PipelineHelpers
    {
        public static IPipelineConfiguration<TPipeline> CreatePipelineConfigurationWithBlock<TPipeline, TInput, TOutput, TContext>(IServiceProvider serviceProvider, Func<CallInfo, TOutput> runFunc)
            where TPipeline : IPipeline<TInput, TOutput, TContext>
            where TContext : IPipelineExecutionContext
        {
            var block = Substitute.For<PipelineBlock<TInput, TOutput, TContext>>(string.Empty);
            block.Run(Arg.Any<TInput>(), Arg.Any<TContext>()).Returns(runFunc);

            return CreatePipelineConfiguration<TPipeline, TInput, TOutput, TContext>(serviceProvider, _ => block);
        }

        public static IPipelineConfiguration<TPipeline> CreatePipelineConfiguration<TPipeline, TInput, TOutput, TContext>(IServiceProvider serviceProvider, Func<IServiceProvider, IPipelineBlock<TInput, TOutput, TContext>> blockFunc)
            where TPipeline : IPipeline<TInput, TOutput, TContext>
            where TContext : IPipelineExecutionContext
        {
            var pipelineDefinition = new PipelineDefinition<TPipeline>();

            pipelineDefinition.Add(new AddPipelineBlockDefinition<IPipelineBlock<TInput, TOutput, TContext>>(blockFunc));

            var configuration = new DefaultPipelineConfiguration<TPipeline>(
                new[] { pipelineDefinition },
                new DefaultPipelineConfigurationValidator(),
                new ReflectionPipelineBlockRunnerFactory(serviceProvider));

            return configuration;
        }
    }
}