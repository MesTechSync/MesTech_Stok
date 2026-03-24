using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Events;

public record SupplierFeedSyncedEvent(
    Guid SupplierFeedId,
    Guid TenantId,
    Guid SupplierId,
    int TotalProducts,
    int UpdatedProducts,
    int DeactivatedProducts,
    FeedSyncStatus Status,
    DateTime OccurredAt
) : IDomainEvent;
