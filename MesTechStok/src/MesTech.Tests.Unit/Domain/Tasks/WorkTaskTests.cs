using FluentAssertions;
using MesTech.Domain.Entities.Tasks;
using MesTech.Domain.Enums;
using MesTech.Domain.Events.Tasks;
using Xunit;

namespace MesTech.Tests.Unit.Domain.Tasks;

/// <summary>
/// WorkTask entity unit testleri.
/// DEV 5 — H27-5.1 (emirname Task 5.1 uyarlanmış gerçek entity'ye gore)
/// </summary>
[Trait("Category", "Unit")]
[Trait("Domain", "Tasks")]
public class WorkTaskTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    private static WorkTask CreateTask(string title = "Test Gorevi",
        TaskPriority priority = TaskPriority.Normal,
        DateTime? dueDate = null)
        => WorkTask.Create(_tenantId, title, priority, dueDate: dueDate);

    // ── CREATE ──────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidTitle_ShouldSetStatusToBacklog()
    {
        var task = CreateTask();

        task.Status.Should().Be(WorkTaskStatus.Backlog);
        task.TenantId.Should().Be(_tenantId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Create_WithEmptyTitle_ShouldThrow(string? title)
    {
        var act = () => WorkTask.Create(_tenantId, title!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldHaveNullActualMinutes()
    {
        var task = CreateTask();
        // Entity'de ActualMinutes null'dan baslar (int? tipinde)
        task.ActualMinutes.Should().BeNull();
    }

    // ── START WORK ────────────────────────────────────────────────────

    [Fact]
    public void StartWork_FromBacklog_ShouldSetStatusToInProgress()
    {
        var task = CreateTask();
        task.StartWork(Guid.NewGuid());

        task.Status.Should().Be(WorkTaskStatus.InProgress);
        task.StartedAt.Should().NotBeNull();
    }

    [Fact]
    public void StartWork_ShouldUpdateAssignedUser()
    {
        var task = CreateTask();
        var userId = Guid.NewGuid();
        task.StartWork(userId);

        task.AssignedToUserId.Should().Be(userId);
    }

    // ── COMPLETE ─────────────────────────────────────────────────────

    [Fact]
    public void Complete_OpenTask_ShouldSetStatusToDone()
    {
        var task = CreateTask();
        task.Complete(Guid.NewGuid());

        task.Status.Should().Be(WorkTaskStatus.Done);
        task.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Complete_ShouldRaiseTaskCompletedEvent()
    {
        var task = CreateTask();
        var userId = Guid.NewGuid();
        task.Complete(userId);

        task.DomainEvents.OfType<TaskCompletedEvent>()
            .Single().CompletedByUserId.Should().Be(userId);
    }

    // ── ASSIGN TO ────────────────────────────────────────────────────

    [Fact]
    public void AssignTo_ShouldUpdateAssignedUser()
    {
        var task = CreateTask();
        var userId = Guid.NewGuid();
        task.AssignTo(userId);

        task.AssignedToUserId.Should().Be(userId);
    }

    // ── MOVE TO STATUS ───────────────────────────────────────────────

    [Fact]
    public void MoveToStatus_InReview_ShouldUpdateStatus()
    {
        var task = CreateTask();
        task.MoveToStatus(WorkTaskStatus.InReview);

        task.Status.Should().Be(WorkTaskStatus.InReview);
    }

    [Theory]
    [InlineData(TaskPriority.Low)]
    [InlineData(TaskPriority.High)]
    [InlineData(TaskPriority.Critical)]
    public void Create_WithPriority_ShouldPreservePriority(TaskPriority priority)
    {
        var task = CreateTask(priority: priority);
        task.Priority.Should().Be(priority);
    }

    // ── PROJECT LINK ─────────────────────────────────────────────────

    [Fact]
    public void Create_WithProjectId_ShouldSetProjectId()
    {
        var projectId = Guid.NewGuid();
        var task = WorkTask.Create(_tenantId, "Proje Gorevi", projectId: projectId);

        task.ProjectId.Should().Be(projectId);
    }

    // ── DOMAIN EVENT ─────────────────────────────────────────────────

    [Fact]
    public void Complete_EventContainsCorrectTaskId()
    {
        var task = CreateTask();
        var userId = Guid.NewGuid();
        task.Complete(userId);

        var evt = task.DomainEvents.OfType<TaskCompletedEvent>().Single();
        evt.TaskId.Should().Be(task.Id);
    }
}
