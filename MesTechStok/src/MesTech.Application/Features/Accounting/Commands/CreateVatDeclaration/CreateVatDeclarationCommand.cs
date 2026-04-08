using MediatR;

namespace MesTech.Application.Features.Accounting.Commands.CreateVatDeclaration;

public record CreateVatDeclarationCommand(
    Guid TenantId,
    int Year,
    int Month
) : IRequest<Guid>;
