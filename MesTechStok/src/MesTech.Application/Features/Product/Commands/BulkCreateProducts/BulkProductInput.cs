namespace MesTech.Application.Features.Product.Commands.BulkCreateProducts;

public sealed class BulkProductInput
{
    public string Name { get; init; } = string.Empty;
    public string SKU { get; init; } = string.Empty;
    public string? Barcode { get; init; }
    public decimal Price { get; init; }
    public int Quantity { get; init; }
    public Guid? CategoryId { get; init; }
    public string? Description { get; init; }
}
