using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Commands.UpdateCariHesap;

public record UpdateCariHesapCommand(
    Guid Id,
    string Name,
    string? TaxNumber,
    CariHesapType Type,
    string? Phone,
    string? Email,
    string? Address
) : IRequest<bool>;
