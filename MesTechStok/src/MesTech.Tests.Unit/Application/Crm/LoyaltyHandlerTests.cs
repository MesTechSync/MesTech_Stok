using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.EarnPoints;
using MesTech.Application.Features.Crm.Commands.RedeemPoints;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Crm;

[Trait("Category", "Unit")]
[Trait("Feature", "CrmLoyalty")]
public class LoyaltyHandlerTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _customerId = Guid.NewGuid();

    private static LoyaltyProgram MakeProgram(decimal pointsPerPurchase = 1m, int minRedeem = 100)
        => LoyaltyProgram.Create(_tenantId, "Test Program", pointsPerPurchase, minRedeem);

    // ── EarnPointsHandler ──

    [Fact]
    public async Task EarnPoints_ValidOrder_ShouldCalculateAndReturnPoints()
    {
        // Arrange
        var program = MakeProgram(pointsPerPurchase: 2m);
        var mockProgramRepo = new Mock<ILoyaltyProgramRepository>();
        mockProgramRepo.Setup(r => r.GetActiveByTenantAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(program);
        var mockTxRepo = new Mock<ILoyaltyTransactionRepository>();
        var mockUow = new Mock<IUnitOfWork>();

        var handler = new EarnPointsHandler(mockProgramRepo.Object, mockTxRepo.Object, mockUow.Object);
        var command = new EarnPointsCommand(_tenantId, _customerId, Guid.NewGuid(), 150m);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.EarnedPoints.Should().Be(300); // 150 * 2 = 300
        result.TransactionId.Should().NotBeEmpty();
        mockTxRepo.Verify(r => r.AddAsync(It.IsAny<LoyaltyTransaction>(), It.IsAny<CancellationToken>()), Times.Once);
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EarnPoints_ZeroAmount_ShouldReturnZeroPoints()
    {
        // Arrange
        var program = MakeProgram(pointsPerPurchase: 1m);
        var mockProgramRepo = new Mock<ILoyaltyProgramRepository>();
        mockProgramRepo.Setup(r => r.GetActiveByTenantAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(program);
        var mockTxRepo = new Mock<ILoyaltyTransactionRepository>();
        var mockUow = new Mock<IUnitOfWork>();

        var handler = new EarnPointsHandler(mockProgramRepo.Object, mockTxRepo.Object, mockUow.Object);
        var command = new EarnPointsCommand(_tenantId, _customerId, Guid.NewGuid(), 0m);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.EarnedPoints.Should().Be(0);
        result.TransactionId.Should().BeNull();
        mockTxRepo.Verify(r => r.AddAsync(It.IsAny<LoyaltyTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EarnPoints_NoProgramFound_ShouldThrow()
    {
        // Arrange
        var mockProgramRepo = new Mock<ILoyaltyProgramRepository>();
        mockProgramRepo.Setup(r => r.GetActiveByTenantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoyaltyProgram?)null);
        var handler = new EarnPointsHandler(
            mockProgramRepo.Object,
            Mock.Of<ILoyaltyTransactionRepository>(),
            Mock.Of<IUnitOfWork>());

        var command = new EarnPointsCommand(_tenantId, _customerId, Guid.NewGuid(), 100m);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No active loyalty program*");
    }

    [Fact]
    public async Task EarnPoints_NullRequest_ShouldThrow()
    {
        var handler = new EarnPointsHandler(
            Mock.Of<ILoyaltyProgramRepository>(),
            Mock.Of<ILoyaltyTransactionRepository>(),
            Mock.Of<IUnitOfWork>());

        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── RedeemPointsHandler ──

    [Fact]
    public async Task RedeemPoints_SufficientBalance_ShouldRedeemAndReturnDiscount()
    {
        // Arrange
        var program = MakeProgram(pointsPerPurchase: 1m, minRedeem: 100);
        var mockProgramRepo = new Mock<ILoyaltyProgramRepository>();
        mockProgramRepo.Setup(r => r.GetActiveByTenantAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(program);

        var mockTxRepo = new Mock<ILoyaltyTransactionRepository>();
        mockTxRepo.Setup(r => r.GetPointsSumByTypeAsync(_tenantId, _customerId, LoyaltyTransactionType.Earn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(500);
        mockTxRepo.Setup(r => r.GetPointsSumByTypeAsync(_tenantId, _customerId, LoyaltyTransactionType.Redeem, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        mockTxRepo.Setup(r => r.GetPointsSumByTypeAsync(_tenantId, _customerId, LoyaltyTransactionType.Expire, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var mockUow = new Mock<IUnitOfWork>();
        var handler = new RedeemPointsHandler(mockProgramRepo.Object, mockTxRepo.Object, mockUow.Object);
        var command = new RedeemPointsCommand(_tenantId, _customerId, 200);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.RedeemedPoints.Should().Be(200);
        result.DiscountAmount.Should().Be(2m); // 200 / 100
        result.RemainingBalance.Should().Be(300); // 500 - 200
        result.TransactionId.Should().NotBeEmpty();
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RedeemPoints_InsufficientBalance_ShouldThrow()
    {
        // Arrange
        var program = MakeProgram(pointsPerPurchase: 1m, minRedeem: 100);
        var mockProgramRepo = new Mock<ILoyaltyProgramRepository>();
        mockProgramRepo.Setup(r => r.GetActiveByTenantAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(program);

        var mockTxRepo = new Mock<ILoyaltyTransactionRepository>();
        mockTxRepo.Setup(r => r.GetPointsSumByTypeAsync(_tenantId, _customerId, LoyaltyTransactionType.Earn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(50);
        mockTxRepo.Setup(r => r.GetPointsSumByTypeAsync(_tenantId, _customerId, LoyaltyTransactionType.Redeem, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        mockTxRepo.Setup(r => r.GetPointsSumByTypeAsync(_tenantId, _customerId, LoyaltyTransactionType.Expire, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var handler = new RedeemPointsHandler(
            mockProgramRepo.Object, mockTxRepo.Object, Mock.Of<IUnitOfWork>());
        var command = new RedeemPointsCommand(_tenantId, _customerId, 200);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Insufficient points*");
    }

    [Fact]
    public async Task RedeemPoints_BelowMinThreshold_ShouldThrow()
    {
        // Arrange
        var program = MakeProgram(pointsPerPurchase: 1m, minRedeem: 500);
        var mockProgramRepo = new Mock<ILoyaltyProgramRepository>();
        mockProgramRepo.Setup(r => r.GetActiveByTenantAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(program);

        var handler = new RedeemPointsHandler(
            mockProgramRepo.Object,
            Mock.Of<ILoyaltyTransactionRepository>(),
            Mock.Of<IUnitOfWork>());
        var command = new RedeemPointsCommand(_tenantId, _customerId, 100);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Minimum redeem threshold*");
    }

    [Fact]
    public async Task RedeemPoints_NullRequest_ShouldThrow()
    {
        var handler = new RedeemPointsHandler(
            Mock.Of<ILoyaltyProgramRepository>(),
            Mock.Of<ILoyaltyTransactionRepository>(),
            Mock.Of<IUnitOfWork>());

        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
