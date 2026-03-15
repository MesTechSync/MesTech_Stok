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
    public void Create_FutureHireDate_ShouldThrow()
    {
        var act = () => Employee.Create(_tenantId, _userId, "EMP-X", DateTime.Today.AddDays(10));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PutOnLeave_ActiveEmployee_ShouldSetStatusToOnLeave()
    {
        var e = CreateEmployee();
        e.PutOnLeave();
        e.Status.Should().Be(EmployeeStatus.OnLeave);
    }

    [Fact]
    public void PutOnLeave_AlreadyOnLeave_ShouldThrow()
    {
        var e = CreateEmployee();
        e.PutOnLeave();
        var act = () => e.PutOnLeave();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ReturnFromLeave_ShouldSetStatusToActive()
    {
        var e = CreateEmployee();
        e.PutOnLeave();
        e.ReturnFromLeave();
        e.Status.Should().Be(EmployeeStatus.Active);
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
    public void Terminate_AlreadyTerminated_ShouldThrow()
    {
        var e = CreateEmployee();
        e.Terminate(DateTime.Today);
        var act = () => e.Terminate(DateTime.Today);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UpdateSalary_NegativeValue_ShouldThrow()
    {
        var e = CreateEmployee();
        var act = () => e.UpdateSalary(-1000m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateSalary_ValidValue_ShouldUpdate()
    {
        var e = CreateEmployee();
        e.UpdateSalary(25000m);
        e.MonthlySalary.Should().Be(25000m);
    }
}
