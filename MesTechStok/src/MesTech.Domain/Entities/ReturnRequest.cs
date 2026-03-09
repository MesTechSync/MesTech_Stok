using MesTech.Domain.Common;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;

namespace MesTech.Domain.Entities;

/// <summary>
/// Platform-agnostik iade talebi. Platform kuralları config-driven uygulanır.
/// İade onaylandığında stok atomik olarak artırılır (aynı transaction).
/// </summary>
public class ReturnRequest : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }
    public Guid? StoreId { get; set; }

    public string? PlatformReturnId { get; set; }
    public PlatformType Platform { get; set; }
    public ReturnStatus Status { get; set; } = ReturnStatus.Pending;
    public ReturnReason Reason { get; set; } = ReturnReason.None;
    public string? ReasonDetail { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }

    public decimal RefundAmount { get; set; }
    public string Currency { get; set; } = "TRY";

    public string? TrackingNumber { get; set; }
    public CargoProvider CargoProvider { get; set; } = CargoProvider.None;
    public bool IsCargoFree { get; set; }

    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public DateTime? DeadlineDate { get; set; }

    public string? Notes { get; set; }
    public bool StockRestored { get; set; }

    // Navigation
    public Order? Order { get; set; }
    public Store? Store { get; set; }

    private readonly List<ReturnRequestLine> _lines = new();
    public IReadOnlyCollection<ReturnRequestLine> Lines => _lines.AsReadOnly();

    public void AddLine(ReturnRequestLine line)
    {
        _lines.Add(line);
        RefundAmount = _lines.Sum(l => l.RefundAmount);
    }

    public void Approve()
    {
        if (Status != ReturnStatus.Pending)
            throw new InvalidOperationException("Sadece bekleyen iade onaylanabilir.");
        Status = ReturnStatus.Approved;
        ApprovedAt = DateTime.UtcNow;
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
        RaiseDomainEvent(new ReturnResolvedEvent(Id, OrderId, ReturnStatus.Refunded, RefundAmount, DateTime.UtcNow));
    }

    public void MarkStockRestored()
    {
        StockRestored = true;
    }

    public static ReturnRequest Create(
        Guid orderId,
        Guid tenantId,
        PlatformType platform,
        ReturnReason reason,
        string customerName,
        string? reasonDetail = null)
    {
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
            request.Id, orderId, platform, reason, DateTime.UtcNow));
        return request;
    }

    public override string ToString() => $"Return #{PlatformReturnId ?? Id.ToString()[..8]} ({Status}) - {Platform}";
}
