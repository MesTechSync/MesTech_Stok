using MediatR;

namespace MesTech.Application.Features.Accounting.Commands.RecordTaxWithholding;

public record RecordTaxWithholdingCommand(
    Guid TenantId,
    decimal TaxExclusiveAmount,
    decimal Rate,
    string TaxType,
    Guid? InvoiceId = null
) : IRequest<Guid>;
