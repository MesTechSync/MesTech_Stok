using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace MesTechStok.Core.Data.Models;

/// <summary>
/// Sipariş durumları için enum
/// </summary>
public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled
}

/// <summary>
/// Sipariş bilgilerini tutan model
/// OpenCart entegrasyonu ile senkronize edilir
/// </summary>
public class Order
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string OrderNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public virtual Customer Customer { get; set; } = null!;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [MaxLength(20)]
    public string Type { get; set; } = "SALE";

    public DateTime OrderDate { get; set; } = DateTime.Now;
    public DateTime? RequiredDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SubTotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal TaxRate { get; set; } = 0.18m;

    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "PENDING";

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public DateTime? LastModifiedAt { get; set; }

    [MaxLength(50)]
    public string? CreatedBy { get; set; }

    // Customer Information (for OpenCart integration)
    [MaxLength(100)]
    public string? CustomerName { get; set; }

    [MaxLength(100)]
    public string? CustomerEmail { get; set; }

    public int? OpenCartOrderId { get; set; }

    // Navigation Properties
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    // Calculated Properties
    [NotMapped]
    public int TotalItems => OrderItems?.Sum(oi => oi.Quantity) ?? 0;

    [NotMapped]
    public string StatusDescription
    {
        get
        {
            return Status switch
            {
                OrderStatus.Pending => "Beklemede",
                OrderStatus.Confirmed => "Onaylandı",
                OrderStatus.Shipped => "Kargoya Verildi",
                OrderStatus.Delivered => "Teslim Edildi",
                OrderStatus.Cancelled => "İptal Edildi",
                _ => "Bilinmeyen"
            };
        }
    }

    public void CalculateTotals()
    {
        if (OrderItems?.Any() == true)
        {
            SubTotal = OrderItems.Sum(oi => oi.TotalPrice);
            TaxAmount = SubTotal * (TaxRate / 100);
            TotalAmount = SubTotal + TaxAmount;
        }
    }

    public override string ToString() => $"{OrderNumber} - {Customer?.Name} ({StatusDescription})";
}


