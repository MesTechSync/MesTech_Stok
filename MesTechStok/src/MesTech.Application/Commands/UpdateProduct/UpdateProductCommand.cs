using MediatR;

namespace MesTech.Application.Commands.UpdateProduct;

public record UpdateProductCommand(
    Guid ProductId,
    string? Name = null,
    string? Description = null,
    decimal? PurchasePrice = null,
    decimal? SalePrice = null,
    decimal? ListPrice = null,
    decimal? TaxRate = null,
    Guid? CategoryId = null,
    Guid? SupplierId = null,
    Guid? WarehouseId = null,
    int? MinimumStock = null,
    int? MaximumStock = null,
    string? Brand = null,
    Guid? BrandId = null,
    bool? IsActive = null,
    bool SyncToPlatforms = true
) : IRequest<UpdateProductResult>;

public class UpdateProductResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
