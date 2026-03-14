using MesTech.Domain.Common;

namespace MesTech.Domain.Entities.Tasks;

public class ProjectMember : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public string Role { get; private set; } = "Member"; // Owner, Manager, Member, Viewer

    public Project Project { get; private set; } = null!;

    private ProjectMember() { }

    public static ProjectMember Create(Guid projectId, Guid userId, string role = "Member")
        => new() { Id = Guid.NewGuid(), ProjectId = projectId, UserId = userId, Role = role, CreatedAt = DateTime.UtcNow };
}
