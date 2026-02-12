using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MesTechStok.Core.Data.Models
{
    /// <summary>
    /// Raf gözü (Bin) - Seviye içi göz sistemi
    /// </summary>
    [Table("WarehouseBins")]
    public class WarehouseBin
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
        public int ShelfId { get; set; }

        // Fiziksel Özellikler
        [Required]
        public int BinNumber { get; set; }

        [Column(TypeName = "decimal(8,2)")]
        public decimal? Width { get; set; }

        [Column(TypeName = "decimal(8,2)")]
        public decimal? Depth { get; set; }

        [Column(TypeName = "decimal(8,2)")]
        public decimal? Height { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal? Volume { get; set; }

        // Konum Bilgileri
        public int? XPosition { get; set; }
        public int? YPosition { get; set; }
        public int? ZPosition { get; set; }

        // Özellikler
        [StringLength(50)]
        public string? BinType { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? MaxWeight { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsReserved { get; set; } = false;
        public bool IsLocked { get; set; } = false;

        // Audit Fields
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        // Navigation Properties (sonra eklenecek)
        // public virtual WarehouseShelf? Shelf { get; set; }
        // public virtual ICollection<ProductLocation> ProductLocations { get; set; } = new List<ProductLocation>();
    }
}
