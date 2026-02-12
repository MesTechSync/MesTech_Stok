using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MesTechStok.Desktop.Models
{
    public class Role
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public int Priority { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

        // Predefined Role Codes
        public static class PredefinedRoles
        {
            public const string ADMIN = "ADMIN";
            public const string WAREHOUSE_MANAGER = "WAREHOUSE_MANAGER";
            public const string WAREHOUSE_STAFF = "WAREHOUSE_STAFF";
            public const string SALES_STAFF = "SALES_STAFF";
            public const string VIEWER = "VIEWER";
        }
    }
}
