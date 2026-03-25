using MediatR;

namespace MesTech.Application.Queries.GetInventoryPaged;

public record GetInventoryPagedQuery(
    int Page = 1,
    int PageSize = 50,
    string? SearchTerm = null,
    StockStatusFilter StatusFilter = StockStatusFilter.All,
    InventorySortOrder SortOrder = InventorySortOrder.ProductName
) : IRequest<GetInventoryPagedResult>;

public enum StockStatusFilter { All, Normal, Low, Critical, OutOfStock }

public enum InventorySortOrder
{
    ProductName, ProductNameDesc,
    Stock, StockDesc,
    Category, Location,
    LastMovement, LastMovementDesc
}

public sealed class GetInventoryPagedResult
{
    public IReadOnlyList<DTOs.InventoryItemDto> Items { get; set; } = [];
    public int TotalItems { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
