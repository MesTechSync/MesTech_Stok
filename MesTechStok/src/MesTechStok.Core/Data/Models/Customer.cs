using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MesTechStok.Core.Data.Models
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(20)]
        public string CustomerType { get; set; } = "INDIVIDUAL"; // INDIVIDUAL, CORPORATE

        // Contact Information
        [MaxLength(100)]
        public string? ContactPerson { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(20)]
        public string? Mobile { get; set; }

        // Address Information
        [MaxLength(500)]
        public string? BillingAddress { get; set; }

        [MaxLength(500)]
        public string? ShippingAddress { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? State { get; set; }

        [MaxLength(20)]
        public string? PostalCode { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; } = "Türkiye";

        // Business Information (for Corporate customers)
        [MaxLength(20)]
        public string? TaxNumber { get; set; }

        [MaxLength(50)]
        public string? TaxOffice { get; set; }

        [MaxLength(20)]
        public string? VatNumber { get; set; }

        [MaxLength(11)]
        public string? IdentityNumber { get; set; } // TC No for individuals

        // Financial Information
        [Column(TypeName = "decimal(18,2)")]
        public decimal? CreditLimit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentBalance { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal? DiscountRate { get; set; }

        public int PaymentTermDays { get; set; } = 0; // 0 = Cash

        [MaxLength(3)]
        public string Currency { get; set; } = "TRY";

        // Customer Segmentation
        [MaxLength(20)]
        public string? Segment { get; set; } // VIP, REGULAR, NEW, etc.

        [Range(1, 5)]
        public int? Rating { get; set; }

        public bool IsVip { get; set; } = false;

        // Status
        public bool IsActive { get; set; } = true;
        public bool IsBlocked { get; set; } = false;

        [MaxLength(200)]
        public string? BlockReason { get; set; }

        // Dates
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }
        public DateTime? LastOrderDate { get; set; }
        public DateTime? BirthDate { get; set; }

        // User Tracking
        [MaxLength(50)]
        public string? CreatedBy { get; set; }

        [MaxLength(50)]
        public string? ModifiedBy { get; set; }

        // Preferences
        [MaxLength(10)]
        public string? PreferredLanguage { get; set; } = "tr-TR";

        [MaxLength(50)]
        public string? PreferredContactMethod { get; set; } = "Email";

        public bool AcceptsMarketing { get; set; } = true;

        // Social Media & Web
        [MaxLength(255)]
        public string? Website { get; set; }

        [MaxLength(100)]
        public string? FacebookProfile { get; set; }

        [MaxLength(100)]
        public string? InstagramProfile { get; set; }

        [MaxLength(100)]
        public string? LinkedInProfile { get; set; }

        // Notes & Documents
        [MaxLength(1000)]
        public string? Notes { get; set; }

        [MaxLength(500)]
        public string? DocumentUrls { get; set; } // JSON array

        // OpenCart Integration
        public int? OpenCartCustomerId { get; set; }
        public DateTime? LastSyncDate { get; set; }
        public bool SyncWithOpenCart { get; set; } = true;

        // Navigation Properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        // Calculated Properties
        [NotMapped]
        public string DisplayName => $"{Name} ({Code})";

        [NotMapped]
        public string FullName => CustomerType == "CORPORATE" ? $"{Name} ({ContactPerson})" : Name;

        [NotMapped]
        public int TotalOrders => Orders?.Count ?? 0;

        [NotMapped]
        public decimal TotalOrderValue => Orders?.Sum(o => o.TotalAmount) ?? 0;

        [NotMapped]
        public string CreditStatus
        {
            get
            {
                if (IsBlocked) return "Blokeli";
                if (!CreditLimit.HasValue) return "Limit Yok";
                if (CurrentBalance >= CreditLimit) return "Limit Aşıldı";
                if (CurrentBalance >= CreditLimit * 0.8m) return "Limite Yakın";
                return "Normal";
            }
        }

        [NotMapped]
        public string StatusText
        {
            get
            {
                if (IsBlocked) return "Blokeli";
                if (!IsActive) return "Pasif";
                if (IsVip) return "VIP";
                return "Aktif";
            }
        }

        [NotMapped]
        public string StatusColor
        {
            get
            {
                if (IsBlocked) return "#F44336";
                if (!IsActive) return "#9E9E9E";
                if (IsVip) return "#FF9800";
                return "#4CAF50";
            }
        }

        public override string ToString() => DisplayName;
    }
}
