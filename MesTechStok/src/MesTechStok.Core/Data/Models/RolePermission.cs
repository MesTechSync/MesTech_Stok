namespace MesTechStok.Core.Data.Models;

/// <summary>
/// Rol-İzin ilişki tablosu
/// </summary>
public class RolePermission
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public int PermissionId { get; set; }
    public DateTime GrantedDate { get; set; } = DateTime.Now;
    public int? GrantedByUserId { get; set; }

    // Navigation Properties
    public virtual Role Role { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;
    public virtual User? GrantedByUser { get; set; }
}