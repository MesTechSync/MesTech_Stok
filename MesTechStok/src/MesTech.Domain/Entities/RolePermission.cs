using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

public class RolePermission : BaseEntity
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }
    public DateTime GrantedDate { get; set; } = DateTime.UtcNow;
    public int? GrantedByUserId { get; set; }
}
