namespace MesTechStok.Core.Data.Models;

/// <summary>
/// Kullanıcı-Rol ilişki tablosu
/// </summary>
public class UserRole
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public DateTime AssignedDate { get; set; } = DateTime.Now;
    public int? AssignedByUserId { get; set; }

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual Role Role { get; set; } = null!;
    public virtual User? AssignedByUser { get; set; }
}