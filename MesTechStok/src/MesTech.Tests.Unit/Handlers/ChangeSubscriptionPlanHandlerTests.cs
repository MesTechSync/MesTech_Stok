using FluentAssertions;
using MesTech.Application.Features.Billing.Commands.ChangeSubscriptionPlan;
using MesTech.Domain.Entities.Billing;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class ChangeSubscriptionPlanHandlerTests
{
    private readonly Mock<ITenantSubscriptionRepository> _subscriptionRepoMock = new();
    private readonly Mock<ISubscriptionPlanRepository> _planRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly ChangeSubscriptionPlanHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ChangeSubscriptionPlanHandlerTests()
    {
        _sut = new ChangeSubscriptionPlanHandler(
            _subscriptionRepoMock.Object, _planRepoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_NoActiveSubscription_ThrowsInvalidOperationException()
    {
        _subscriptionRepoMock.Setup(r => r.GetActiveByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantSubscription?)null);

        var cmd = new ChangeSubscriptionPlanCommand(_tenantId, Guid.NewGuid());
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Aktif abonelik*");
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
