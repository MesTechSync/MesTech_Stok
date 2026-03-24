using FluentAssertions;
using MesTech.Application.Features.Tasks.Queries.GetProjects;
using MesTech.Application.Features.Tasks.Queries.GetProjectTasks;
using MesTech.Domain.Entities.Tasks;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class TaskQueryHandlerTests
{
    [Fact]
    public async Task GetProjects_ReturnsProjectList()
    {
        var repo = new Mock<IProjectRepository>();
        repo.Setup(r => r.GetByTenantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());

        var handler = new GetProjectsHandler(repo.Object);
        var result = await handler.Handle(
            new GetProjectsQuery(TenantId: Guid.NewGuid()),
            CancellationToken.None);
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProjectTasks_ReturnsTaskList()
    {
        var repo = new Mock<IWorkTaskRepository>();
        repo.Setup(r => r.GetByProjectAsync(
                It.IsAny<Guid>(), It.IsAny<WorkTaskStatus?>(),
                It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkTask>());

        var handler = new GetProjectTasksHandler(repo.Object);
        var result = await handler.Handle(
            new GetProjectTasksQuery(ProjectId: Guid.NewGuid()),
            CancellationToken.None);
        result.Should().NotBeNull();
    }
}
