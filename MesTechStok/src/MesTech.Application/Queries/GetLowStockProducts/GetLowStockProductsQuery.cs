using MediatR;
using MesTech.Application.DTOs;

namespace MesTech.Application.Queries.GetLowStockProducts;

public record GetLowStockProductsQuery() : IRequest<IReadOnlyList<ProductDto>>;
