using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

/// <summary>Platform senkronizasyon hatasi olustu — MESA OS alarm sistemi tetikler.</summary>
public record SyncErrorOccurredEvent(
    string Platform,
    string ErrorType,
    string Message,
    DateTime OccurredAt) : IDomainEvent;
