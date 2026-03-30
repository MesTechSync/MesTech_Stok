namespace MesTech.Application.Features.Product.Commands.BulkCreateProducts;

public sealed class BulkCreateProductsResult
{
    public int TotalReceived { get; init; }
    public int SuccessCount { get; init; }
    public int FailCount { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}
