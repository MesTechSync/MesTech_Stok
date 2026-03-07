using MediatR;
using MesTech.Application.DTOs;

namespace MesTech.Application.Queries.GetProductById;

public record GetProductByIdQuery(Guid ProductId) : IRequest<ProductDto?>;
