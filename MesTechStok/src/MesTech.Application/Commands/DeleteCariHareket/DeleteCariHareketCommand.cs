using MediatR;

namespace MesTech.Application.Commands.DeleteCariHareket;

public record DeleteCariHareketCommand(Guid Id) : IRequest<DeleteCariHareketResult>;
