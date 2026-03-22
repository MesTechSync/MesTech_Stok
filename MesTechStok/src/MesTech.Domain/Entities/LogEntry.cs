using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

public class LogEntry : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Level { get; set; } = "Info";
    public string Category { get; set; } = "General";
    public string Message { get; set; } = string.Empty;
    public string? Data { get; set; }
    public string? UserId { get; set; }
    public string? Exception { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? MachineName { get; set; }
}
