using MediatR;

namespace MesTech.Application.Features.EInvoice.Commands;

public record SendEInvoiceCommand(Guid EInvoiceId) : IRequest<bool>;
