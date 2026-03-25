using MediatR;

namespace MesTech.Application.Commands.CreateProduct;

public record CreateProductCommand(
    string Name,
    string SKU,
    string? Barcode,
    decimal PurchasePrice,
    decimal SalePrice,
    Guid CategoryId,
    Guid? SupplierId = null,
    Guid? WarehouseId = null,
    string? Description = null,
    int MinimumStock = 5,
    int MaximumStock = 1000,
    decimal TaxRate = 0.18m,
    string? Brand = null,
    string? ImageUrl = null,
    bool SyncToPlatforms = true
) : IRequest<CreateProductResult>;

public sealed class CreateProductResult
{
    public bool IsSuccess { get; set; }
    public Guid ProductId { get; set; }
    public string? ErrorMessage { get; set; }
}
