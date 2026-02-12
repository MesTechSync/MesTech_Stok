using System;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MesTechStok.Core.Data.Models
{
    public class Warehouse
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Type { get; set; } = "MAIN"; // MAIN, BRANCH, VIRTUAL, CONSIGNMENT

        // Location Information
        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? State { get; set; }

        [MaxLength(20)]
        public string? PostalCode { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; } = "Türkiye";

        // Contact Information
        [MaxLength(100)]
        public string? ContactPerson { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        // Physical Properties
        [Column(TypeName = "decimal(10,2)")]
        public decimal? TotalArea { get; set; } // m²

        [Column(TypeName = "decimal(10,2)")]
        public decimal? UsableArea { get; set; } // m²

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Height { get; set; } // m

        [Column(TypeName = "decimal(15,3)")]
        public decimal? MaxCapacity { get; set; } // m³ or units

        [MaxLength(20)]
        public string? CapacityUnit { get; set; } = "m³";

        // Climate Control
        [Column(TypeName = "decimal(5,2)")]
        public decimal? MinTemperature { get; set; } // °C

        [Column(TypeName = "decimal(5,2)")]
        public decimal? MaxTemperature { get; set; } // °C

        [Column(TypeName = "decimal(5,2)")]
        public decimal? MinHumidity { get; set; } // %

        [Column(TypeName = "decimal(5,2)")]
        public decimal? MaxHumidity { get; set; } // %

        public bool HasClimateControl { get; set; } = false;

        // Features
        public bool HasSecuritySystem { get; set; } = false;
        public bool HasFireProtection { get; set; } = false;
        public bool HasLoadingDock { get; set; } = false;
        public bool HasRacking { get; set; } = false;
        public bool HasForklift { get; set; } = false;

        // Operating Hours
        [MaxLength(100)]
        public string? OperatingHours { get; set; }

        public bool Is24Hours { get; set; } = false;

        // Cost Center
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MonthlyCost { get; set; }

        [Column(TypeName = "decimal(10,4)")]
        public decimal? CostPerSquareMeter { get; set; }

        [MaxLength(20)]
        public string? CostCenter { get; set; }

        // Status
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;

        // Dates
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }

        // User Tracking
        [MaxLength(50)]
        public string? CreatedBy { get; set; }

        [MaxLength(50)]
        public string? ModifiedBy { get; set; }

        // Notes
        [MaxLength(1000)]
        public string? Notes { get; set; }

        // Navigation Properties
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        public virtual ICollection<StockMovement> FromMovements { get; set; } = new List<StockMovement>();
        public virtual ICollection<StockMovement> ToMovements { get; set; } = new List<StockMovement>();
        // TEMPORARILY DISABLED: public virtual ICollection<WarehouseZone> WarehouseZones { get; set; } = new List<WarehouseZone>();

        // Calculated Properties
        [NotMapped]
        public string DisplayName => $"{Name} ({Code})";

        [NotMapped]
        public string FullAddress => $"{Address}, {City}, {PostalCode} {Country}".Trim();

        [NotMapped]
        public int ProductCount => Products?.Count(p => p.IsActive) ?? 0;

        [NotMapped]
        public decimal TotalStockValue => Products?.Sum(p => p.TotalValue) ?? 0;

        [NotMapped]
        public decimal CurrentCapacityUsage
        {
            get
            {
                if (!MaxCapacity.HasValue || MaxCapacity == 0) return 0;
                // This would need actual calculation based on product volumes
                return 0; // Placeholder
            }
        }

        [NotMapped]
        public string CapacityStatus
        {
            get
            {
                var usage = CurrentCapacityUsage;
                if (usage >= 95) return "Dolu";
                if (usage >= 80) return "Neredeyse Dolu";
                if (usage >= 60) return "Orta";
                return "Boş";
            }
        }

        [NotMapped]
        public string TypeDescription
        {
            get
            {
                return Type?.ToUpper() switch
                {
                    "MAIN" => "Ana Depo",
                    "BRANCH" => "Şube Deposu",
                    "VIRTUAL" => "Sanal Depo",
                    "CONSIGNMENT" => "Konsinye Depo",
                    "EXTERNAL" => "Harici Depo",
                    "TEMPORARY" => "Geçici Depo",
                    _ => Type ?? "Bilinmeyen"
                };
            }
        }

        public override string ToString() => DisplayName;
    }
}
