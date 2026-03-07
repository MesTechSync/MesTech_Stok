using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

public class AccessLog : BaseEntity
{
    public int UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public bool IsAllowed { get; set; }
    public DateTime AccessTime { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? AdditionalInfo { get; set; }
    public string? CorrelationId { get; set; }
}
