using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Commands.CreateCariHesap;

public record CreateCariHesapCommand(
    Guid TenantId,
    string Name,
    string? TaxNumber,
    CariHesapType Type,
    string? Phone,
    string? Email,
    string? Address
) : IRequest<Guid>;
