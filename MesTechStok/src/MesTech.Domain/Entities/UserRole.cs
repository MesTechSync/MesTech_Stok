using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

public class UserRole : BaseEntity
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
    public int? AssignedByUserId { get; set; }
}
