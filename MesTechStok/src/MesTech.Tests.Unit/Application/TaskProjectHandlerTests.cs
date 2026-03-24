using FluentAssertions;
using MesTech.Application.Features.Tasks.Commands.CreateProject;
using MesTech.Application.Features.Tasks.Commands.CreateWorkTask;
using MesTech.Domain.Entities.Tasks;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class TaskProjectHandlerTests
{
    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly Mock<IWorkTaskRepository> _taskRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    [Fact]
    public async Task CreateProject_ValidCommand_ReturnsNonEmptyGuid()
    {
        _projectRepo.Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cmd = new CreateProjectCommand(
            TenantId: Guid.NewGuid(), Name: "Release v2", OwnerUserId: Guid.NewGuid(),
            Description: "Major release", StartDate: DateTime.UtcNow,
            DueDate: DateTime.UtcNow.AddMonths(1), Color: "#FF5733");
        var handler = new CreateProjectHandler(_projectRepo.Object, _uow.Object);
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateWorkTask_ValidCommand_ReturnsNonEmptyGuid()
    {
        _taskRepo.Setup(r => r.AddAsync(It.IsAny<WorkTask>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cmd = new CreateWorkTaskCommand(
            TenantId: Guid.NewGuid(),
            Title: "Fix login bug",
            Priority: TaskPriority.High,
            ProjectId: Guid.NewGuid(),
            MilestoneId: null,
            AssignedToUserId: Guid.NewGuid(),
            CreatedByUserId: Guid.NewGuid(),
            DueDate: DateTime.UtcNow.AddDays(7),
            EstimatedMinutes: 120,
            OrderId: null,
            CrmContactId: null,
            ProductId: null);
        var handler = new CreateWorkTaskHandler(_taskRepo.Object, _uow.Object);
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
    }
}
