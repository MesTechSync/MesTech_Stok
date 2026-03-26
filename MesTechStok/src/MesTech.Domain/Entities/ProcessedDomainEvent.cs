using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// İşlenmiş domain event kaydı — idempotency guard.
/// Aynı EventId'li event tekrar işlenmez (çift stok düşürme, çift GL kaydı koruması).
/// </summary>
public sealed class ProcessedDomainEvent : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public string? HandlerName { get; set; }
}
