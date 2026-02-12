using System;
using System.ComponentModel.DataAnnotations;

namespace MesTechStok.Desktop.Models
{
    public class Session
    {
        [Key]
        [StringLength(255)]
        public string SessionId { get; set; } = string.Empty;

        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(45)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        [StringLength(100)]
        public string? DeviceInfo { get; set; }

        public DateTime? LastActivity { get; set; }

        // Computed Properties
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;

        public bool IsValid => IsActive && !IsExpired;

        public TimeSpan TimeUntilExpiry => ExpiresAt - DateTime.UtcNow;
    }
}
