using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MesTechStok.Core.Data.Models
{
    /// <summary>
    /// Depo rafı (Rack) - Bölüm içi raf sistemi
    /// </summary>
    [Table("WarehouseRacks")]
    public class WarehouseRack
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
        public int ZoneId { get; set; }

        // Fiziksel Özellikler
        [Column(TypeName = "decimal(8,2)")]
        public decimal? Width { get; set; }

        [Column(TypeName = "decimal(8,2)")]
        public decimal? Depth { get; set; }

        [Column(TypeName = "decimal(8,2)")]
        public decimal? Height { get; set; }

        public int ShelfCount { get; set; } = 1;
        public int BinCount { get; set; } = 1;

        // Konum Bilgileri
        public int? RowNumber { get; set; }
        public int? ColumnNumber { get; set; }

        [StringLength(20)]
        public string? Orientation { get; set; }

        // Özellikler
        [StringLength(50)]
        public string? RackType { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? MaxWeight { get; set; }

        public bool IsMovable { get; set; } = false;
        public bool IsActive { get; set; } = true;

        // Audit Fields
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        // Navigation Properties (sonra eklenecek)
        // public virtual WarehouseZone? Zone { get; set; }
        // public virtual ICollection<WarehouseShelf> Shelves { get; set; } = new List<WarehouseShelf>();
    }
}
