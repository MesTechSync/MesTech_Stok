using System;
using System.ComponentModel.DataAnnotations;

namespace MesTechStok.Desktop.Models
{
    public class UserRole
    {
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;

        public int RoleId { get; set; }
        public virtual Role Role { get; set; } = null!;

        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? AssignedBy { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public bool IsActive { get; set; } = true;

        // Composite Primary Key
        public override bool Equals(object? obj)
        {
            if (obj is UserRole other)
                return UserId == other.UserId && RoleId == other.RoleId;
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(UserId, RoleId);
        }
    }
}
