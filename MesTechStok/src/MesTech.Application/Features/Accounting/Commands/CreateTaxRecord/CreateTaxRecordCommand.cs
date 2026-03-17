using MediatR;

namespace MesTech.Application.Features.Accounting.Commands.CreateTaxRecord;

public record CreateTaxRecordCommand(
    Guid TenantId,
    string Period,
    string TaxType,
    decimal TaxableAmount,
    decimal TaxRate,
    decimal TaxAmount,
    DateTime DueDate,
    int? Year = null
) : IRequest<Guid>;
