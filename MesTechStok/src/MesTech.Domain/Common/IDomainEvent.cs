namespace MesTech.Domain.Common;

/// <summary>
/// Domain Event marker interface.
/// Tüm domain olayları bu interface'i implement eder.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}
