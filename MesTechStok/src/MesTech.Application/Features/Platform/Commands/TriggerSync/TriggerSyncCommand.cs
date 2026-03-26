using MediatR;

namespace MesTech.Application.Features.Platform.Commands.TriggerSync;

/// <summary>
/// Belirtilen platform için anlık senkronizasyon başlatır — Ekran 19 "Şimdi Senkron Et".
/// </summary>
public record TriggerSyncCommand(
    Guid TenantId,
    string PlatformCode
) : IRequest<TriggerSyncResult>;

public sealed class TriggerSyncResult
{
    public bool IsSuccess { get; set; }
    public string? JobId { get; set; }
    public string? ErrorMessage { get; set; }
}
