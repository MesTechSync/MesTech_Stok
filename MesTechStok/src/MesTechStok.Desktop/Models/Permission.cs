using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MesTechStok.Desktop.Models
{
    public class Permission
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Module { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

        // Predefined Permission Codes
        public static class PredefinedPermissions
        {
            // Product Management
            public const string PRODUCT_CREATE = "PRODUCT_CREATE";
            public const string PRODUCT_READ = "PRODUCT_READ";
            public const string PRODUCT_UPDATE = "PRODUCT_UPDATE";
            public const string PRODUCT_DELETE = "PRODUCT_DELETE";

            // Inventory Management
            public const string INVENTORY_READ = "INVENTORY_READ";
            public const string INVENTORY_UPDATE = "INVENTORY_UPDATE";
            public const string INVENTORY_ADJUST = "INVENTORY_ADJUST";

            // Order Management
            public const string ORDER_CREATE = "ORDER_CREATE";
            public const string ORDER_READ = "ORDER_READ";
            public const string ORDER_UPDATE = "ORDER_UPDATE";
            public const string ORDER_DELETE = "ORDER_DELETE";

            // Reports
            public const string REPORTS_READ = "REPORTS_READ";
            public const string REPORTS_EXPORT = "REPORTS_EXPORT";

            // User Management
            public const string USER_CREATE = "USER_CREATE";
            public const string USER_READ = "USER_READ";
            public const string USER_UPDATE = "USER_UPDATE";
            public const string USER_DELETE = "USER_DELETE";

            // System Settings
            public const string SETTINGS_READ = "SETTINGS_READ";
            public const string SETTINGS_UPDATE = "SETTINGS_UPDATE";
        }
    }
}
