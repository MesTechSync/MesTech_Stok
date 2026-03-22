using FluentAssertions;
using MesTech.Application.Features.Crm.Queries.GetCustomerPoints;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Crm;

[Trait("Category", "Unit")]
[Trait("Feature", "CrmLoyaltyQueries")]
public class CustomerPointsQueryHandlerTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _customerId = Guid.NewGuid();

    [Fact]
    public async Task Handle_WithTransactions_ShouldCalculateBalance()
    {
        // Arrange
        var mockTxRepo = new Mock<ILoyaltyTransactionRepository>();
        mockTxRepo.Setup(r => r.GetPointsSumByTypeAsync(_tenantId, _customerId, LoyaltyTransactionType.Earn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1000);
        mockTxRepo.Setup(r => r.GetPointsSumByTypeAsync(_tenantId, _customerId, LoyaltyTransactionType.Redeem, It.IsAny<CancellationToken>()))
            .ReturnsAsync(-200);
        mockTxRepo.Setup(r => r.GetPointsSumByTypeAsync(_tenantId, _customerId, LoyaltyTransactionType.Expire, It.IsAny<CancellationToken>()))
            .ReturnsAsync(-50);
        mockTxRepo.Setup(r => r.GetByCustomerPagedAsync(_tenantId, _customerId, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LoyaltyTransaction>().AsReadOnly());

        var handler = new GetCustomerPointsHandler(mockTxRepo.Object);
        var query = new GetCustomerPointsQuery(_tenantId, _customerId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalEarned.Should().Be(1000);
        result.TotalRedeemed.Should().Be(200);
        result.TotalExpired.Should().Be(50);
        result.AvailableBalance.Should().Be(750); // 1000 - 200 - 50
    }

    [Fact]
    public async Task Handle_NoTransactions_ShouldReturnZeroBalance()
    {
        // Arrange
        var mockTxRepo = new Mock<ILoyaltyTransactionRepository>();
        mockTxRepo.Setup(r => r.GetPointsSumByTypeAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LoyaltyTransactionType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        mockTxRepo.Setup(r => r.GetByCustomerPagedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LoyaltyTransaction>().AsReadOnly());

        var handler = new GetCustomerPointsHandler(mockTxRepo.Object);

        // Act
        var result = await handler.Handle(new GetCustomerPointsQuery(_tenantId, _customerId), CancellationToken.None);

        // Assert
        result.AvailableBalance.Should().Be(0);
        result.TransactionHistory.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrow()
    {
        var handler = new GetCustomerPointsHandler(Mock.Of<ILoyaltyTransactionRepository>());
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
