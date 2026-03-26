using MediatR;

namespace MesTech.Application.Features.Settings.Commands.UpdateStoreSettings;

public record UpdateStoreSettingsCommand(
    Guid TenantId,
    string CompanyName,
    string? TaxNumber,
    string? Phone,
    string? Email,
    string? Address
) : IRequest<bool>;
