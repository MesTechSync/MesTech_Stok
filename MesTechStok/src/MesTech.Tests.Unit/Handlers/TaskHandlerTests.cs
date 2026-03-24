using FluentAssertions;
using MediatR;
using MesTech.Application.Features.Tasks.Commands.CompleteTask;
using MesTech.Application.Features.Tasks.Commands.CreateProject;
using MesTech.Application.Features.Tasks.Commands.CreateWorkTask;
using MesTech.Domain.Entities.Tasks;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class TaskHandlerTests
{
    private readonly Mock<IWorkTaskRepository> _taskRepo;
    private readonly Mock<IProjectRepository> _projectRepo;
    private readonly Mock<IUnitOfWork> _uow;

    public TaskHandlerTests()
    {
        _taskRepo = new Mock<IWorkTaskRepository>();
        _projectRepo = new Mock<IProjectRepository>();
        _uow = new Mock<IUnitOfWork>();
    }

    // ═══════════════════════════════════════
    // CompleteTaskHandler
    // ═══════════════════════════════════════

    [Fact]
    public async Task CompleteTask_ExistingTask_CompletesAndSaves()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var task = WorkTask.Create(tenantId, "Test Task", TaskPriority.Normal);
        _taskRepo.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        var sut = new CompleteTaskHandler(_taskRepo.Object, _uow.Object);

        var result = await sut.Handle(new CompleteTaskCommand(task.Id, userId), CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task CompleteTask_NotFound_ThrowsInvalidOperation()
    {
        var taskId = Guid.NewGuid();
        _taskRepo.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>())).ReturnsAsync((WorkTask?)null);

        var sut = new CompleteTaskHandler(_taskRepo.Object, _uow.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(new CompleteTaskCommand(taskId, Guid.NewGuid()), CancellationToken.None));
    }

    // ═══════════════════════════════════════
    // CreateWorkTaskHandler
    // ═══════════════════════════════════════

    [Fact]
    public async Task CreateWorkTask_ValidCommand_ReturnsGuidAndSaves()
    {
        var sut = new CreateWorkTaskHandler(_taskRepo.Object, _uow.Object);
        var command = new CreateWorkTaskCommand(Guid.NewGuid(), "Yeni gorev", TaskPriority.High);

        var result = await sut.Handle(command, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
        _taskRepo.Verify(r => r.AddAsync(It.IsAny<WorkTask>(), It.IsAny<CancellationToken>()), Times.Once());
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task CreateWorkTask_NullRequest_ThrowsArgumentNullException()
    {
        var sut = new CreateWorkTaskHandler(_taskRepo.Object, _uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    // ═══════════════════════════════════════
    // CreateProjectHandler
    // ═══════════════════════════════════════

    [Fact]
    public async Task CreateProject_ValidCommand_ReturnsGuidAndSaves()
    {
        var sut = new CreateProjectHandler(_projectRepo.Object, _uow.Object);
        var command = new CreateProjectCommand(Guid.NewGuid(), "Proje Alpha");

        var result = await sut.Handle(command, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
        _projectRepo.Verify(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Once());
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task CreateProject_NullRequest_ThrowsArgumentNullException()
    {
        var sut = new CreateProjectHandler(_projectRepo.Object, _uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }
}
