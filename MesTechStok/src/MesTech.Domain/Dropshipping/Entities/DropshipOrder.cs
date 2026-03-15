using MesTech.Domain.Common;
using MesTech.Domain.Dropshipping.Enums;

namespace MesTech.Domain.Dropshipping.Entities;

/// <summary>
/// Dropshipping sipariş kaydı.
/// MesTech siparişi ile tedarikçi siparişi arasındaki köprü.
/// Durum geçişleri guard validation ile korunur.
/// </summary>
public class DropshipOrder : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    /// <summary>MesTech iç sipariş ID'si.</summary>
    public Guid OrderId { get; private set; }

    public Guid DropshipSupplierId { get; private set; }
    public Guid DropshipProductId { get; private set; }

    /// <summary>Tedarikçi tarafındaki sipariş referans numarası.</summary>
    public string? SupplierOrderRef { get; private set; }

    /// <summary>Tedarikçiden alınan kargo takip numarası.</summary>
    public string? SupplierTrackingNumber { get; private set; }

    public DropshipOrderStatus Status { get; private set; } = DropshipOrderStatus.Pending;
    public string? FailureReason { get; private set; }

    public DateTime? OrderedAt { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }

    // EF Core parametresiz ctor
    private DropshipOrder() { }

    /// <summary>
    /// Factory method — yeni dropship sipariş kaydı oluşturur.
    /// </summary>
    public static DropshipOrder Create(
        Guid tenantId,
        Guid orderId,
        Guid supplierId,
        Guid productId)
    {
        return new DropshipOrder
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrderId = orderId,
            DropshipSupplierId = supplierId,
            DropshipProductId = productId,
            Status = DropshipOrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Tedarikçiye sipariş verildi olarak işaretle.
    /// Guard: sadece Pending durumundan geçilebilir.
    /// </summary>
    public void PlaceWithSupplier(string supplierOrderRef)
    {
        GuardStatus(DropshipOrderStatus.Pending, nameof(PlaceWithSupplier));
        ArgumentException.ThrowIfNullOrWhiteSpace(supplierOrderRef);

        SupplierOrderRef = supplierOrderRef.Trim();
        Status = DropshipOrderStatus.OrderedFromSupplier;
        OrderedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Tedarikçi siparişi kargoya verildi.
    /// Guard: sadece OrderedFromSupplier durumundan geçilebilir.
    /// </summary>
    public void MarkShipped(string trackingNumber)
    {
        GuardStatus(DropshipOrderStatus.OrderedFromSupplier, nameof(MarkShipped));
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);

        SupplierTrackingNumber = trackingNumber.Trim();
        Status = DropshipOrderStatus.Shipped;
        ShippedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sipariş teslim edildi.
    /// Guard: sadece Shipped durumundan geçilebilir.
    /// </summary>
    public void MarkDelivered()
    {
        GuardStatus(DropshipOrderStatus.Shipped, nameof(MarkDelivered));

        Status = DropshipOrderStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sipariş başarısız olarak işaretle.
    /// Guard: sadece Pending veya OrderedFromSupplier durumlarından geçilebilir.
    /// </summary>
    public void MarkFailed(string reason)
    {
        if (Status is DropshipOrderStatus.Delivered or DropshipOrderStatus.Failed)
            throw new InvalidOperationException(
                $"Cannot mark as failed from status '{Status}'. " +
                $"Only Pending or OrderedFromSupplier orders can be marked as failed.");

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        FailureReason = reason.Trim();
        Status = DropshipOrderStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }

    private void GuardStatus(DropshipOrderStatus expectedCurrent, string operationName)
    {
        if (Status != expectedCurrent)
            throw new InvalidOperationException(
                $"Cannot perform '{operationName}' when status is '{Status}'. Expected: '{expectedCurrent}'.");
    }

    public override string ToString() =>
        $"DropshipOrder [{Id}] Order:{OrderId} Status:{Status} SupplierRef:{SupplierOrderRef}";
}
