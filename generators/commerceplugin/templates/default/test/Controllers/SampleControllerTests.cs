namespace <%= solutionX %>.Plugin.<%= pluginNameX %>.Tests.Controllers
{
    using System;

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

    public class SampleControllerTests
    {
        private readonly ISamplePipeline pipeline;
        private readonly IServiceProvider serviceProvider;

        public SampleControllerTests()
        {
            this.pipeline = Substitute.For<ISamplePipeline>();

            this.serviceProvider = Substitute.For<IServiceProvider>()
                .ConfigureNullLogger();
        }

        [Fact]
        public async void Get_IdParameter_PipelineIsRunWithParameter()
        {
            this.pipeline
                .Run(Arg.Any<SampleArgument>(), Arg.Any<IPipelineExecutionContextOptions>())
                .Returns(new SampleEntity("ResultId"));

            this.serviceProvider
                .GetService(typeof(SampleCommand))
                .Returns(_ => new SampleCommand(this.pipeline, this.serviceProvider));

            using (var context = ContextHelpers.CreateCommercePipelineExecutionContext())
            {
                var controller = new SampleController(this.serviceProvider, context.CommerceContext.GlobalEnvironment);

                var actionResult = await controller.Get("TestId");

                Assert.NotNull(actionResult);
                var objectResult = Assert.IsType<ObjectResult>(actionResult);
                var value = Assert.IsType<SampleEntity>(objectResult.Value);
                Assert.Equal("ResultId", value.Id);
            }

            await this.pipeline
                .Received(1)
                .Run(Arg.Is<SampleArgument>(x => x.Parameter.ToString() == "TestId"), Arg.Any<IPipelineExecutionContextOptions>());
        }

        [Fact]
        public async void Get_NoRegisteredCommand_BadRequest()
        {
            using (var context = ContextHelpers.CreateCommercePipelineExecutionContext())
            {
                var controller = new SampleController(this.serviceProvider, context.CommerceContext.GlobalEnvironment);

                var actionResult = await controller.Get("Id");

                Assert.NotNull(actionResult);
                Assert.IsType<BadRequestObjectResult>(actionResult);
            }
        }

        [Fact]
        public async void Get_NotValidModelState_BadRequestWithModelState()
        {
            this.serviceProvider
                .GetService(typeof(SampleCommand))
                .Returns(_ => new SampleCommand(this.pipeline, this.serviceProvider));

            using (var context = ContextHelpers.CreateCommercePipelineExecutionContext())
            {
                var controller = new SampleController(this.serviceProvider, context.CommerceContext.GlobalEnvironment);
                controller.ModelState.AddModelError("Id", "IdError");

                var actionResult = await controller.Get(null);

                Assert.NotNull(actionResult);
                var result = Assert.IsType<BadRequestObjectResult>(actionResult);
                Assert.IsType<SerializableError>(result.Value);
            }
        }

        [Fact]
        public async void Get_NoItem_NotFound()
        {
            this.serviceProvider
                .GetService(typeof(SampleCommand))
                .Returns(_ => new SampleCommand(this.pipeline, this.serviceProvider));

            using (var context = ContextHelpers.CreateCommercePipelineExecutionContext())
            {
                var controller = new SampleController(this.serviceProvider, context.CommerceContext.GlobalEnvironment);

                var actionResult = await controller.Get("Id");

                Assert.NotNull(actionResult);
                Assert.IsType<NotFoundResult>(actionResult);
            }
        }
    }
}

