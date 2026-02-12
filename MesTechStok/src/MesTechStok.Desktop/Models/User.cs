using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MesTechStok.Desktop.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(100)]
        public string? FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsLocked { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        public int LoginAttempts { get; set; } = 0;

        public DateTime? LockedUntil { get; set; }

        // Navigation Properties
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        // Computed Properties
        public string FullName => $"{FirstName} {LastName}".Trim();

        public bool IsLockedOut => IsLocked && LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;
    }
}
