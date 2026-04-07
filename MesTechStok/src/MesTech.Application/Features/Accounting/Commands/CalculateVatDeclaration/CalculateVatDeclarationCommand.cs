using MediatR;

namespace MesTech.Application.Features.Accounting.Commands.CalculateVatDeclaration;

public record CalculateVatDeclarationCommand(Guid DeclarationId) : IRequest<VatCalculationResult>;

public sealed class VatCalculationResult
{
    public bool IsSuccess { get; set; }
    public decimal TotalSales { get; set; }
    public decimal VatCollected { get; set; }
    public decimal VatPaid { get; set; }
    public decimal NetVatPayable { get; set; }
    public string? ErrorMessage { get; set; }
}
