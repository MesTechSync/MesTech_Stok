using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MesTechStok.Core.Data.Models
{
    /// <summary>
    /// Ürün konumu (Product Location) - Ürünün depodaki konumu
    /// </summary>
    [Table("ProductLocations")]
    public class ProductLocation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int BinId { get; set; }

        // Konum Detayları
        [Required]
        public int Quantity { get; set; }

        [StringLength(50)]
        public string? Position { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        // Takip Bilgileri
        [Required]
        public DateTime PlacedDate { get; set; } = DateTime.Now;

        public DateTime? LastMovedDate { get; set; }

        [StringLength(100)]
        public string? PlacedBy { get; set; }

        [StringLength(100)]
        public string? LastMovedBy { get; set; }

        // Özellikler
        public bool IsPrimary { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public bool IsReserved { get; set; } = false;

        // Audit Fields
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        // Navigation Properties (sonra eklenecek)
        // public virtual Product? Product { get; set; }
        // public virtual WarehouseBin? Bin { get; set; }
        // public virtual ICollection<LocationMovement> Movements { get; set; } = new List<LocationMovement>();
    }
}
