using MesTech.Domain.Common;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;

namespace MesTech.Domain.Entities;

/// <summary>
/// Sipariş Aggregate Root.
/// </summary>
public class Order : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string Type { get; set; } = "SALE";
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? RequiredDate { get; set; }

    // Tutarlar
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TaxRate { get; set; }

    // Durum
    public string PaymentStatus { get; set; } = "Pending";
    public string? Notes { get; set; }

    // Müşteri snapshot
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }

    // Concurrency
    public byte[]? RowVersion { get; set; }

    // Navigation
    private readonly List<OrderItem> _orderItems = new();
    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();

    private readonly List<StockMovement> _stockMovements = new();
    public IReadOnlyCollection<StockMovement> StockMovements => _stockMovements.AsReadOnly();

    // ── Domain Logic ──

    public void AddItem(OrderItem item)
    {
        _orderItems.Add(item);
        CalculateTotals();
    }

    public void CalculateTotals()
    {
        SubTotal = _orderItems.Sum(i => i.TotalPrice);
        TaxAmount = _orderItems.Sum(i => i.TaxAmount);
        TotalAmount = SubTotal + TaxAmount;
    }

    public void Place()
    {
        Status = OrderStatus.Confirmed;
        RaiseDomainEvent(new OrderPlacedEvent(Id, OrderNumber, CustomerId, TotalAmount, DateTime.UtcNow));
    }

    public int TotalItems => _orderItems.Sum(i => i.Quantity);

    public override string ToString() => $"Order #{OrderNumber} ({Status}) - {TotalAmount:C}";
}
