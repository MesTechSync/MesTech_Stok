using MesTech.Domain.Common;
using MesTech.Domain.Interfaces;

namespace MesTech.Domain.Entities;

public class CircuitStateLog : BaseEntity, ITenantEntity
{
    public string PreviousState { get; set; } = string.Empty;
    public string NewState { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public double FailureRate { get; set; }
    public int WindowTotalCalls { get; set; }
    public DateTime TransitionTimeUtc { get; set; } = DateTime.UtcNow;
    public string? CorrelationId { get; set; }
    public string? AdditionalInfo { get; set; }
    public Guid TenantId { get; set; }
}
