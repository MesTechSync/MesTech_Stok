using System;
using System.ComponentModel.DataAnnotations;

namespace MesTechStok.Desktop.Models
{
    public class RolePermission
    {
        public int RoleId { get; set; }
        public virtual Role Role { get; set; } = null!;

        public int PermissionId { get; set; }
        public virtual Permission Permission { get; set; } = null!;

        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? AssignedBy { get; set; }

        public bool IsActive { get; set; } = true;

        // Composite Primary Key
        public override bool Equals(object? obj)
        {
            if (obj is RolePermission other)
                return RoleId == other.RoleId && PermissionId == other.PermissionId;
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(RoleId, PermissionId);
        }
    }
}
