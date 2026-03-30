namespace MesTech.Application.Features.Platform.Queries.GetOpenCartProducts;

public sealed class OpenCartProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string SKU { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int Quantity { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime? LastSyncAt { get; init; }
    public string? OpenCartId { get; init; }
}
