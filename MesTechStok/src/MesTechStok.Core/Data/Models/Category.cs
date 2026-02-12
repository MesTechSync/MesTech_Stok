using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MesTechStok.Core.Data.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        // Hierarchy Support
        public int? ParentCategoryId { get; set; }
        public virtual Category? ParentCategory { get; set; }
        public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();

        // Display & Organization
        [MaxLength(255)]
        public string? ImageUrl { get; set; }

        [MaxLength(7)]
        public string? Color { get; set; } = "#2196F3";

        [MaxLength(50)]
        public string? Icon { get; set; }

        public int SortOrder { get; set; } = 0;

        // Status
        public bool IsActive { get; set; } = true;
        public bool ShowInMenu { get; set; } = true;

        // Dates
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }

        // User Tracking
        [MaxLength(50)]
        public string? CreatedBy { get; set; }

        [MaxLength(50)]
        public string? ModifiedBy { get; set; }

        // OpenCart Integration
        public int? OpenCartCategoryId { get; set; }
        public DateTime? LastSyncDate { get; set; }
        public bool SyncWithOpenCart { get; set; } = true;

        // Navigation Properties
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();

        // Calculated Properties
        [NotMapped]
        public int ProductCount => Products?.Count(p => p.IsActive) ?? 0;

        [NotMapped]
        public string FullPath
        {
            get
            {
                var path = Name;
                var parent = ParentCategory;
                while (parent != null)
                {
                    path = $"{parent.Name} > {path}";
                    parent = parent.ParentCategory;
                }
                return path;
            }
        }

        [NotMapped]
        public int Level
        {
            get
            {
                int level = 0;
                var parent = ParentCategory;
                while (parent != null)
                {
                    level++;
                    parent = parent.ParentCategory;
                }
                return level;
            }
        }

        [NotMapped]
        public bool HasSubCategories => SubCategories?.Any() == true;

        public override string ToString() => Name;
    }
}
