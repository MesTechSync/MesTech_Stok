using MesTech.Domain.Common;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;

namespace MesTech.Domain.Entities;

/// <summary>
/// Platform-agnostik iade talebi. Platform kuralları config-driven uygulanır.
/// İade onaylandığında stok atomik olarak artırılır (aynı transaction).
/// </summary>
public sealed class ReturnRequest : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }
    public Guid? StoreId { get; set; }

    public string? PlatformReturnId { get; set; }
    public PlatformType Platform { get; set; }
    public ReturnStatus Status { get; private set; } = ReturnStatus.Pending;
    public ReturnReason Reason { get; set; } = ReturnReason.None;
    public string? ReasonDetail { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }

    public decimal RefundAmount { get; private set; }
    public string Currency { get; set; } = "TRY";

    public string? TrackingNumber { get; private set; }
    public CargoProvider CargoProvider { get; private set; } = CargoProvider.None;
    public bool IsCargoFree { get; set; }

    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? ReceivedAt { get; private set; }
    public DateTime? RefundedAt { get; private set; }
    public DateTime? DeadlineDate { get; set; }

    public string? Notes { get; set; }
    public bool StockRestored { get; private set; }

    // Navigation
    public Order? Order { get; set; }
    public Store? Store { get; set; }

    private readonly List<ReturnRequestLine> _lines = new();
    public IReadOnlyCollection<ReturnRequestLine> Lines => _lines.AsReadOnly();

    public void AddLine(ReturnRequestLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
        _lines.Add(line);
        RefundAmount = _lines.Sum(l => l.RefundAmount);
    }

    public void Approve()
    {
        if (Status != ReturnStatus.Pending)
            throw new InvalidOperationException("Sadece bekleyen iade onaylanabilir.");
        Status = ReturnStatus.Approved;
        ApprovedAt = DateTime.UtcNow;

        var lineInfos = _lines
            .Where(l => l.ProductId.HasValue)
            .Select(l => new ReturnLineInfoEvent(
                l.ProductId!.Value, l.SKU ?? "", l.Quantity, l.UnitPrice))
            .ToList();

        RaiseDomainEvent(new ReturnApprovedEvent(
            Id, OrderId, TenantId, lineInfos, DateTime.UtcNow));
    }

    public void Reject(string? reason = null)
    {
        if (Status != ReturnStatus.Pending)
            throw new InvalidOperationException("Sadece bekleyen iade reddedilebilir.");
        Status = ReturnStatus.Rejected;
        if (reason != null)
            Notes = reason;
    }

    public void MarkAsReceived()
    {
        if (Status != ReturnStatus.Approved && Status != ReturnStatus.InTransit)
            throw new InvalidOperationException("İade ürünü teslim alınamaz — onay veya kargoda olmalı.");
        Status = ReturnStatus.Received;
        ReceivedAt = DateTime.UtcNow;
    }

    public void MarkAsRefunded()
    {
        if (Status != ReturnStatus.Received)
            throw new InvalidOperationException("İade ürünü teslim alınmadan iade yapılamaz.");
        Status = ReturnStatus.Refunded;
        RefundedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ReturnResolvedEvent(Id, TenantId, OrderId, ReturnStatus.Refunded, RefundAmount, DateTime.UtcNow));
    }

    public void MarkStockRestored()
    {
        if (Status is not (ReturnStatus.Approved or ReturnStatus.Received or ReturnStatus.Refunded))
            throw new InvalidOperationException($"Stok geri yükleme sadece onay/teslim/iade durumunda yapılabilir. Mevcut: {Status}");
        if (StockRestored)
            throw new InvalidOperationException("Stok zaten geri yüklenmiş — çift geri yükleme engellendi.");
        StockRestored = true;
    }

    public void SetCargoInfo(string trackingNumber, CargoProvider provider)
    {
        if (Status != ReturnStatus.Approved)
            throw new InvalidOperationException($"Kargo bilgisi sadece onaylanmış iadeye eklenebilir. Mevcut durum: {Status}");
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);
        TrackingNumber = trackingNumber;
        CargoProvider = provider;
        Status = ReturnStatus.InTransit;
    }

    public static ReturnRequest Create(
        Guid orderId,
        Guid tenantId,
        PlatformType platform,
        ReturnReason reason,
        string customerName,
        string? reasonDetail = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerName);

        var request = new ReturnRequest
        {
            OrderId = orderId,
            TenantId = tenantId,
            Platform = platform,
            Reason = reason,
            CustomerName = customerName,
            ReasonDetail = reasonDetail
        };
        request.RaiseDomainEvent(new ReturnCreatedEvent(
            request.Id, tenantId, orderId, platform, reason, DateTime.UtcNow));
        return request;
    }

    // Concurrency
    public byte[]? RowVersion { get; set; }

    public override string ToString() => $"Return #{PlatformReturnId ?? Id.ToString()[..8]} ({Status}) - {Platform}";
}
