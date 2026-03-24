using MediatR;

namespace MesTech.Application.Features.Settings.Commands.UpdateProfileSettings;

public record UpdateProfileSettingsCommand(
    Guid TenantId,
    string Name,
    string? TaxNumber) : IRequest<bool>;
