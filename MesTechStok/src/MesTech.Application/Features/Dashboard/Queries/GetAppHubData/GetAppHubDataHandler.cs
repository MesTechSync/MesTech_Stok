using MediatR;
using MesTech.Application.Features.Dashboard.Queries.GetPendingInvoices;
using MesTech.Application.Features.Dashboard.Queries.GetServiceHealth;
using MesTech.Application.Queries.GetInventoryStatistics;
using MesTech.Application.Queries.GetProductDbStatus;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dashboard.Queries.GetAppHubData;

public sealed class GetAppHubDataHandler
    : IRequestHandler<GetAppHubDataQuery, AppHubDataDto>
{
    private readonly ISender _mediator;
    private readonly IOrderRepository _orderRepo;

    public GetAppHubDataHandler(ISender mediator, IOrderRepository orderRepo)
    {
        _mediator = mediator;
        _orderRepo = orderRepo ?? throw new ArgumentNullException(nameof(orderRepo));
    }

    public async Task<AppHubDataDto> Handle(
        GetAppHubDataQuery request, CancellationToken ct)
    {
        var productTask = _mediator.Send(new GetProductDbStatusQuery(), ct);
        var inventoryTask = _mediator.Send(new GetInventoryStatisticsQuery(), ct);
        var invoiceTask = _mediator.Send(new GetPendingInvoicesQuery(request.TenantId), ct);
        var healthTask = _mediator.Send(new GetServiceHealthQuery(), ct);
        var orderCountTask = _orderRepo.GetCountAsync();

        await Task.WhenAll(productTask, inventoryTask, invoiceTask, healthTask, orderCountTask).ConfigureAwait(false);

        var products = await productTask.ConfigureAwait(false);
        var inventory = await inventoryTask.ConfigureAwait(false);
        var invoices = await invoiceTask.ConfigureAwait(false);
        var health = await healthTask.ConfigureAwait(false);
        var totalOrders = await orderCountTask.ConfigureAwait(false);

        return new AppHubDataDto
        {
            TotalProducts = products.TotalCount,
            TotalOrders = totalOrders,
            InventoryValue = inventory.TotalInventoryValue,
            LowStockCount = inventory.LowStockCount,
            PendingInvoices = invoices.Count,
            ServiceHealth = health,
            GeneratedAt = DateTime.UtcNow
        };
    }
}
