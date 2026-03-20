using MesTech.Domain.Enums;

namespace MesTech.Infrastructure.Integration.ERP;

/// <summary>
/// Bidirectional sync conflict resolution.
/// Rules:
///   Stock  → ERP wins (physical count at warehouse)
///   Price  → ERP wins (cost calculation in ERP)
///   Order  → MesTech wins (marketplace origin)
///   Invoice→ MesTech wins (e-invoice issued from MesTech)
///   Account→ Last-updated wins (UpdatedAt comparison)
/// </summary>
public sealed class ErpConflictResolver
{
    public ConflictResolution Resolve(
        SyncEntityType entityType,
        string entityCode,
        string mestechValue,
        string erpValue,
        DateTimeOffset? mestechUpdatedAt = null,
        DateTimeOffset? erpUpdatedAt = null)
    {
        var winner = entityType switch
        {
            SyncEntityType.Stock => SyncSource.Erp,
            SyncEntityType.Price => SyncSource.Erp,
            SyncEntityType.Order => SyncSource.MesTech,
            SyncEntityType.Invoice => SyncSource.MesTech,
            SyncEntityType.Account => ResolveByTimestamp(mestechUpdatedAt, erpUpdatedAt),
            _ => SyncSource.Erp
        };

        return new ConflictResolution
        {
            EntityType = entityType,
            EntityCode = entityCode,
            MestechValue = mestechValue,
            ErpValue = erpValue,
            Winner = winner,
            WinnerValue = winner == SyncSource.Erp ? erpValue : mestechValue,
            ResolvedAt = DateTimeOffset.UtcNow,
            Resolution = "Auto"
        };
    }

    private static SyncSource ResolveByTimestamp(
        DateTimeOffset? mestechUpdatedAt, DateTimeOffset? erpUpdatedAt)
    {
        if (mestechUpdatedAt is null && erpUpdatedAt is null) return SyncSource.Erp;
        if (mestechUpdatedAt is null) return SyncSource.Erp;
        if (erpUpdatedAt is null) return SyncSource.MesTech;
        return mestechUpdatedAt >= erpUpdatedAt ? SyncSource.MesTech : SyncSource.Erp;
    }
}

public enum SyncEntityType
{
    Stock,
    Price,
    Order,
    Invoice,
    Account
}

public enum SyncSource
{
    MesTech,
    Erp
}

public sealed class ConflictResolution
{
    public SyncEntityType EntityType { get; init; }
    public string EntityCode { get; init; } = string.Empty;
    public string MestechValue { get; init; } = string.Empty;
    public string ErpValue { get; init; } = string.Empty;
    public SyncSource Winner { get; init; }
    public string WinnerValue { get; init; } = string.Empty;
    public DateTimeOffset ResolvedAt { get; init; }
    public string Resolution { get; init; } = "Auto";
}
