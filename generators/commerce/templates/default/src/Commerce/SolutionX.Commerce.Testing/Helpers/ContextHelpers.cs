namespace <%= solutionX %>.Commerce.Testing.Helpers
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;

    using Sitecore.Commerce.Core;
    using Sitecore.Framework.Pipelines;

    public static class ContextHelpers
    {
        public static CommercePipelineExecutionContext CreateCommercePipelineExecutionContext()
        {
            return new CommercePipelineExecutionContext(CreateOptions(), CreateLogger());
        }

        private static IPipelineExecutionContextOptions CreateOptions()
        {
            return new CommercePipelineExecutionContextOptions(CreateCommerceContext());
        }

        private static CommerceContext CreateCommerceContext()
        {
            var context = new CommerceContext(CreateLogger(), null)
            {
                Environment = new CommerceEnvironment()
            };

            return context;
        }

        private static ILogger CreateLogger()
        {
            return NullLogger.Instance;
        }
    }
}