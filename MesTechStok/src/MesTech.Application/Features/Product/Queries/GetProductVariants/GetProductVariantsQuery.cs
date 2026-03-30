using MediatR;

namespace MesTech.Application.Features.Product.Queries.GetProductVariants;

public record GetProductVariantsQuery(Guid TenantId, Guid ProductId) : IRequest<ProductVariantMatrixDto>;

public sealed class ProductVariantMatrixDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public IReadOnlyList<VariantRowDto> Variants { get; init; } = [];
    public int TotalStock { get; init; }
}

public sealed class VariantRowDto
{
    public Guid VariantId { get; init; }
    public string SKU { get; init; } = string.Empty;
    public string? Barcode { get; init; }
    public string? Color { get; init; }
    public string? Size { get; init; }
    public int Stock { get; init; }
    public decimal SalePrice { get; init; }
    public decimal? PurchasePrice { get; init; }
    public bool IsActive { get; init; }
    public IReadOnlyDictionary<string, string> Attributes { get; init; } = new Dictionary<string, string>();
}
