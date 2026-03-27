using FluentAssertions;
using MesTech.Application.Features.Tasks.Commands.CompleteTask;
using MesTech.Domain.Entities.Tasks;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class CompleteTaskHandlerTests
{
    private readonly Mock<IWorkTaskRepository> _taskRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CompleteTaskHandler _sut;

    public CompleteTaskHandlerTests()
    {
        _sut = new CompleteTaskHandler(_taskRepoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_NonExistentTask_ThrowsInvalidOperationException()
    {
        var taskId = Guid.NewGuid();
        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>())).ReturnsAsync((WorkTask?)null);

        var cmd = new CompleteTaskCommand(taskId, Guid.NewGuid());
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingTask_CallsSaveChanges()
    {
        var taskId = Guid.NewGuid();
        var task = WorkTask.Create(Guid.NewGuid(), "Test Task", MesTech.Domain.Enums.TaskPriority.Normal);
        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        var cmd = new CompleteTaskCommand(taskId, Guid.NewGuid());
        await _sut.Handle(cmd, CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
