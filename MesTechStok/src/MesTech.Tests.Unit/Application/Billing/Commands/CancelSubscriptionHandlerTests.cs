using FluentAssertions;
using MesTech.Application.Features.Billing.Commands.CancelSubscription;
using MesTech.Domain.Entities.Billing;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Billing.Commands;

[Trait("Category", "Unit")]
public class CancelSubscriptionHandlerTests
{
    private readonly Mock<ITenantSubscriptionRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CancelSubscriptionHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public CancelSubscriptionHandlerTests()
    {
        _sut = new CancelSubscriptionHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCancellation_ShouldCancelAndReturnUnit()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var subscription = TenantSubscription.Activate(TenantId, planId, BillingPeriod.Monthly);
        _repoMock.Setup(r => r.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        var command = new CancelSubscriptionCommand(TenantId, subscription.Id, "Cok pahali");

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
        subscription.CancellationReason.Should().Be("Cok pahali");
        _repoMock.Verify(r => r.UpdateAsync(subscription, It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentSubscription_ShouldThrow()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantSubscription?)null);
        var command = new CancelSubscriptionCommand(TenantId, Guid.NewGuid());

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*bulunamadi*");
    }

    [Fact]
    public async Task Handle_WrongTenant_ShouldThrow()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var subscription = TenantSubscription.Activate(TenantId, planId, BillingPeriod.Monthly);
        _repoMock.Setup(r => r.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        var otherTenant = Guid.NewGuid();
        var command = new CancelSubscriptionCommand(otherTenant, subscription.Id);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tenant*");
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
