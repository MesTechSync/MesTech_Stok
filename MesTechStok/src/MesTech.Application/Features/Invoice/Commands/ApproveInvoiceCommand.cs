using MediatR;

namespace MesTech.Application.Features.Invoice.Commands;

public record ApproveInvoiceCommand(Guid InvoiceId) : IRequest<bool>;
