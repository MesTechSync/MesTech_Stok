using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.RejectReconciliation;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Handlers;

/// <summary>
/// RejectReconciliationHandler tests — reject match, not found.
/// </summary>
[Trait("Category", "Unit")]
public class RejectReconciliationHandlerTests
{
    private readonly Mock<IReconciliationMatchRepository> _matchRepoMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly RejectReconciliationHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public RejectReconciliationHandlerTests()
    {
        _matchRepoMock = new Mock<IReconciliationMatchRepository>();
        _uowMock = new Mock<IUnitOfWork>();

        _sut = new RejectReconciliationHandler(
            _matchRepoMock.Object,
            _uowMock.Object);
    }

    private ReconciliationMatch CreateNeedsReviewMatch()
    {
        return ReconciliationMatch.Create(
            _tenantId,
            DateTime.UtcNow,
            0.75m,
            ReconciliationStatus.NeedsReview,
            Guid.NewGuid(),
            Guid.NewGuid());
    }

    [Fact]
    public async Task Handle_ValidMatch_SetsRejected()
    {
        var match = CreateNeedsReviewMatch();
        var reviewedBy = Guid.NewGuid();

        _matchRepoMock
            .Setup(r => r.GetByIdAsync(match.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        var command = new RejectReconciliationCommand(match.Id, reviewedBy, "Incorrect match");
        await _sut.Handle(command, CancellationToken.None);

        match.Status.Should().Be(ReconciliationStatus.Rejected);
        match.ReviewedBy.Should().Be(reviewedBy.ToString());
        match.ReviewedAt.Should().NotBeNull();

        _matchRepoMock.Verify(
            r => r.UpdateAsync(match, It.IsAny<CancellationToken>()),
            Times.Once());

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_NonExistent_ThrowsInvalidOperation()
    {
        var matchId = Guid.NewGuid();

        _matchRepoMock
            .Setup(r => r.GetByIdAsync(matchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReconciliationMatch?)null);

        var command = new RejectReconciliationCommand(matchId, Guid.NewGuid());

        var act = async () => await _sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{matchId}*not found*");
    }

    [Fact]
    public async Task Handle_SavesChangesOnce()
    {
        var match = CreateNeedsReviewMatch();

        _matchRepoMock
            .Setup(r => r.GetByIdAsync(match.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        await _sut.Handle(
            new RejectReconciliationCommand(match.Id, Guid.NewGuid()), CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_WithNoReason_StillSetsRejected()
    {
        var match = CreateNeedsReviewMatch();

        _matchRepoMock
            .Setup(r => r.GetByIdAsync(match.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        var command = new RejectReconciliationCommand(match.Id, Guid.NewGuid());
        await _sut.Handle(command, CancellationToken.None);

        match.Status.Should().Be(ReconciliationStatus.Rejected);
    }

    [Fact]
    public async Task Handle_UpdatesMatch()
    {
        var match = CreateNeedsReviewMatch();

        _matchRepoMock
            .Setup(r => r.GetByIdAsync(match.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        await _sut.Handle(
            new RejectReconciliationCommand(match.Id, Guid.NewGuid()), CancellationToken.None);

        _matchRepoMock.Verify(
            r => r.UpdateAsync(It.Is<ReconciliationMatch>(m => m.Status == ReconciliationStatus.Rejected),
                It.IsAny<CancellationToken>()),
            Times.Once());
    }
}
