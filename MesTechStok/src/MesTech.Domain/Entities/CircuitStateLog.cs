using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

public class CircuitStateLog : BaseEntity
{
    public string PreviousState { get; set; } = string.Empty;
    public string NewState { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public double FailureRate { get; set; }
    public int WindowTotalCalls { get; set; }
    public DateTime TransitionTimeUtc { get; set; } = DateTime.UtcNow;
    public string? CorrelationId { get; set; }
    public string? AdditionalInfo { get; set; }
}
