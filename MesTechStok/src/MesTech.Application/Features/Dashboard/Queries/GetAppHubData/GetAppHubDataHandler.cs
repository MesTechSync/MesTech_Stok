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
        GetAppHubDataQuery request, CancellationToken cancellationToken)
    {
        // Sequential execution — Task.WhenAll causes concurrent DbContext access (G67/KÖK-1)
        var products = await _mediator.Send(new GetProductDbStatusQuery(), cancellationToken).ConfigureAwait(false);
        var inventory = await _mediator.Send(new GetInventoryStatisticsQuery(), cancellationToken).ConfigureAwait(false);
        var invoices = await _mediator.Send(new GetPendingInvoicesQuery(request.TenantId), cancellationToken).ConfigureAwait(false);
        var health = await _mediator.Send(new GetServiceHealthQuery(), cancellationToken).ConfigureAwait(false);
        var totalOrders = await _orderRepo.GetCountAsync(cancellationToken).ConfigureAwait(false);

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
