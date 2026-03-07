using MediatR;

namespace MesTech.Application.Commands.UpdateProduct;

public record UpdateProductCommand(
    int ProductId,
    string? Name = null,
    string? Description = null,
    decimal? PurchasePrice = null,
    decimal? SalePrice = null,
    decimal? ListPrice = null,
    decimal? TaxRate = null,
    int? CategoryId = null,
    int? SupplierId = null,
    int? WarehouseId = null,
    int? MinimumStock = null,
    int? MaximumStock = null,
    string? Brand = null,
    int? BrandId = null,
    bool? IsActive = null,
    bool SyncToPlatforms = true
) : IRequest<UpdateProductResult>;

public class UpdateProductResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
