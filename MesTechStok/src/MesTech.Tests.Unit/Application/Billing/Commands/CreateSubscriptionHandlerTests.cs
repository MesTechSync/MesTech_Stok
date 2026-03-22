using FluentAssertions;
using MesTech.Application.Features.Billing.Commands.CreateSubscription;
using MesTech.Domain.Entities.Billing;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Billing.Commands;

[Trait("Category", "Unit")]
public class CreateSubscriptionHandlerTests
{
    private readonly Mock<ITenantSubscriptionRepository> _subscriptionRepoMock = new();
    private readonly Mock<ISubscriptionPlanRepository> _planRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateSubscriptionHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public CreateSubscriptionHandlerTests()
    {
        _sut = new CreateSubscriptionHandler(
            _subscriptionRepoMock.Object, _planRepoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidTrialRequest_ShouldCreateTrialSubscription()
    {
        // Arrange
        var plan = SubscriptionPlan.SeedBasic();
        _planRepoMock.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        _subscriptionRepoMock.Setup(r => r.GetActiveByTenantIdAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantSubscription?)null);

        var command = new CreateSubscriptionCommand(TenantId, plan.Id, StartAsTrial: true);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _subscriptionRepoMock.Verify(r => r.AddAsync(
            It.Is<TenantSubscription>(s => s.Status == SubscriptionStatus.Trial),
            It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingActiveSubscription_ShouldThrow()
    {
        // Arrange
        var plan = SubscriptionPlan.SeedBasic();
        _planRepoMock.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        var existingSub = TenantSubscription.Activate(TenantId, plan.Id, BillingPeriod.Monthly);
        _subscriptionRepoMock.Setup(r => r.GetActiveByTenantIdAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSub);

        var command = new CreateSubscriptionCommand(TenantId, plan.Id);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*aktif*aboneli*");
    }

    [Fact]
    public async Task Handle_NonExistentPlan_ShouldThrow()
    {
        // Arrange
        _planRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan?)null);

        var command = new CreateSubscriptionCommand(TenantId, Guid.NewGuid());

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*bulunamadi*");
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
