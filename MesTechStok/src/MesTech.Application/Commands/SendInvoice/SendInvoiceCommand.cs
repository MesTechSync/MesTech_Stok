using MediatR;

namespace MesTech.Application.Commands.SendInvoice;

public record SendInvoiceCommand(Guid InvoiceId) : IRequest<SendInvoiceResult>;

public sealed class SendInvoiceResult
{
    public bool IsSuccess { get; set; }
    public string? ProviderRef { get; set; }
    public string? ErrorMessage { get; set; }
}
