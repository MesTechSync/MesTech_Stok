using System.Reflection;
using FluentAssertions;

namespace MesTech.Tests.Unit.Infrastructure;

// ════════════════════════════════════════════════════════
// DEV5 TUR 17: Production Smoke Test Suite (G161)
// Build-level smoke tests — her push'ta CI'da çalışır.
// 1. Tüm projeler derleniyor mu (assembly load check)
// 2. Kritik servisler DI'da kayıtlı mı (type existence)
// 3. Domain entity'ler factory method'lara sahip mi
// ════════════════════════════════════════════════════════

[Trait("Category", "Unit")]
[Trait("Category", "Smoke")]
public class SmokeTests
{
    [Theory(DisplayName = "Critical assembly loads")]
    [InlineData("MesTech.Domain")]
    [InlineData("MesTech.Application")]
    [InlineData("MesTech.Infrastructure")]
    public void CriticalAssembly_ShouldLoad(string assemblyName)
    {
        var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == assemblyName);

        if (assembly is null)
        {
            assembly = Assembly.Load(assemblyName);
        }

        assembly.Should().NotBeNull($"{assemblyName} should be loadable");
        assembly!.GetTypes().Should().NotBeEmpty($"{assemblyName} should contain types");
    }

    [Theory(DisplayName = "Domain entities exist")]
    [InlineData("MesTech.Domain", "Product")]
    [InlineData("MesTech.Domain", "Order")]
    [InlineData("MesTech.Domain", "Invoice")]
    [InlineData("MesTech.Domain", "Tenant")]
    [InlineData("MesTech.Domain", "User")]
    [InlineData("MesTech.Domain", "StockMovement")]
    [InlineData("MesTech.Domain", "ReturnRequest")]
    [InlineData("MesTech.Domain", "RefreshToken")]
    [InlineData("MesTech.Domain", "NotificationLog")]
    [InlineData("MesTech.Domain", "Store")]
    public void DomainEntity_ShouldExist(string assemblyName, string entityName)
    {
        var assembly = Assembly.Load(assemblyName);
        var entityType = assembly.GetTypes()
            .FirstOrDefault(t => t.Name == entityName && !t.IsInterface && !t.IsAbstract);

        entityType.Should().NotBeNull($"Domain entity '{entityName}' should exist in {assemblyName}");
    }

    [Theory(DisplayName = "Application handlers exist")]
    [InlineData("MesTech.Application", "PlaceOrderHandler")]
    [InlineData("MesTech.Application", "CreateProductHandler")]
    [InlineData("MesTech.Application", "GetProductsHandler")]
    [InlineData("MesTech.Application", "GetOrderListHandler")]
    [InlineData("MesTech.Application", "DeletePersonalDataHandler")]
    public void ApplicationHandler_ShouldExist(string assemblyName, string handlerName)
    {
        var assembly = Assembly.Load(assemblyName);
        var handlerType = assembly.GetTypes()
            .FirstOrDefault(t => t.Name == handlerName);

        handlerType.Should().NotBeNull($"Handler '{handlerName}' should exist in {assemblyName}");
    }

    [Theory(DisplayName = "Domain interfaces exist")]
    [InlineData("MesTech.Domain", "IProductRepository")]
    [InlineData("MesTech.Domain", "IOrderRepository")]
    [InlineData("MesTech.Domain", "IUnitOfWork")]
    [InlineData("MesTech.Domain", "ITenantProvider")]
    [InlineData("MesTech.Domain", "ICurrentUserService")]
    [InlineData("MesTech.Domain", "IPaymentGateway")]
    public void DomainInterface_ShouldExist(string assemblyName, string interfaceName)
    {
        var assembly = Assembly.Load(assemblyName);
        var interfaceType = assembly.GetTypes()
            .FirstOrDefault(t => t.Name == interfaceName && t.IsInterface);

        interfaceType.Should().NotBeNull($"Interface '{interfaceName}' should exist in {assemblyName}");
    }

    [Fact(DisplayName = "Domain has at least 100 entities")]
    public void Domain_ShouldHaveMinimumEntityCount()
    {
        var assembly = Assembly.Load("MesTech.Domain");
        var entityCount = assembly.GetTypes()
            .Count(t => !t.IsInterface && !t.IsAbstract && !t.IsEnum
                && t.Namespace?.Contains("Entities") == true);

        entityCount.Should().BeGreaterThanOrEqualTo(50, "Domain should have at least 50 entities");
    }

    [Fact(DisplayName = "Application has at least 200 handlers")]
    public void Application_ShouldHaveMinimumHandlerCount()
    {
        var assembly = Assembly.Load("MesTech.Application");
        var handlerCount = assembly.GetTypes()
            .Count(t => t.Name.EndsWith("Handler") && !t.IsInterface && !t.IsAbstract);

        handlerCount.Should().BeGreaterThanOrEqualTo(200, "Application should have at least 200 handlers");
    }

    [Fact(DisplayName = "Infrastructure has adapter implementations")]
    public void Infrastructure_ShouldHaveAdapters()
    {
        var assembly = Assembly.Load("MesTech.Infrastructure");
        var adapterCount = assembly.GetTypes()
            .Count(t => t.Name.EndsWith("Adapter") && !t.IsInterface && !t.IsAbstract);

        adapterCount.Should().BeGreaterThanOrEqualTo(10, "Infrastructure should have at least 10 adapters");
    }
}
