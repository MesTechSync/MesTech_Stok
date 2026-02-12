using System.ComponentModel.DataAnnotations;

namespace MesTechStok.Core.Data.Models;

/// <summary>
/// İzin varlığı - Detaylı yetkilendirme için
/// </summary>
public class Permission
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Module { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    // Navigation Properties
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}