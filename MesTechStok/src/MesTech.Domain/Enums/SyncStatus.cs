namespace MesTech.Domain.Enums;

/// <summary>
/// Platform senkronizasyon durumu.
/// </summary>
public enum SyncStatus
{
    NotSynced = 0,
    Syncing = 1,
    Synced = 2,
    Failed = 3,
    PendingSync = 4
}
