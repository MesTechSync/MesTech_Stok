using MediatR;

namespace MesTech.Application.Commands.CreateProduct;

public record CreateProductCommand(
    string Name,
    string SKU,
    string? Barcode,
    decimal PurchasePrice,
    decimal SalePrice,
    int CategoryId,
    int? SupplierId = null,
    int? WarehouseId = null,
    string? Description = null,
    int MinimumStock = 5,
    int MaximumStock = 1000,
    decimal TaxRate = 0.18m,
    string? Brand = null,
    string? ImageUrl = null,
    bool SyncToPlatforms = true
) : IRequest<CreateProductResult>;

public class CreateProductResult
{
    public bool IsSuccess { get; set; }
    public int ProductId { get; set; }
    public string? ErrorMessage { get; set; }
}
