using MediatR;

namespace MesTech.Application.Commands.AcceptQuotation;

public record AcceptQuotationCommand(Guid QuotationId) : IRequest<AcceptQuotationResult>;

public class AcceptQuotationResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
