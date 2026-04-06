using MediatR;

namespace MesTech.Application.Queries.GetStockLotById;

public record GetStockLotByIdQuery(Guid Id) : IRequest<GetStockLotByIdResult?>;
