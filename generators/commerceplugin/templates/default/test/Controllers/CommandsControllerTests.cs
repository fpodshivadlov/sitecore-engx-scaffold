namespace <%= solutionX %>.Plugin.<%= pluginNameX %>.Tests.Controllers
{
    using System;
    using System.Web.Http.OData;

    using Microsoft.AspNetCore.Mvc;

    using NSubstitute;

    using Sitecore.Framework.Pipelines;

    using <%= solutionX %>.Commerce.Testing.Helpers;
    using <%= solutionX %>.Plugin.<%= pluginNameX %>.Commands;
    using <%= solutionX %>.Plugin.<%= pluginNameX %>.Controllers;
    using <%= solutionX %>.Plugin.<%= pluginNameX %>.Entities;
    using <%= solutionX %>.Plugin.<%= pluginNameX %>.Pipelines;
    using <%= solutionX %>.Plugin.<%= pluginNameX %>.Pipelines.Arguments;

    using Xunit;

    public class CommandsControllerTests
    {
        [Fact]
        public async void SampleCommand_IdParameter_PipelineIsRunWithParameter()
        {
            var pipeline = Substitute.For<ISamplePipeline>();
            pipeline
                .Run(Arg.Any<SampleArgument>(), Arg.Any<IPipelineExecutionContextOptions>())
                .Returns(new SampleEntity());

            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.ConfigureNullLogger();
            serviceProvider
                .GetService(typeof(SampleCommand))
                .Returns(_ => new SampleCommand(pipeline, serviceProvider));

            using (var context = ContextHelpers.CreateCommercePipelineExecutionContext())
            {
                var controller = new CommandsController(serviceProvider, context.CommerceContext.GlobalEnvironment);

                var parameter = new ODataActionParameters
                {
                    { "Id", "TestId" }
                };

                var actionResult = await controller.SampleCommand(parameter);

                Assert.NotNull(actionResult);
                var objectResult = Assert.IsType<ObjectResult>(actionResult);
                Assert.IsType<SampleCommand>(objectResult.Value);
            }

            await pipeline.Received(1).Run(Arg.Is<SampleArgument>(x => x.Parameter.ToString() == "TestId"), Arg.Any<IPipelineExecutionContextOptions>());
        }
    }
}

