using MediatR;

namespace MesTech.Application.Commands.ConvertQuotationToInvoice;

public record ConvertQuotationToInvoiceCommand(
    Guid QuotationId,
    string InvoiceNumber
) : IRequest<ConvertQuotationToInvoiceResult>;

public class ConvertQuotationToInvoiceResult
{
    public bool IsSuccess { get; set; }
    public Guid InvoiceId { get; set; }
    public string? ErrorMessage { get; set; }
}
