using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

public sealed class ApiCallLog : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int? StatusCode { get; set; }
    public string? Category { get; set; }
    public long DurationMs { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string? CorrelationId { get; set; }
}
