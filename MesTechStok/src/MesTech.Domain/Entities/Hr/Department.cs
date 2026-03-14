using MesTech.Domain.Common;

namespace MesTech.Domain.Entities.Hr;

public class Department : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; private set; } = string.Empty;
    public Guid? ManagerEmployeeId { get; private set; }
    public Guid? ParentDepartmentId { get; private set; }

    private Department() { }

    public static Department Create(Guid tenantId, string name, Guid? parentDepartmentId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Department
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            ParentDepartmentId = parentDepartmentId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetManager(Guid employeeId) { ManagerEmployeeId = employeeId; UpdatedAt = DateTime.UtcNow; }
    public void Rename(string name) { ArgumentException.ThrowIfNullOrWhiteSpace(name); Name = name; UpdatedAt = DateTime.UtcNow; }
}
