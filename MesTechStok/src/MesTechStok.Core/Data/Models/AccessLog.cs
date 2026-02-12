using System;
using System.ComponentModel.DataAnnotations;

namespace MesTechStok.Core.Data.Models
{
    public class AccessLog
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Resource { get; set; } = string.Empty;

        public bool IsAllowed { get; set; }

        public DateTime AccessTime { get; set; } = DateTime.UtcNow;

        [StringLength(45)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        [StringLength(1000)]
        public string? AdditionalInfo { get; set; }

        [StringLength(64)]
        public string? CorrelationId { get; set; }

        // Navigation Properties - UserNavigation removed to avoid EF Core conflict
        // Already have User property above
    }
}
