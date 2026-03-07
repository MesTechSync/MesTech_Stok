using MediatR;
using MesTech.Application.DTOs;

namespace MesTech.Application.Queries.GetProductById;

public record GetProductByIdQuery(int ProductId) : IRequest<ProductDto?>;
