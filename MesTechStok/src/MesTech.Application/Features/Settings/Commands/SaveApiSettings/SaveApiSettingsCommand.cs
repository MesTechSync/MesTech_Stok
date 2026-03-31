using MediatR;

namespace MesTech.Application.Features.Settings.Commands.SaveApiSettings;

public record SaveApiSettingsCommand(
    Guid TenantId,
    string ApiBaseUrl,
    string? WebhookSecret,
    int RateLimitPerMinute,
    bool EnableCors
) : IRequest<SaveApiSettingsResult>;
