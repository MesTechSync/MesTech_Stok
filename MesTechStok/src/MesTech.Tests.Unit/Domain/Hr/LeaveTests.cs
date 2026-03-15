using FluentAssertions;
using MesTech.Domain.Entities.Hr;
using MesTech.Domain.Enums;
using MesTech.Domain.Events.Hr;

namespace MesTech.Tests.Unit.Domain.Hr;

/// <summary>
/// Leave entity domain logic tests — H28 DEV5 T5.2
/// </summary>
public class LeaveTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _employeeId = Guid.NewGuid();
    private static readonly Guid _approverUserId = Guid.NewGuid();

    private static Leave CreateLeave(int daysFromNow = 7, int lengthDays = 5)
    {
        var start = DateTime.Today.AddDays(daysFromNow);
        var end = start.AddDays(lengthDays - 1);
        return Leave.Create(_tenantId, _employeeId, LeaveType.Annual, start, end, "Tatil");
    }

    [Fact]
    public void Create_ValidDates_ShouldSetStatusToPending()
    {
        var leave = CreateLeave();
        leave.Status.Should().Be(LeaveStatus.Pending);
        leave.TotalDays.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Create_EndBeforeStart_ShouldThrow()
    {
        var start = DateTime.Today.AddDays(10);
        var end = DateTime.Today.AddDays(5);
        var act = () => Leave.Create(_tenantId, _employeeId, LeaveType.Annual, start, end);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_SickLeave_ShouldCalculateDays()
    {
        var monday = DateTime.Today.AddDays((1 - (int)DateTime.Today.DayOfWeek + 14) % 7 + 7);
        var leave = Leave.Create(_tenantId, _employeeId, LeaveType.Sick, monday, monday.AddDays(4), "Hastalık");
        leave.TotalDays.Should().Be(5);
    }

    [Fact]
    public void Create_SameDayLeave_ShouldHaveOneTotalDay()
    {
        var date = DateTime.Today.AddDays(7);
        var leave = Leave.Create(_tenantId, _employeeId, LeaveType.Sick, date, date, "Doktor");
        leave.TotalDays.Should().Be(1);
    }

    [Fact]
    public void Approve_PendingLeave_ShouldRaiseEvent()
    {
        var leave = CreateLeave();
        leave.Approve(_approverUserId);
        leave.Status.Should().Be(LeaveStatus.Approved);
        leave.ApprovedByUserId.Should().Be(_approverUserId);
        leave.DomainEvents.Should().ContainSingle(e => e is LeaveApprovedEvent);
    }

    [Fact]
    public void Approve_AlreadyApproved_ShouldThrow()
    {
        var leave = CreateLeave();
        leave.Approve(_approverUserId);
        var act = () => leave.Approve(_approverUserId);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Reject_PendingLeave_ShouldRaiseEvent()
    {
        var leave = CreateLeave();
        leave.Reject(_approverUserId, "Yoğun dönem");
        leave.Status.Should().Be(LeaveStatus.Rejected);
        leave.Reason.Should().Be("Yoğun dönem");
        leave.DomainEvents.Should().ContainSingle(e => e is LeaveRejectedEvent);
    }

    [Fact]
    public void Reject_AlreadyApproved_ShouldThrow()
    {
        var leave = CreateLeave();
        leave.Approve(_approverUserId);
        var act = () => leave.Reject(_approverUserId, "Sonradan iptal");
        act.Should().Throw<InvalidOperationException>();
    }
}
