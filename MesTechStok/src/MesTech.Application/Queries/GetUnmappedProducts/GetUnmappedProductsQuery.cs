using MesTech.Application.DTOs;
using MesTech.Domain.Enums;
using MediatR;

namespace MesTech.Application.Queries.GetUnmappedProducts;

public record GetUnmappedProductsQuery(PlatformType Platform) : IRequest<List<ProductDto>>;
