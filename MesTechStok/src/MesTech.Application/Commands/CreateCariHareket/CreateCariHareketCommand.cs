using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Commands.CreateCariHareket;

public record CreateCariHareketCommand(
    Guid TenantId,
    Guid CariHesapId,
    decimal Amount,
    CariDirection Direction,
    string Description,
    DateTime? Date,
    Guid? InvoiceId,
    Guid? OrderId
) : IRequest<Guid>;
