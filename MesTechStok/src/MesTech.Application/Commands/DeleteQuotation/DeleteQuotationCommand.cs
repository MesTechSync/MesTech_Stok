using MediatR;

namespace MesTech.Application.Commands.DeleteQuotation;

public record DeleteQuotationCommand(Guid Id) : IRequest<DeleteQuotationResult>;
