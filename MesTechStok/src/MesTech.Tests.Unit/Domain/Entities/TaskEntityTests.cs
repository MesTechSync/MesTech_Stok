using FluentAssertions;
using MesTech.Domain.Entities.Calendar;
using MesTech.Domain.Entities.Hr;
using MesTech.Domain.Entities.Onboarding;
using MesTech.Domain.Entities.Tasks;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain.Entities;

/// <summary>
/// Task, HR, Calendar, Onboarding entity domain behavior tests.
/// WorkTask, Project, ProjectMember, TimeEntry, WorkSchedule,
/// OnboardingProgress, OnboardingStep, CalendarEvent, CalendarEventAttendee,
/// Leave, Employee, Department.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "TaskEntities")]
[Trait("Phase", "Dalga15")]
public class TaskEntityTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    // ═══════════════════════════════════════════
    // WorkTask
    // ═══════════════════════════════════════════

    [Fact]
    public void WorkTask_Create_SetsBacklogStatusAndTitle()
    {
        var task = WorkTask.Create(TenantId, "Build feature", TaskPriority.High);

        task.Title.Should().Be("Build feature");
        task.Status.Should().Be(WorkTaskStatus.Backlog);
        task.Priority.Should().Be(TaskPriority.High);
        task.TenantId.Should().Be(TenantId);
        task.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void WorkTask_Create_WithEmptyTitle_Throws()
    {
        var act = () => WorkTask.Create(TenantId, "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WorkTask_StartWork_SetsInProgressAndAssigns()
    {
        var task = WorkTask.Create(TenantId, "Task A");

        task.StartWork(UserId);

        task.Status.Should().Be(WorkTaskStatus.InProgress);
        task.AssignedToUserId.Should().Be(UserId);
        task.StartedAt.Should().NotBeNull();
    }

    [Fact]
    public void WorkTask_Complete_SetsDoneAndRaisesEvent()
    {
        var task = WorkTask.Create(TenantId, "Task A");
        task.StartWork(UserId);

        task.Complete(UserId);

        task.Status.Should().Be(WorkTaskStatus.Done);
        task.CompletedAt.Should().NotBeNull();
        task.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "TaskCompletedEvent");
    }

    [Fact]
    public void WorkTask_AssignTo_UpdatesAssignment()
    {
        var task = WorkTask.Create(TenantId, "Task A");
        var newUser = Guid.NewGuid();

        task.AssignTo(newUser);

        task.AssignedToUserId.Should().Be(newUser);
    }

    [Fact]
    public void WorkTask_MoveToStatus_ChangesStatus()
    {
        var task = WorkTask.Create(TenantId, "Task A");

        task.MoveToStatus(WorkTaskStatus.InReview);

        task.Status.Should().Be(WorkTaskStatus.InReview);
    }

    [Fact]
    public void WorkTask_CheckOverdue_WhenPastDueAndNotDone_ReturnsTrue()
    {
        var task = WorkTask.Create(TenantId, "Task A", dueDate: DateTime.UtcNow.AddDays(-1));

        var result = task.CheckOverdue();

        result.Should().BeTrue();
        task.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "TaskOverdueEvent");
    }

    [Fact]
    public void WorkTask_CheckOverdue_WhenDoneStatus_ReturnsFalse()
    {
        var task = WorkTask.Create(TenantId, "Task A", dueDate: DateTime.UtcNow.AddDays(-1));
        task.Complete(UserId);
        task.ClearDomainEvents();

        var result = task.CheckOverdue();

        result.Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    // Project
    // ═══════════════════════════════════════════

    [Fact]
    public void Project_Create_SetsPlanningStatus()
    {
        var project = Project.Create(TenantId, "Alpha Project");

        project.Name.Should().Be("Alpha Project");
        project.Status.Should().Be(ProjectStatus.Planning);
    }

    [Fact]
    public void Project_Create_WithEmptyName_Throws()
    {
        var act = () => Project.Create(TenantId, "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Project_Start_FromPlanning_SetsActive()
    {
        var project = Project.Create(TenantId, "Alpha");

        project.Start();

        project.Status.Should().Be(ProjectStatus.Active);
    }

    [Fact]
    public void Project_Start_FromActive_Throws()
    {
        var project = Project.Create(TenantId, "Alpha");
        project.Start();

        var act = () => project.Start();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Project_Complete_SetsCompletedAtTimestamp()
    {
        var project = Project.Create(TenantId, "Alpha");
        project.Start();

        project.Complete();

        project.Status.Should().Be(ProjectStatus.Completed);
        project.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Project_PutOnHold_SetsOnHoldStatus()
    {
        var project = Project.Create(TenantId, "Alpha");

        project.PutOnHold();

        project.Status.Should().Be(ProjectStatus.OnHold);
    }

    [Fact]
    public void Project_Start_FromOnHold_SetsActive()
    {
        var project = Project.Create(TenantId, "Alpha");
        project.PutOnHold();

        project.Start();

        project.Status.Should().Be(ProjectStatus.Active);
    }

    // ═══════════════════════════════════════════
    // ProjectMember
    // ═══════════════════════════════════════════

    [Fact]
    public void ProjectMember_Create_SetsDefaultMemberRole()
    {
        var projectId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var member = ProjectMember.Create(tenantId, projectId, UserId);

        member.TenantId.Should().Be(tenantId);
        member.ProjectId.Should().Be(projectId);
        member.UserId.Should().Be(UserId);
        member.Role.Should().Be("Member");
    }

    [Fact]
    public void ProjectMember_Create_WithCustomRole_SetsRole()
    {
        var member = ProjectMember.Create(Guid.NewGuid(), Guid.NewGuid(), UserId, "Owner");

        member.Role.Should().Be("Owner");
    }

    // ═══════════════════════════════════════════
    // TimeEntry
    // ═══════════════════════════════════════════

    [Fact]
    public void TimeEntry_Start_CreatesWithZeroMinutes()
    {
        var taskId = Guid.NewGuid();
        var entry = TimeEntry.Start(TenantId, taskId, UserId, "coding");

        entry.WorkTaskId.Should().Be(taskId);
        entry.UserId.Should().Be(UserId);
        entry.Minutes.Should().Be(0);
        entry.Description.Should().Be("coding");
        entry.EndedAt.Should().BeNull();
    }

    [Fact]
    public void TimeEntry_Stop_CalculatesMinutesAndSetsEndedAt()
    {
        var entry = TimeEntry.Start(TenantId, Guid.NewGuid(), UserId);

        entry.Stop();

        entry.EndedAt.Should().NotBeNull();
        entry.Minutes.Should().BeGreaterThanOrEqualTo(0);
    }

    // ═══════════════════════════════════════════
    // WorkSchedule
    // ═══════════════════════════════════════════

    [Fact]
    public void WorkSchedule_Create_ValidWorkDay_Succeeds()
    {
        var empId = Guid.NewGuid();
        var schedule = WorkSchedule.Create(TenantId, empId, DayOfWeek.Monday,
            new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0));

        schedule.DayOfWeek.Should().Be(DayOfWeek.Monday);
        schedule.IsWorkDay.Should().BeTrue();
        schedule.StartTime.Should().Be(new TimeSpan(9, 0, 0));
        schedule.EndTime.Should().Be(new TimeSpan(17, 0, 0));
    }

    [Fact]
    public void WorkSchedule_Create_WorkDay_EndBeforeStart_Throws()
    {
        var act = () => WorkSchedule.Create(TenantId, Guid.NewGuid(), DayOfWeek.Monday,
            new TimeSpan(17, 0, 0), new TimeSpan(9, 0, 0));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WorkSchedule_Create_NonWorkDay_EndBeforeStart_DoesNotThrow()
    {
        var schedule = WorkSchedule.Create(TenantId, Guid.NewGuid(), DayOfWeek.Sunday,
            new TimeSpan(0, 0, 0), new TimeSpan(0, 0, 0), isWorkDay: false);

        schedule.IsWorkDay.Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    // OnboardingProgress
    // ═══════════════════════════════════════════

    [Fact]
    public void OnboardingProgress_Start_BeginsAtRegistration()
    {
        var progress = OnboardingProgress.Start(TenantId);

        progress.CurrentStep.Should().Be(OnboardingStep.Registration);
        progress.IsCompleted.Should().BeFalse();
        progress.CompletedStepsJson.Should().Be("[]");
    }

    [Fact]
    public void OnboardingProgress_CompleteCurrentStep_AdvancesToNext()
    {
        var progress = OnboardingProgress.Start(TenantId);

        progress.CompleteCurrentStep();

        progress.CurrentStep.Should().Be(OnboardingStep.CompanyInfo);
    }

    [Fact]
    public void OnboardingProgress_CompleteAllSteps_MarksCompleted()
    {
        var progress = OnboardingProgress.Start(TenantId);

        for (int i = 0; i < 7; i++)
            progress.CompleteCurrentStep();

        progress.IsCompleted.Should().BeTrue();
        progress.CompletedAt.Should().NotBeNull();
        progress.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "OnboardingCompletedEvent");
    }

    [Fact]
    public void OnboardingProgress_CompleteCurrentStep_WhenAlreadyCompleted_Throws()
    {
        var progress = OnboardingProgress.Start(TenantId);
        for (int i = 0; i < 7; i++)
            progress.CompleteCurrentStep();

        var act = () => progress.CompleteCurrentStep();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void OnboardingProgress_SkipToStep_JumpsToSpecifiedStep()
    {
        var progress = OnboardingProgress.Start(TenantId);

        progress.SkipToStep(OnboardingStep.InitialSync);

        progress.CurrentStep.Should().Be(OnboardingStep.InitialSync);
    }

    [Fact]
    public void OnboardingProgress_CompletionPercentage_ReflectsStep()
    {
        var progress = OnboardingProgress.Start(TenantId);

        progress.CompletionPercentage.Should().BeGreaterThanOrEqualTo(0);
        progress.CompletionPercentage.Should().BeLessThan(100);
    }

    // ═══════════════════════════════════════════
    // CalendarEvent
    // ═══════════════════════════════════════════

    [Fact]
    public void CalendarEvent_Create_SetsFieldsAndRaisesEvent()
    {
        var start = DateTime.UtcNow.AddHours(1);
        var end = DateTime.UtcNow.AddHours(2);

        var ev = CalendarEvent.Create(TenantId, "Meeting", start, end, EventType.Custom);

        ev.Title.Should().Be("Meeting");
        ev.StartAt.Should().Be(start);
        ev.EndAt.Should().Be(end);
        ev.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "CalendarEventCreatedEvent");
    }

    [Fact]
    public void CalendarEvent_Create_EndBeforeStart_Throws()
    {
        var start = DateTime.UtcNow.AddHours(2);
        var end = DateTime.UtcNow.AddHours(1);

        var act = () => CalendarEvent.Create(TenantId, "Meeting", start, end);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CalendarEvent_Create_EmptyTitle_Throws()
    {
        var act = () => CalendarEvent.Create(TenantId, "", DateTime.UtcNow, DateTime.UtcNow.AddHours(1));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CalendarEvent_AddAttendee_AddsOnce_IgnoresDuplicate()
    {
        var ev = CalendarEvent.Create(TenantId, "Meeting", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), isAllDay: true);

        ev.AddAttendee(UserId);
        ev.AddAttendee(UserId); // duplicate

        ev.Attendees.Should().HaveCount(1);
    }

    [Fact]
    public void CalendarEvent_MarkAsCompleted_SetsFlag()
    {
        var ev = CalendarEvent.Create(TenantId, "Task", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), isAllDay: true);

        ev.MarkAsCompleted();

        ev.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void CalendarEvent_MarkAsIncomplete_ClearsFlag()
    {
        var ev = CalendarEvent.Create(TenantId, "Task", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), isAllDay: true);
        ev.MarkAsCompleted();

        ev.MarkAsIncomplete();

        ev.IsCompleted.Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    // CalendarEventAttendee
    // ═══════════════════════════════════════════

    [Fact]
    public void CalendarEventAttendee_Create_StartsAsPending()
    {
        var attendee = CalendarEventAttendee.Create(Guid.NewGuid(), UserId);

        attendee.Status.Should().Be(AttendeeStatus.Pending);
        attendee.UserId.Should().Be(UserId);
    }

    [Fact]
    public void CalendarEventAttendee_Accept_SetsAccepted()
    {
        var attendee = CalendarEventAttendee.Create(Guid.NewGuid(), UserId);

        attendee.Accept();

        attendee.Status.Should().Be(AttendeeStatus.Accepted);
    }

    [Fact]
    public void CalendarEventAttendee_Decline_SetsDeclined()
    {
        var attendee = CalendarEventAttendee.Create(Guid.NewGuid(), UserId);

        attendee.Decline();

        attendee.Status.Should().Be(AttendeeStatus.Declined);
    }

    // ═══════════════════════════════════════════
    // Leave
    // ═══════════════════════════════════════════

    [Fact]
    public void Leave_Create_SetsPendingStatus()
    {
        var leave = Leave.Create(TenantId, Guid.NewGuid(), LeaveType.Annual,
            DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));

        leave.Status.Should().Be(LeaveStatus.Pending);
        leave.TotalDays.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Leave_Create_EndBeforeStart_Throws()
    {
        var act = () => Leave.Create(TenantId, Guid.NewGuid(), LeaveType.Annual,
            DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(5));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Leave_Approve_SetStatusAndRaisesEvent()
    {
        var leave = Leave.Create(TenantId, Guid.NewGuid(), LeaveType.Annual,
            DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));
        var approverId = Guid.NewGuid();

        leave.Approve(approverId);

        leave.Status.Should().Be(LeaveStatus.Approved);
        leave.ApprovedByUserId.Should().Be(approverId);
        leave.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "LeaveApprovedEvent");
    }

    [Fact]
    public void Leave_Approve_WhenNotPending_Throws()
    {
        var leave = Leave.Create(TenantId, Guid.NewGuid(), LeaveType.Annual,
            DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));
        leave.Approve(Guid.NewGuid());

        var act = () => leave.Approve(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Leave_Reject_SetStatusAndReason()
    {
        var leave = Leave.Create(TenantId, Guid.NewGuid(), LeaveType.Annual,
            DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));

        leave.Reject(Guid.NewGuid(), "Team capacity");

        leave.Status.Should().Be(LeaveStatus.Rejected);
        leave.RejectionReason.Should().Be("Team capacity");
        leave.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "LeaveRejectedEvent");
    }

    [Fact]
    public void Leave_Reject_WithEmptyReason_Throws()
    {
        var leave = Leave.Create(TenantId, Guid.NewGuid(), LeaveType.Annual,
            DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));

        var act = () => leave.Reject(Guid.NewGuid(), "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Leave_Cancel_SetsCancelledStatus()
    {
        var leave = Leave.Create(TenantId, Guid.NewGuid(), LeaveType.Annual,
            DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10));

        leave.Cancel();

        leave.Status.Should().Be(LeaveStatus.Cancelled);
    }

    // ═══════════════════════════════════════════
    // Employee
    // ═══════════════════════════════════════════

    [Fact]
    public void Employee_Create_SetsActiveStatusAndCode()
    {
        var emp = Employee.Create(TenantId, UserId, "EMP-001", DateTime.UtcNow.AddDays(-30));

        emp.EmployeeCode.Should().Be("EMP-001");
        emp.Status.Should().Be(EmployeeStatus.Active);
    }

    [Fact]
    public void Employee_Create_EmptyCode_Throws()
    {
        var act = () => Employee.Create(TenantId, UserId, "", DateTime.UtcNow);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Employee_Create_FutureHireDate_Throws()
    {
        var act = () => Employee.Create(TenantId, UserId, "EMP-X", DateTime.UtcNow.AddDays(30));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Employee_Terminate_SetsTerminatedStatusAndDate()
    {
        var emp = Employee.Create(TenantId, UserId, "EMP-001", DateTime.UtcNow.AddDays(-30));

        emp.Terminate(DateTime.UtcNow);

        emp.Status.Should().Be(EmployeeStatus.Terminated);
        emp.TerminationDate.Should().NotBeNull();
    }

    [Fact]
    public void Employee_Terminate_WhenAlreadyTerminated_Throws()
    {
        var emp = Employee.Create(TenantId, UserId, "EMP-001", DateTime.UtcNow.AddDays(-30));
        emp.Terminate(DateTime.UtcNow);

        var act = () => emp.Terminate(DateTime.UtcNow);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Employee_PutOnLeave_FromActive_Succeeds()
    {
        var emp = Employee.Create(TenantId, UserId, "EMP-001", DateTime.UtcNow.AddDays(-30));

        emp.PutOnLeave();

        emp.Status.Should().Be(EmployeeStatus.OnLeave);
    }

    [Fact]
    public void Employee_PutOnLeave_WhenNotActive_Throws()
    {
        var emp = Employee.Create(TenantId, UserId, "EMP-001", DateTime.UtcNow.AddDays(-30));
        emp.Terminate(DateTime.UtcNow);

        var act = () => emp.PutOnLeave();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Employee_ReturnFromLeave_SetsActive()
    {
        var emp = Employee.Create(TenantId, UserId, "EMP-001", DateTime.UtcNow.AddDays(-30));
        emp.PutOnLeave();

        emp.ReturnFromLeave();

        emp.Status.Should().Be(EmployeeStatus.Active);
    }

    [Fact]
    public void Employee_ReturnFromLeave_WhenNotOnLeave_Throws()
    {
        var emp = Employee.Create(TenantId, UserId, "EMP-001", DateTime.UtcNow.AddDays(-30));

        var act = () => emp.ReturnFromLeave();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Employee_UpdateSalary_NegativeAmount_Throws()
    {
        var emp = Employee.Create(TenantId, UserId, "EMP-001", DateTime.UtcNow.AddDays(-30));

        var act = () => emp.UpdateSalary(-1000m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Employee_UpdateSalary_SetsValue()
    {
        var emp = Employee.Create(TenantId, UserId, "EMP-001", DateTime.UtcNow.AddDays(-30));

        emp.UpdateSalary(15000m);

        emp.MonthlySalary.Should().Be(15000m);
    }

    [Fact]
    public void Employee_AssignToDepartment_SetsDepartmentId()
    {
        var emp = Employee.Create(TenantId, UserId, "EMP-001", DateTime.UtcNow.AddDays(-30));
        var deptId = Guid.NewGuid();

        emp.AssignToDepartment(deptId);

        emp.DepartmentId.Should().Be(deptId);
    }

    // ═══════════════════════════════════════════
    // Department
    // ═══════════════════════════════════════════

    [Fact]
    public void Department_Create_SetsNameAndTenant()
    {
        var dept = Department.Create(TenantId, "Engineering");

        dept.Name.Should().Be("Engineering");
        dept.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public void Department_Create_EmptyName_Throws()
    {
        var act = () => Department.Create(TenantId, "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Department_Rename_UpdatesName()
    {
        var dept = Department.Create(TenantId, "Engineering");

        dept.Rename("Product Engineering");

        dept.Name.Should().Be("Product Engineering");
    }

    [Fact]
    public void Department_Rename_EmptyName_Throws()
    {
        var dept = Department.Create(TenantId, "Engineering");

        var act = () => dept.Rename("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Department_SetManager_SetsManagerId()
    {
        var dept = Department.Create(TenantId, "Engineering");
        var managerId = Guid.NewGuid();

        dept.SetManager(managerId);

        dept.ManagerEmployeeId.Should().Be(managerId);
    }
}
