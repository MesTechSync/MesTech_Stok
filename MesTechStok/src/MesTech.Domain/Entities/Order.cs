using MesTech.Domain.Common;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Domain.Exceptions;

namespace MesTech.Domain.Entities;

/// <summary>
/// Sipariş Aggregate Root.
/// </summary>
public sealed class Order : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public OrderStatus Status { get; internal set; } = OrderStatus.Pending;
    public string Type { get; set; } = "SALE";
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? RequiredDate { get; set; }

    // Tutarlar
    public decimal SubTotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal TaxRate { get; set; }

    // Durum
    public string PaymentStatus { get; internal set; } = "Pending";
    public string? Notes { get; set; }

    // Müşteri snapshot
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? RecipientPhone { get; set; }
    public string? ShippingAddress { get; set; }

    // Platform kaynagi
    public PlatformType? SourcePlatform { get; set; }
    public string? ExternalOrderId { get; set; }
    public string? PlatformOrderNumber { get; set; }

    // Onay
    public DateTime? ConfirmedAt { get; private set; }

    // Kargo bilgisi
    public CargoProvider? CargoProvider { get; private set; }
    public string? TrackingNumber { get; private set; }
    public string? CargoBarcode { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }

    // Otomatik gonderim
    public bool AutoShipmentEnabled { get; set; }
    public DateTime? AutoShipmentScheduledAt { get; private set; }

    // ── Muhasebe Modulu (MUH-01) ──
    public decimal? CommissionAmount { get; private set; }
    public decimal? CommissionRate { get; private set; }
    public decimal? CargoExpenseAmount { get; private set; }

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
        ArgumentNullException.ThrowIfNull(item);
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
        if (Status != OrderStatus.Pending)
            throw new BusinessRuleException("OrderStatusTransition",
                $"Cannot place order in {Status} status. Only Pending orders can be placed.");

        Status = OrderStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
        RaiseDomainEvent(new OrderPlacedEvent(Id, TenantId, OrderNumber, CustomerId, TotalAmount, DateTime.UtcNow));
    }

    /// <summary>
    /// Siparisi kargoya verir. Sadece Confirmed statusunden gecis yapilabilir.
    /// </summary>
    public void MarkAsShipped(string trackingNumber, CargoProvider provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);

        if (Status != OrderStatus.Confirmed)
            throw new BusinessRuleException("OrderStatusTransition",
                $"Cannot ship order in {Status} status. Only Confirmed orders can be shipped.");

        TrackingNumber = trackingNumber;
        CargoProvider = provider;
        ShippedAt = DateTime.UtcNow;
        Status = OrderStatus.Shipped;

        RaiseDomainEvent(new OrderShippedEvent(Id, TenantId, trackingNumber, provider, DateTime.UtcNow));
    }

    /// <summary>
    /// Siparisi iptal eder. Sadece Pending veya Confirmed statusunden iptal edilebilir.
    /// </summary>
    public void Cancel(string? reason = null)
    {
        if (Status is not (OrderStatus.Pending or OrderStatus.Confirmed))
            throw new BusinessRuleException("OrderStatusTransition",
                $"Cannot cancel order in {Status} status. Only Pending or Confirmed orders can be cancelled.");

        Status = OrderStatus.Cancelled;

        RaiseDomainEvent(new OrderCancelledEvent(
            Id,
            TenantId,
            SourcePlatform?.ToString() ?? "Internal",
            ExternalOrderId ?? OrderNumber,
            reason,
            DateTime.UtcNow));
    }

    /// <summary>
    /// Siparisi teslim alindi olarak isaretler. Sadece Shipped statusunden gecis yapilabilir.
    /// </summary>
    public void MarkAsDelivered()
    {
        if (Status != OrderStatus.Shipped)
            throw new BusinessRuleException("OrderStatusTransition",
                $"Cannot mark as delivered in {Status} status. Only Shipped orders can be delivered.");

        DeliveredAt = DateTime.UtcNow;
        Status = OrderStatus.Delivered;

        RaiseDomainEvent(new OrderReceivedEvent(
            Id,
            TenantId,
            SourcePlatform?.ToString() ?? "Internal",
            ExternalOrderId ?? OrderNumber,
            TotalAmount,
            DateTime.UtcNow));

        RaiseDomainEvent(new OrderCompletedEvent(
            Id,
            TenantId,
            OrderNumber,
            TotalAmount,
            CustomerName,
            DateTime.UtcNow));
    }

    public void SetFinancials(decimal subTotal, decimal taxAmount, decimal totalAmount)
    {
        if (subTotal < 0)
            throw new ArgumentOutOfRangeException(nameof(subTotal), "SubTotal cannot be negative.");
        if (taxAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(taxAmount), "TaxAmount cannot be negative.");
        if (totalAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(totalAmount), "TotalAmount cannot be negative.");
        SubTotal = subTotal;
        TaxAmount = taxAmount;
        TotalAmount = totalAmount;
    }

    public void MarkAsPaid()
    {
        PaymentStatus = "Paid";
        RaiseDomainEvent(new OrderPaidEvent(Id, TenantId, OrderNumber, TotalAmount, DateTime.UtcNow));
    }

    public void SetCommission(decimal? rate, decimal? amount)
    {
        if (rate.HasValue && rate.Value < 0)
            throw new ArgumentOutOfRangeException(nameof(rate), "Commission rate cannot be negative.");
        if (amount.HasValue && amount.Value < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Commission amount cannot be negative.");
        CommissionRate = rate;
        CommissionAmount = amount;
    }

    public void SetCargoExpense(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Cargo expense cannot be negative.");
        CargoExpenseAmount = amount;
    }

    public void SetCargoBarcode(string barcode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(barcode);
        CargoBarcode = barcode;
    }

    public void ScheduleAutoShipment(DateTime scheduledAt)
    {
        if (scheduledAt < DateTime.UtcNow.AddMinutes(-1))
            throw new ArgumentOutOfRangeException(nameof(scheduledAt), "Scheduled time cannot be in the past.");
        AutoShipmentScheduledAt = scheduledAt;
    }

    public int TotalItems => _orderItems.Sum(i => i.Quantity);

    // === Factory Methods ===

    /// <summary>
    /// Manuel sipariş girişi — stok etkisi yok, Pending statüsünde oluşturulur.
    /// </summary>
    public static Order CreateManual(
        Guid tenantId,
        Guid customerId,
        string customerName,
        string? customerEmail,
        string orderType,
        string? notes = null,
        DateTime? requiredDate = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerName);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}",
            CustomerId = customerId,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            Type = orderType,
            Notes = notes,
            OrderDate = DateTime.UtcNow,
            RequiredDate = requiredDate,
            Status = OrderStatus.Pending,
            PaymentStatus = "Pending"
        };

        // OrderPlacedEvent burada fırlatılMAZ — Pending = taslak.
        // Place() çağrıldığında Confirmed statüsüne geçer ve event fırlatır.
        // Aksi halde OrderPlacedStockDeductionHandler çift stok düşürür.
        // NOT: OrderReceivedEvent burada fırlatılMAZ — MarkAsDelivered()'da fırlatılır.
        // Pending taslakta event fırlatmak çift gelir kaydı oluşturuyordu (G10869).

        return order;
    }

    /// <summary>
    /// Platform siparis verisi ile Order olusturur.
    /// Adapter'dan gelen veriler icin kullanilir.
    /// </summary>
    public static Order CreateFromPlatform(
        Guid tenantId,
        string platformOrderId,
        PlatformType platform,
        string? customerName,
        string? customerEmail,
        IReadOnlyList<OrderItem> items)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platformOrderId);
        ArgumentNullException.ThrowIfNull(items);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrderNumber = $"{platform.ToString()[..2].ToUpperInvariant()}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpperInvariant()}",
            ExternalOrderId = platformOrderId,
            PlatformOrderNumber = platformOrderId,
            SourcePlatform = platform,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            PaymentStatus = "Pending",
            Type = "SALE"
        };

        foreach (var item in items)
            order.AddItem(item);

        return order;
    }

    public override string ToString() => $"Order #{OrderNumber} ({Status}) - {TotalAmount:C}";
}
