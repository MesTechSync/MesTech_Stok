namespace MesTech.Application.Interfaces;

public interface IProductSearchService
{
    Task<ProductSearchResult> SearchAsync(
        string query, Guid tenantId,
        int page = 1, int pageSize = 20,
        CancellationToken ct = default);

    Task<IReadOnlyList<SimilarProduct>> FindSimilarAsync(
        Guid productId, int maxResults = 10,
        CancellationToken ct = default);

    Task<IReadOnlyList<ProductSummary>> DiscoverByCategoryAsync(
        string categoryName, Guid tenantId,
        int maxResults = 20,
        CancellationToken ct = default);
}

public record ProductSearchResult(
    IReadOnlyList<ProductSummary> Items,
    int TotalCount,
    int Page,
    int PageSize,
    string? DidYouMean);

public record ProductSummary(
    Guid ProductId,
    string SKU,
    string Name,
    string? Category,
    decimal SalePrice,
    int StockQuantity,
    double Relevance);

public record SimilarProduct(
    Guid ProductId,
    string SKU,
    string Name,
    decimal SalePrice,
    double Similarity,
    string Reason);
