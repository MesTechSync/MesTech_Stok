using FluentAssertions;
using MesTech.Application.Queries.GetStoresByTenant;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Persistence.Repositories;
using NetArchTest.Rules;

namespace MesTech.Tests.Architecture;

[Trait("Category", "Architecture")]
public class CleanArchitectureTests
{
    [Fact]
    public void DomainShouldNotDependOnOuterLayers()
    {
        var result = Types.InAssembly(typeof(Product).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "MesTech.Application",
                "MesTech.Infrastructure",
                "MesTechStok.Core",
                "MesTechStok.Desktop")
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Domain katmanı dış katmanlara bağımlı olmamalı.");
    }

    [Fact]
    public void ApplicationShouldNotDependOnInfrastructureOrLegacyUi()
    {
        var result = Types.InAssembly(typeof(GetStoresByTenantQuery).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "MesTech.Infrastructure",
                "MesTechStok.Core",
                "MesTechStok.Desktop")
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Application katmanı altyapı ve legacy UI katmanlarından bağımsız kalmalı.");
    }

    [Fact]
    public void RepositoriesShouldImplementDomainContracts()
    {
        typeof(ProductRepository).Should().BeAssignableTo<IProductRepository>();
    }
}