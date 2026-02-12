using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MesTechStok.Core.Data.Models;

public enum LotStatus : byte
{
    Open = 0,
    Closed = 1,
    Expired = 2
}

/// <summary>
/// Parti/Lot takibi için temel model. FEFO tahsis ve son kullanım kontrolü için kullanılır.
/// </summary>
public class InventoryLot
{
    [Key]
    public int Id { get; set; }

    public int ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string LotNumber { get; set; } = string.Empty;

    public DateTime? ExpiryDate { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ReceivedQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal RemainingQty { get; set; }

    public LotStatus Status { get; set; } = LotStatus.Open;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedDate { get; set; }
}


