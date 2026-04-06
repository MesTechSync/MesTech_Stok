using FluentAssertions;
using MesTech.Application.Features.Crm.Queries.GetDeals;
using MesTech.Application.Features.Crm.Queries.GetLeads;
using MesTech.Application.Features.Product.Queries.GetPlatformProducts;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Common;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Queries;

/// <summary>
/// DEV5 Batch 8: CRM + Platform query handler testleri.
/// </summary>

#region GetDeals

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetDealsHandlerTests
{
    private readonly Mock<ICrmDealRepository> _dealRepo = new();

    [Fact]
    public async Task Handle_NoDeals_ShouldReturnEmpty()
    {
        _dealRepo.Setup(r => r.GetByTenantPagedAsync(
            It.IsAny<Guid>(), null, 1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Deal>());

        var handler = new GetDealsHandler(_dealRepo.Object);
        var result = await handler.Handle(new GetDealsQuery(Guid.NewGuid()), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithPipelineFilter_ShouldUseGetByPipeline()
    {
        var pipelineId = Guid.NewGuid();
        _dealRepo.Setup(r => r.GetByPipelineAsync(
            It.IsAny<Guid>(), pipelineId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Deal>());

        var handler = new GetDealsHandler(_dealRepo.Object);
        await handler.Handle(new GetDealsQuery(Guid.NewGuid(), PipelineId: pipelineId), CancellationToken.None);

        _dealRepo.Verify(r => r.GetByPipelineAsync(
            It.IsAny<Guid>(), pipelineId, null, It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region GetLeads

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetLeadsHandlerTests
{
    private readonly Mock<ICrmLeadRepository> _leadRepo = new();

    [Fact]
    public async Task Handle_NoLeads_ShouldReturnEmpty()
    {
        _leadRepo.Setup(r => r.GetPagedAsync(
            It.IsAny<Guid>(), null, null, 1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Lead>().AsReadOnly() as IReadOnlyList<Lead>, 0));

        var handler = new GetLeadsHandler(_leadRepo.Object);
        var result = await handler.Handle(new GetLeadsQuery(Guid.NewGuid()), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }
}

#endregion

#region GetPlatformProducts

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetPlatformProductsHandlerTests
{
    private readonly Mock<IAdapterFactory> _adapterFactory = new();
    private readonly Mock<ILogger<GetPlatformProductsHandler>> _logger = new();

    [Fact]
    public async Task Handle_NoAdapter_ShouldReturnEmptyResult()
    {
        _adapterFactory.Setup(f => f.Resolve("UNKNOWN")).Returns((IIntegratorAdapter?)null);

        var handler = new GetPlatformProductsHandler(_adapterFactory.Object, _logger.Object);
        var result = await handler.Handle(new GetPlatformProductsQuery("UNKNOWN"), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithAdapter_ShouldPullAndPageProducts()
    {
        var products = Enumerable.Range(1, 5)
            .Select(i => FakeData.CreateProduct(sku: $"PLAT-{i}")).ToList();

        var adapter = new Mock<IIntegratorAdapter>();
        adapter.Setup(a => a.PullProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);
        _adapterFactory.Setup(f => f.Resolve("TRENDYOL")).Returns(adapter.Object);

        var handler = new GetPlatformProductsHandler(_adapterFactory.Object, _logger.Object);
        var result = await handler.Handle(
            new GetPlatformProductsQuery("TRENDYOL", PageSize: 3), CancellationToken.None);

        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(5);
    }
}

#endregion
