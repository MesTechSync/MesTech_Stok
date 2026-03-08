using MediatR;

namespace MesTech.Application.Queries.GetDashboardData;

public record GetRecentStockMovementsQuery(int Count = 10) : IRequest<IReadOnlyList<RecentMovementDto>>;

public class RecentMovementDto
{
    public DateTime Date { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Reason { get; set; }
    public RecentMovementProductDto Product { get; set; } = new();
}

public class RecentMovementProductDto
{
    public string Name { get; set; } = string.Empty;
}
