using FluentAssertions;
using MesTech.Application.Features.Hr.Commands.ApproveLeave;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class ApproveLeaveHandlerTests
{
    private readonly Mock<ILeaveRepository> _leaveRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly ApproveLeaveHandler _sut;

    public ApproveLeaveHandlerTests()
    {
        _sut = new ApproveLeaveHandler(_leaveRepoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_NonExistentLeave_ThrowsInvalidOperationException()
    {
        var leaveId = Guid.NewGuid();
        _leaveRepoMock.Setup(r => r.GetByIdAsync(leaveId, It.IsAny<CancellationToken>())).ReturnsAsync((Leave?)null);

        var cmd = new ApproveLeaveCommand(leaveId, Guid.NewGuid());
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
