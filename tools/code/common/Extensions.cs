using Microsoft.Extensions.DependencyInjection;
using System;

namespace common;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddSingletonStruct<T>(this IServiceCollection services, Func<IServiceProvider, T> factory) where T : struct
    {
        var descriptor = new ServiceDescriptor(typeof(T), provider => factory(provider), ServiceLifetime.Singleton);
        services.Add(descriptor);
        return services;
    }
}
