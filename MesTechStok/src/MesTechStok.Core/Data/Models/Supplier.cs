using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MesTechStok.Core.Data.Models
{
    public class Supplier
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        // Contact Information
        [MaxLength(100)]
        public string? ContactPerson { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(20)]
        public string? Mobile { get; set; }

        [MaxLength(20)]
        public string? Fax { get; set; }

        [MaxLength(255)]
        public string? Website { get; set; }

        // Address Information
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

        // Business Information
        [MaxLength(20)]
        public string? TaxNumber { get; set; }

        [MaxLength(50)]
        public string? TaxOffice { get; set; }

        [MaxLength(20)]
        public string? VatNumber { get; set; }

        [MaxLength(50)]
        public string? TradeRegisterNumber { get; set; }

        // Financial Terms
        public int PaymentTermDays { get; set; } = 30;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? CreditLimit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentBalance { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal? DiscountRate { get; set; }

        [MaxLength(3)]
        public string Currency { get; set; } = "TRY";

        // Status & Ratings
        public bool IsActive { get; set; } = true;
        public bool IsPreferred { get; set; } = false;

        [Range(1, 5)]
        public int? Rating { get; set; }

        // Dates
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }
        public DateTime? LastOrderDate { get; set; }

        // User Tracking
        [MaxLength(50)]
        public string? CreatedBy { get; set; }

        [MaxLength(50)]
        public string? ModifiedBy { get; set; }

        // Notes
        [MaxLength(1000)]
        public string? Notes { get; set; }

        // Documents
        [MaxLength(500)]
        public string? DocumentUrls { get; set; } // JSON array

        // Navigation Properties
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

        // Calculated Properties
        [NotMapped]
        public int ProductCount => Products?.Count ?? 0;

        [NotMapped]
        public string DisplayName => $"{Name} ({Code})";

        [NotMapped]
        public string FullAddress => $"{Address}, {City}, {PostalCode} {Country}".Trim();

        [NotMapped]
        public bool HasOverduePayments => CurrentBalance < 0;

        [NotMapped]
        public string CreditStatus
        {
            get
            {
                if (!CreditLimit.HasValue) return "Limit Yok";
                if (CurrentBalance >= CreditLimit) return "Limit Aşıldı";
                if (CurrentBalance >= CreditLimit * 0.8m) return "Limite Yakın";
                return "Normal";
            }
        }

        public override string ToString() => DisplayName;
    }
}
