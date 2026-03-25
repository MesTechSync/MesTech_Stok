using MediatR;

namespace MesTech.Application.Commands.CreateQuotation;

public record CreateQuotationLineInput(
    Guid? ProductId,
    string ProductName,
    string? SKU,
    int Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    string? Description = null
);

public record CreateQuotationCommand(
    string QuotationNumber,
    DateTime ValidUntil,
    Guid? CustomerId = null,
    string CustomerName = "",
    string? CustomerTaxNumber = null,
    string? CustomerTaxOffice = null,
    string? CustomerAddress = null,
    string? CustomerEmail = null,
    string? Notes = null,
    string? Terms = null,
    List<CreateQuotationLineInput>? Lines = null
) : IRequest<CreateQuotationResult>;

public sealed class CreateQuotationResult
{
    public bool IsSuccess { get; set; }
    public Guid QuotationId { get; set; }
    public string? ErrorMessage { get; set; }
}
