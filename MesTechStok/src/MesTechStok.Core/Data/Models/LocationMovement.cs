using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MesTechStok.Core.Data.Models
{
    /// <summary>
    /// Konum hareketi (Location Movement) - Ürün konum değişiklikleri
    /// Tüm konum değişikliklerinin audit trail'i
    /// </summary>
    [Table("LocationMovements")]
    public class LocationMovement
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        // Hareket Detayları
        public int? FromBinId { get; set; }

        // TEMPORARILY DISABLED: [ForeignKey("FromBinId")]
        // public virtual WarehouseBin? FromBin { get; set; }

        public int? ToBinId { get; set; }

        // TEMPORARILY DISABLED: [ForeignKey("ToBinId")]
        // public virtual WarehouseBin? ToBin { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [StringLength(50)]
        public string MovementType { get; set; } = string.Empty; // "PLACE", "MOVE", "REMOVE", "ADJUST", "TRANSFER"

        [StringLength(200)]
        public string? Reason { get; set; } // "Sipariş", "Sayım", "Reorganizasyon", "Hasar"

        [StringLength(1000)]
        public string? Notes { get; set; } // Detaylı açıklama

        // Takip Bilgileri
        [Required]
        public DateTime MovementDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? MovedBy { get; set; }

        [StringLength(100)]
        public string? Reference { get; set; } // Sipariş no, sayım no, transfer no

        // Hareket Detayları
        [StringLength(50)]
        public string? MovementStatus { get; set; } // "PENDING", "IN_PROGRESS", "COMPLETED", "CANCELLED"

        public DateTime? CompletedAt { get; set; }

        [StringLength(100)]
        public string? CompletedBy { get; set; }

        // Audit Fields
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        // Computed Properties
        [NotMapped]
        public string MovementSummary => $"{Product?.Name ?? "N/A"} - {MovementType} - {Quantity} adet";

        [NotMapped]
        public string MovementPath =>
            // TEMPORARILY DISABLED: FromBin != null && ToBin != null 
            //     ? $"{FromBin.Code} → {ToBin.Code}" 
            //     : (FromBin != null ? $"{FromBin.Code} → Çıkış" : $"Giriş → {ToBin?.Code ?? "N/A"}");
            $"Bin {FromBinId} → Bin {ToBinId}"; // Temporary simplified version

        [NotMapped]
        public string StatusInfo => MovementStatus switch
        {
            "PENDING" => "Bekliyor",
            "IN_PROGRESS" => "Devam Ediyor",
            "COMPLETED" => "Tamamlandı",
            "CANCELLED" => "İptal Edildi",
            _ => "Bilinmeyen"
        };

        [NotMapped]
        public bool IsCompleted => MovementStatus == "COMPLETED";

        [NotMapped]
        public bool IsPending => MovementStatus == "PENDING";
    }
}
