using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities.Hr;

public sealed class Employee : BaseEntity, ITenantEntity
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

    public static Employee Create(Guid tenantId, Guid userId, string employeeCode,
        DateTime hireDate, Guid? departmentId = null,
        string? jobTitle = null, decimal? monthlySalary = null)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId boş olamaz.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeCode);
        if (hireDate > DateTime.UtcNow.AddDays(1))
            throw new ArgumentException("Hire date cannot be in the future.", nameof(hireDate));
        return new Employee
        {
            Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId,
            EmployeeCode = employeeCode, HireDate = hireDate, DepartmentId = departmentId,
            JobTitle = jobTitle, MonthlySalary = monthlySalary,
            Status = EmployeeStatus.Active, CreatedAt = DateTime.UtcNow
        };
    }

    public void AssignToDepartment(Guid departmentId) { DepartmentId = departmentId; UpdatedAt = DateTime.UtcNow; }

    public void Terminate(DateTime terminationDate)
    {
        if (Status == EmployeeStatus.Terminated)
            throw new InvalidOperationException("Employee is already terminated.");
        Status = EmployeeStatus.Terminated; TerminationDate = terminationDate; UpdatedAt = DateTime.UtcNow;
    }

    public void PutOnLeave()
    {
        if (Status != EmployeeStatus.Active)
            throw new InvalidOperationException("Only active employees can go on leave.");
        Status = EmployeeStatus.OnLeave; UpdatedAt = DateTime.UtcNow;
    }

    public void ReturnFromLeave()
    {
        if (Status != EmployeeStatus.OnLeave)
            throw new InvalidOperationException("Employee is not on leave.");
        Status = EmployeeStatus.Active; UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSalary(decimal monthlySalary)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(monthlySalary);
        MonthlySalary = monthlySalary; UpdatedAt = DateTime.UtcNow;
    }
}
