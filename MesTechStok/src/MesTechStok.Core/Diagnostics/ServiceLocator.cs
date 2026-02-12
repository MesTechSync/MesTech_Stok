using System;
using Microsoft.Extensions.DependencyInjection;

namespace MesTechStok.Core.Diagnostics
{
    /// <summary>
    /// Minimal service locator to bridge Core library logging/persistence helpers
    /// with the application's DI container. Must be initialized at app startup.
    /// </summary>
    public static class ServiceLocator
    {
        private static IServiceProvider? _serviceProvider;

        public static void SetProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public static IServiceScope CreateScope()
        {
            if (_serviceProvider == null)
            {
                throw new InvalidOperationException("ServiceLocator not initialized. Call SetProvider() at startup.");
            }
            return _serviceProvider.CreateScope();
        }
    }
}


