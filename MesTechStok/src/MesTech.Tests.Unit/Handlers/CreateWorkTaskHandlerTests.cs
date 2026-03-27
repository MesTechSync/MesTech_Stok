using FluentAssertions;
using MesTech.Application.Features.Tasks.Commands.CreateWorkTask;
using MesTech.Domain.Entities.Tasks;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class CreateWorkTaskHandlerTests
{
    private readonly Mock<IWorkTaskRepository> _taskRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateWorkTaskHandler _sut;

    public CreateWorkTaskHandlerTests()
    {
        _sut = new CreateWorkTaskHandler(_taskRepoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesTaskAndReturnsId()
    {
        var cmd = new CreateWorkTaskCommand(Guid.NewGuid(), "Fix login bug");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
        _taskRepoMock.Verify(r => r.AddAsync(It.Is<WorkTask>(t => t.Title == "Fix login bug"), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
