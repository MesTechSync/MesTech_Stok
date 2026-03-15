using MediatR;

namespace MesTech.Application.Features.EInvoice.Commands;

public record CancelEInvoiceCommand(Guid EInvoiceId, string Reason) : IRequest<bool>;
