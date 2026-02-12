using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MesTechStok.Core.Data.Models
{
    /// <summary>
    /// Depo bölümü (Zone) - Depo içi ana bölümler
    /// </summary>
    [Table("WarehouseZones")]
    public class WarehouseZone
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string Code { get; set; } = string.Empty;

        [Required]
        public int WarehouseId { get; set; }

        // Fiziksel Özellikler
        [Column(TypeName = "decimal(10,2)")]
        public decimal? Width { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Length { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Height { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Area { get; set; }

        // Konum Bilgileri
        public int? FloorNumber { get; set; }

        [StringLength(50)]
        public string? BuildingSection { get; set; }

        // Özellikler
        public bool HasClimateControl { get; set; } = false;
        public bool HasSecurity { get; set; } = false;

        [StringLength(50)]
        public string? TemperatureRange { get; set; }

        [StringLength(50)]
        public string? HumidityRange { get; set; }

        // Organizasyon
        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        // Audit Fields
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        // Navigation Properties (sonra eklenecek)
        // public virtual Warehouse? Warehouse { get; set; }
        // public virtual ICollection<WarehouseRack> Racks { get; set; } = new List<WarehouseRack>();
    }
}
