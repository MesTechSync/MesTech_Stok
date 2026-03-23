using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Application.Queries.GetDashboardData;
using MesTechStok.Desktop.Data;

namespace MesTechStok.Desktop.Handlers;

public class GetRecentStockMovementsHandler : IRequestHandler<GetRecentStockMovementsQuery, IReadOnlyList<RecentMovementDto>>
{
    private readonly IServiceProvider _serviceProvider;

    public GetRecentStockMovementsHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<IReadOnlyList<RecentMovementDto>> Handle(GetRecentStockMovementsQuery request, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DesktopDbContext>();

        var movements = await context.StockMovements
            .Include(sm => sm.Product)
            .OrderByDescending(sm => sm.Date)
            .Take(request.Count)
            .Select(sm => new RecentMovementDto
            {
                Date = sm.Date,
                MovementType = sm.MovementType,
                Quantity = sm.Quantity,
                Reason = sm.Reason,
                Product = new RecentMovementProductDto
                {
                    Name = sm.Product.Name,
                },
            })
            .ToListAsync(cancellationToken);

        return movements;
    }
}
