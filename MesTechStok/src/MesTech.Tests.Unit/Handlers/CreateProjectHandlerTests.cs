using FluentAssertions;
using MesTech.Application.Features.Tasks.Commands.CreateProject;
using MesTech.Domain.Entities.Tasks;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class CreateProjectHandlerTests
{
    private readonly Mock<IProjectRepository> _projectRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateProjectHandler _sut;

    public CreateProjectHandlerTests()
    {
        _sut = new CreateProjectHandler(_projectRepoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesProjectAndReturnsId()
    {
        var cmd = new CreateProjectCommand(Guid.NewGuid(), "Sprint 15");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
        _projectRepoMock.Verify(r => r.AddAsync(It.Is<Project>(p => p.Name == "Sprint 15"), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
