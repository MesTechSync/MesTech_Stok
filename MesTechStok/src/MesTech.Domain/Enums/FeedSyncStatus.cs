namespace MesTech.Domain.Enums;

/// <summary>
/// Tedarikçi feed sync durumları.
/// </summary>
public enum FeedSyncStatus
{
    None = 0,
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    PartiallyCompleted = 4,
    Failed = 5
}
