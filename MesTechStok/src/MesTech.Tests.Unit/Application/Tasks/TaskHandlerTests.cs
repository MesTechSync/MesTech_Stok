using FluentAssertions;
using MesTech.Application.Features.Tasks.Commands.CompleteTask;
using MesTech.Domain.Entities.Tasks;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Tasks;

[Trait("Category", "Unit")]
public class TaskHandlerTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task CompleteTaskHandler_OpenTask_ShouldComplete()
    {
        var task = WorkTask.Create(_tenantId, "Test Gorevi");
        var userId = Guid.NewGuid();
        var mockRepo = new Mock<IWorkTaskRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        await new CompleteTaskHandler(mockRepo.Object, Mock.Of<IUnitOfWork>())
            .Handle(new CompleteTaskCommand(task.Id, userId), CancellationToken.None);

        task.Status.Should().Be(WorkTaskStatus.Done);
        task.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CompleteTaskHandler_NotFound_ShouldThrow()
    {
        var mockRepo = new Mock<IWorkTaskRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((WorkTask?)null);

        var act = () => new CompleteTaskHandler(mockRepo.Object, Mock.Of<IUnitOfWork>())
            .Handle(new CompleteTaskCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
