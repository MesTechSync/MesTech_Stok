using MediatR;

namespace MesTech.Application.Commands.RejectQuotation;

public record RejectQuotationCommand(Guid QuotationId) : IRequest<RejectQuotationResult>;

public sealed class RejectQuotationResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
