using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities.Hr;

public class Employee : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public string EmployeeCode { get; private set; } = string.Empty;
    public string? JobTitle { get; private set; }
    public DateTime HireDate { get; private set; }
    public DateTime? TerminationDate { get; private set; }
    public EmployeeStatus Status { get; private set; }
    public decimal? HourlyRate { get; private set; }
    public decimal? MonthlySalary { get; private set; }
    public string? WorkEmail { get; private set; }
    public string? WorkPhone { get; private set; }

    private Employee() { }

    public static Employee Create(
        Guid tenantId, Guid userId, string employeeCode, DateTime hireDate,
        Guid? departmentId = null, string? jobTitle = null,
        string? workEmail = null, string? workPhone = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeCode);
        return new Employee
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            EmployeeCode = employeeCode,
            HireDate = hireDate,
            DepartmentId = departmentId,
            JobTitle = jobTitle,
            WorkEmail = workEmail,
            WorkPhone = workPhone,
            Status = EmployeeStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Terminate(DateTime terminationDate)
    {
        Status = EmployeeStatus.Terminated;
        TerminationDate = terminationDate;
        UpdatedAt = DateTime.UtcNow;
    }
}
