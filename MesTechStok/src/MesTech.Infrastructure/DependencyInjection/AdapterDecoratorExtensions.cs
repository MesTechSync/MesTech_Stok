using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Adapters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.DependencyInjection;

/// <summary>
/// Wraps all registered IIntegratorAdapter instances with InstrumentedAdapterDecorator.
/// This adds Prometheus metrics (call count, duration) to every adapter without modifying
/// the 23 individual adapter files.
/// </summary>
public static class AdapterDecoratorExtensions
{
    public static IServiceCollection DecorateAllIntegratorAdapters(this IServiceCollection services)
    {
        // Collect existing IIntegratorAdapter descriptors
        var adapterDescriptors = services
            .Where(d => d.ServiceType == typeof(IIntegratorAdapter))
            .ToList();

        if (adapterDescriptors.Count == 0)
            return services;

        // Remove originals
        foreach (var descriptor in adapterDescriptors)
            services.Remove(descriptor);

        // Re-add wrapped with InstrumentedAdapterDecorator
        foreach (var descriptor in adapterDescriptors)
        {
            var originalFactory = descriptor.ImplementationFactory;
            if (originalFactory is null) continue;

            services.AddSingleton<IIntegratorAdapter>(sp =>
            {
                var inner = (IIntegratorAdapter)originalFactory(sp);
                var logger = sp.GetRequiredService<ILogger<InstrumentedAdapterDecorator>>();
                return new InstrumentedAdapterDecorator(inner, logger);
            });
        }

        return services;
    }
}
