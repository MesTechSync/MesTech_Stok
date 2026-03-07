using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

public class RolePermission : BaseEntity
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    public DateTime GrantedDate { get; set; } = DateTime.UtcNow;
    public Guid? GrantedByUserId { get; set; }
}
