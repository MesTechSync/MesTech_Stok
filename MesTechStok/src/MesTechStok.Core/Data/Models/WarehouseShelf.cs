using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MesTechStok.Core.Data.Models
{
    /// <summary>
    /// Raf seviyesi (Shelf) - Raf içi seviye sistemi
    /// </summary>
    [Table("WarehouseShelves")]
    public class WarehouseShelf
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        public int RackId { get; set; }

        // Fiziksel Özellikler
        [Required]
        public int LevelNumber { get; set; }

        [Column(TypeName = "decimal(8,2)")]
        public decimal? Height { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? MaxWeight { get; set; }

        // Konum Bilgileri
        [Column(TypeName = "decimal(8,2)")]
        public decimal? DistanceFromGround { get; set; }

        [StringLength(20)]
        public string? Accessibility { get; set; }

        // Özellikler
        public bool IsActive { get; set; } = true;
        public bool IsAccessible { get; set; } = true;

        // Audit Fields
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        // Navigation Properties (sonra eklenecek)
        // public virtual WarehouseRack? Rack { get; set; }
        // public virtual ICollection<WarehouseBin> Bins { get; set; } = new List<WarehouseBin>();
    }
}
