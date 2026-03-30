using MediatR;

namespace MesTech.Application.Features.Product.Commands.SaveProductVariants;

public record SaveProductVariantsCommand(
    Guid TenantId,
    Guid ProductId,
    List<ProductVariantInput> Variants
) : IRequest<SaveProductVariantsResult>;

public sealed class ProductVariantInput
{
    public string SKU { get; init; } = string.Empty;
    public string? Color { get; init; }
    public string? Size { get; init; }
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public string? Barcode { get; init; }
    public bool IsActive { get; init; } = true;
}
