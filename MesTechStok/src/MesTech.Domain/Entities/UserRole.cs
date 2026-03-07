using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

public class UserRole : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
    public Guid? AssignedByUserId { get; set; }
}
