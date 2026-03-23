using FluentAssertions;
using MesTech.Application.Features.Billing.Queries.GetTenantSubscription;
using MesTech.Domain.Entities.Billing;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Billing.Queries;

[Trait("Category", "Unit")]
public class GetTenantSubscriptionHandlerTests
{
    private readonly Mock<ITenantSubscriptionRepository> _repoMock = new();
    private readonly GetTenantSubscriptionHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid PlanId = Guid.NewGuid();

    public GetTenantSubscriptionHandlerTests()
    {
        _sut = new GetTenantSubscriptionHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ActiveSubscriptionExists_ShouldReturnDto()
    {
        // Arrange
        var sub = TenantSubscription.Activate(TenantId, PlanId, BillingPeriod.Monthly);
        _repoMock.Setup(r => r.GetActiveByTenantIdAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sub);

        // Act
        var result = await _sut.Handle(new GetTenantSubscriptionQuery(TenantId), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.PlanId.Should().Be(PlanId);
        result.Status.Should().Be(SubscriptionStatus.Active);
        result.Period.Should().Be(BillingPeriod.Monthly);
        result.IsExpired.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NoActiveSubscription_ShouldReturnNull()
    {
        // Arrange
        _repoMock.Setup(r => r.GetActiveByTenantIdAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantSubscription?)null);

        // Act
        var result = await _sut.Handle(new GetTenantSubscriptionQuery(TenantId), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => _sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_TrialSubscription_ShouldReturnWithTrialInfo()
    {
        // Arrange
        var sub = TenantSubscription.StartTrial(TenantId, PlanId, trialDays: 14);
        _repoMock.Setup(r => r.GetActiveByTenantIdAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sub);

        // Act
        var result = await _sut.Handle(new GetTenantSubscriptionQuery(TenantId), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(SubscriptionStatus.Trial);
        result.TrialEndsAt.Should().NotBeNull();
        result.PlanName.Should().BeEmpty(); // Plan navigation null in unit test
    }
}
