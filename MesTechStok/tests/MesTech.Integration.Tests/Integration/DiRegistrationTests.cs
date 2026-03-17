using MesTech.Application.Interfaces;
using MesTech.Infrastructure.DependencyInjection;
using MesTech.Infrastructure.Integration.Adapters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace MesTech.Integration.Tests.Integration;

public class DiRegistrationTests
{
    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole());
        services.AddMemoryCache();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Integrations:Trendyol:Enabled"] = "false",
                ["Integrations:eBay:Enabled"] = "false"
            })
            .Build();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddIntegrationServices(configuration);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddIntegration_ResolvesFactory()
    {
        using var provider = BuildProvider();
        var factory = provider.GetService<IAdapterFactory>();
        Assert.NotNull(factory);
    }

    [Fact]
    public void AddIntegration_ResolvesOrchestrator()
    {
        using var provider = BuildProvider();
        var orchestrator = provider.GetService<IIntegratorOrchestrator>();
        Assert.NotNull(orchestrator);
    }

    [Fact]
    public void Factory_ResolvesTrendyol()
    {
        using var provider = BuildProvider();
        var factory = provider.GetRequiredService<IAdapterFactory>();
        var adapter = factory.Resolve("Trendyol");
        Assert.NotNull(adapter);
        Assert.IsType<TrendyolAdapter>(adapter);
    }

    [Fact]
    public void FullChain_OrchestratorFactoryAdapter()
    {
        using var provider = BuildProvider();
        var orchestrator = provider.GetRequiredService<IIntegratorOrchestrator>();
        Assert.NotEmpty(orchestrator.RegisteredAdapters);
        Assert.Contains(orchestrator.RegisteredAdapters, a => a.PlatformCode == "Trendyol");
        Assert.Contains(orchestrator.RegisteredAdapters, a => a.PlatformCode == "OpenCart");
    }
}
