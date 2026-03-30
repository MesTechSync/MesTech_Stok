using MediatR;

namespace MesTech.Application.Features.Settings.Commands.SaveFulfillmentSettings;

public record SaveFulfillmentSettingsCommand(
    Guid TenantId,
    bool FbaAutoReplenish,
    bool HepsiAutoReplenish
) : IRequest<SaveFulfillmentSettingsResult>;
