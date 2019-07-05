namespace <%= solutionX %>.Commerce.Testing.Helpers
{
    using System;
    using System.Reflection;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;

    using NSubstitute;

    public static class ServiceProviderHelpers
    {
        public static IServiceProvider ConfigureNullLogger(this IServiceProvider serviceProvider)
        {
            serviceProvider.GetService(typeof(ILogger)).Returns(NullLogger.Instance);
            serviceProvider
                .GetService(Arg.Is<Type>(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ILogger<>)))
                .Returns(_ => typeof(NullLogger<>)
                    .MakeGenericType(_.Arg<Type>().GetGenericArguments())
                    .GetField(nameof(NullLogger<object>.Instance), BindingFlags.Public | BindingFlags.Static)
                    ?.GetValue(null));

            return serviceProvider;
        }
    }
}