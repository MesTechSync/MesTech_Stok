using System.ComponentModel.DataAnnotations;

namespace MesTechStok.Core.Data.Models;

/// <summary>
/// Rol varlığı - Kullanıcı yetkilendirmesi için
/// </summary>
public class Role
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsSystemRole { get; set; } = false;
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime? ModifiedDate { get; set; }

    // Navigation Properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}