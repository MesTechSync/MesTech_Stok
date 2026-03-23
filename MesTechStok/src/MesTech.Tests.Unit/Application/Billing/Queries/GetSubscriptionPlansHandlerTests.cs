using FluentAssertions;
using MesTech.Application.Features.Billing.Queries.GetSubscriptionPlans;
using MesTech.Domain.Entities.Billing;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Billing.Queries;

[Trait("Category", "Unit")]
public class GetSubscriptionPlansHandlerTests
{
    private readonly Mock<ISubscriptionPlanRepository> _repoMock = new();
    private readonly GetSubscriptionPlansHandler _sut;

    public GetSubscriptionPlansHandlerTests()
    {
        _sut = new GetSubscriptionPlansHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ActivePlansExist_ShouldReturnMappedDtos()
    {
        // Arrange
        var plan = SubscriptionPlan.Create("Profesyonel", 799m, 7990m,
            maxStores: 5, maxProducts: 10000, maxUsers: 5,
            description: "Coklu magaza");
        _repoMock.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SubscriptionPlan> { plan }.AsReadOnly());

        // Act
        var result = await _sut.Handle(new GetSubscriptionPlansQuery(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Profesyonel");
        result[0].MonthlyPrice.Should().Be(799m);
        result[0].AnnualPrice.Should().Be(7990m);
        result[0].MaxStores.Should().Be(5);
    }

    [Fact]
    public async Task Handle_NoActivePlans_ShouldReturnEmptyList()
    {
        // Arrange
        _repoMock.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SubscriptionPlan>().AsReadOnly());

        // Act
        var result = await _sut.Handle(new GetSubscriptionPlansQuery(), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
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
    public async Task Handle_MultiplePlans_ShouldReturnAllWithCorrectTrialDays()
    {
        // Arrange
        var basic = SubscriptionPlan.SeedBasic();
        var pro = SubscriptionPlan.SeedProfessional();
        var enterprise = SubscriptionPlan.SeedEnterprise();
        _repoMock.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SubscriptionPlan> { basic, pro, enterprise }.AsReadOnly());

        // Act
        var result = await _sut.Handle(new GetSubscriptionPlansQuery(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(p => p.TrialDays.Should().Be(14));
    }
}
