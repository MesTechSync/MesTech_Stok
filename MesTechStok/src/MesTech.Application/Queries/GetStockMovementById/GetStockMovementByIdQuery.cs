using MesTech.Application.DTOs;
using MediatR;

namespace MesTech.Application.Queries.GetStockMovementById;

public record GetStockMovementByIdQuery(Guid Id) : IRequest<StockMovementDto?>;
