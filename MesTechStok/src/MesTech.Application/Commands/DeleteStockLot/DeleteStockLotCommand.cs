using MediatR;

namespace MesTech.Application.Commands.DeleteStockLot;

public record DeleteStockLotCommand(Guid Id) : IRequest<DeleteStockLotResult>;
