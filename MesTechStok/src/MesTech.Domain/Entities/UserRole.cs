using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

public sealed class UserRole : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
    public Guid? AssignedByUserId { get; set; }

    // Navigation — JWT role claim'leri için gerekli
    public Role? Role { get; set; }
}
