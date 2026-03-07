using MediatR;
using MesTech.Application.DTOs;

namespace MesTech.Application.Queries.GetStockMovements;

public record GetStockMovementsQuery(
    Guid? ProductId = null,
    DateTime? From = null,
    DateTime? To = null
) : IRequest<IReadOnlyList<StockMovementDto>>;
