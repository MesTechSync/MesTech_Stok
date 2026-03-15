using FluentAssertions;
using MesTech.Domain.Entities.Hr;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain.Hr;

/// <summary>
/// Employee entity domain logic tests — H28 DEV5 T5.1
/// </summary>
public class EmployeeTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _userId = Guid.NewGuid();

    private static Employee CreateEmployee(string code = "EMP-001")
        => Employee.Create(_tenantId, _userId, code, DateTime.Today.AddMonths(-6));

    [Fact]
    public void Create_ValidData_ShouldSetStatusToActive()
    {
        var e = CreateEmployee();
        e.Status.Should().Be(EmployeeStatus.Active);
        e.EmployeeCode.Should().Be("EMP-001");
    }

    [Fact]
    public void Create_EmptyCode_ShouldThrow()
    {
        var act = () => Employee.Create(_tenantId, _userId, "", DateTime.Today);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WhitespaceCode_ShouldThrow()
    {
        var act = () => Employee.Create(_tenantId, _userId, "   ", DateTime.Today);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_SetsHireDate()
    {
        var hireDate = DateTime.Today.AddMonths(-3);
        var e = Employee.Create(_tenantId, _userId, "EMP-002", hireDate);
        e.HireDate.Should().Be(hireDate);
    }

    [Fact]
    public void Create_WithDepartmentId_ShouldSetDepartmentId()
    {
        var deptId = Guid.NewGuid();
        var e = Employee.Create(_tenantId, _userId, "EMP-003", DateTime.Today.AddMonths(-1),
            departmentId: deptId);
        e.DepartmentId.Should().Be(deptId);
    }

    [Fact]
    public void Create_WithoutDepartment_DepartmentIdShouldBeNull()
    {
        var e = CreateEmployee();
        e.DepartmentId.Should().BeNull();
    }

    [Fact]
    public void Terminate_ActiveEmployee_ShouldSetStatusToTerminated()
    {
        var e = CreateEmployee();
        e.Terminate(DateTime.Today);
        e.Status.Should().Be(EmployeeStatus.Terminated);
        e.TerminationDate.Should().Be(DateTime.Today);
    }

    [Fact]
    public void Terminate_SetsTerminationDate()
    {
        var e = CreateEmployee();
        var terminationDate = DateTime.Today.AddDays(-1);
        e.Terminate(terminationDate);
        e.TerminationDate.Should().Be(terminationDate);
    }

    [Fact]
    public void Create_WithJobTitle_ShouldSetJobTitle()
    {
        var e = Employee.Create(_tenantId, _userId, "EMP-005", DateTime.Today.AddYears(-1),
            jobTitle: "Senior Developer");
        e.JobTitle.Should().Be("Senior Developer");
    }

    [Fact]
    public void Create_WithWorkEmail_ShouldSetWorkEmail()
    {
        var e = Employee.Create(_tenantId, _userId, "EMP-006", DateTime.Today.AddYears(-2),
            workEmail: "emp@mestech.com");
        e.WorkEmail.Should().Be("emp@mestech.com");
    }
}
