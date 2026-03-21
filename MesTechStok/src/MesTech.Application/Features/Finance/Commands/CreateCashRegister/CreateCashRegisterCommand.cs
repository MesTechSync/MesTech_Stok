using MediatR;

namespace MesTech.Application.Features.Finance.Commands.CreateCashRegister;

public record CreateCashRegisterCommand(
    Guid TenantId, string Name, string CurrencyCode = "TRY",
    bool IsDefault = false, decimal OpeningBalance = 0m
) : IRequest<Guid>;
