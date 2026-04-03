using FluentAssertions;
using MediatR;
using MesTech.Application.DTOs;
using MesTech.Application.Features.Dashboard.Queries.GetAppHubData;
using MesTech.Application.Features.Dashboard.Queries.GetPendingInvoices;
using MesTech.Application.Features.Dashboard.Queries.GetServiceHealth;
using MesTech.Application.Queries.GetInventoryStatistics;
using MesTech.Application.Queries.GetProductDbStatus;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Queries;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetAppHubDataHandlerTests
{
    private readonly Mock<ISender> _mediator = new();
    private readonly Mock<IOrderRepository> _orderRepo = new();

    private GetAppHubDataHandler CreateHandler() =>
        new(_mediator.Object, _orderRepo.Object);

    [Fact]
    public void Constructor_NullOrderRepo_ShouldThrow()
    {
        var act = () => new GetAppHubDataHandler(_mediator.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("orderRepo");
    }

    [Fact]
    public async Task Handle_ShouldAggregateAllSubQueries()
    {
        var tenantId = Guid.NewGuid();

        _mediator.Setup(m => m.Send(It.IsAny<GetProductDbStatusQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductDbStatusDto { TotalCount = 150, ActiveCount = 120, IsConnected = true });

        _mediator.Setup(m => m.Send(It.IsAny<GetInventoryStatisticsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryStatisticsDto { TotalInventoryValue = 50000m, LowStockCount = 5 });

        var pendingInvoices = new List<PendingInvoiceDto>
        {
            new() { InvoiceId = Guid.NewGuid(), InvoiceNumber = "INV-001" },
            new() { InvoiceId = Guid.NewGuid(), InvoiceNumber = "INV-002" },
            new() { InvoiceId = Guid.NewGuid(), InvoiceNumber = "INV-003" }
        };
        _mediator.Setup(m => m.Send(It.IsAny<GetPendingInvoicesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingInvoices.AsReadOnly());

        var healthList = new List<ServiceHealthDto>
        {
            new() { ServiceName = "PostgreSQL", IsHealthy = true, ResponseTime = "12ms" }
        };
        _mediator.Setup(m => m.Send(It.IsAny<GetServiceHealthQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthList.AsReadOnly());

        _orderRepo.Setup(r => r.GetCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var handler = CreateHandler();
        var query = new GetAppHubDataQuery(tenantId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.TotalProducts.Should().Be(150);
        result.TotalOrders.Should().Be(42);
        result.InventoryValue.Should().Be(50000m);
        result.LowStockCount.Should().Be(5);
        result.PendingInvoices.Should().Be(3);
        result.ServiceHealth.Should().HaveCount(1);
        result.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_ZeroCounts_ShouldReturnZeros()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetProductDbStatusQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductDbStatusDto { TotalCount = 0 });

        _mediator.Setup(m => m.Send(It.IsAny<GetInventoryStatisticsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryStatisticsDto { TotalInventoryValue = 0, LowStockCount = 0 });

        _mediator.Setup(m => m.Send(It.IsAny<GetPendingInvoicesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PendingInvoiceDto>().AsReadOnly());

        _mediator.Setup(m => m.Send(It.IsAny<GetServiceHealthQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ServiceHealthDto>().AsReadOnly());

        _orderRepo.Setup(r => r.GetCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetAppHubDataQuery(Guid.NewGuid()), CancellationToken.None);

        result.TotalProducts.Should().Be(0);
        result.TotalOrders.Should().Be(0);
        result.InventoryValue.Should().Be(0);
        result.PendingInvoices.Should().Be(0);
    }
}
