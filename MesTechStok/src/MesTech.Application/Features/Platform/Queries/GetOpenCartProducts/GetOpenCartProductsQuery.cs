using MediatR;

namespace MesTech.Application.Features.Platform.Queries.GetOpenCartProducts;

public record GetOpenCartProductsQuery(
    Guid TenantId,
    Guid StoreId,
    int Page = 1,
    int PageSize = 50,
    string? SearchTerm = null
) : IRequest<GetOpenCartProductsResult>;

public sealed class GetOpenCartProductsResult
{
    public IReadOnlyList<OpenCartProductDto> Products { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
