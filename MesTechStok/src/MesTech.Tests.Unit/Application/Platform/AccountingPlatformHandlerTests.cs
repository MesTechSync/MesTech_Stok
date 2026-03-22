using FluentAssertions;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Features.Accounting.Commands.CreatePlatformCommissionRate;
using MesTech.Application.Features.Accounting.Commands.UpdatePlatformCommissionRate;
using MesTech.Application.Features.Accounting.Queries.GetPlatformCommissionRates;
using MesTech.Application.Features.Accounting.Queries.GetReconciliationDashboard;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Platform;

// ════════════════════════════════════════════════════════
// DEV5 Agent E: Accounting Platform Handler Tests
// ════════════════════════════════════════════════════════

#region CreatePlatformCommissionRateHandler

[Trait("Category", "Unit")]
public class CreatePlatformCommissionRateHandlerTests
{
    private readonly Mock<IPlatformCommissionRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private CreatePlatformCommissionRateHandler CreateHandler() => new(_repo.Object, _uow.Object);

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateCommissionAndReturnId()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new CreatePlatformCommissionRateCommand(
            _tenantId, PlatformType.Trendyol, 0.12m, CommissionType.Percentage);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _repo.Verify(r => r.AddAsync(It.Is<PlatformCommission>(c =>
            c.Platform == PlatformType.Trendyol &&
            c.Rate == 0.12m &&
            c.IsActive), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEffectiveDates_ShouldSetDates()
    {
        // Arrange
        var effectiveFrom = new DateTime(2026, 4, 1);
        var effectiveTo = new DateTime(2026, 12, 31);

        var handler = CreateHandler();
        var command = new CreatePlatformCommissionRateCommand(
            _tenantId, PlatformType.Hepsiburada, 0.15m,
            EffectiveFrom: effectiveFrom, EffectiveTo: effectiveTo);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _repo.Verify(r => r.AddAsync(It.Is<PlatformCommission>(c =>
            c.EffectiveFrom == effectiveFrom &&
            c.EffectiveTo == effectiveTo), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

#endregion

#region UpdatePlatformCommissionRateHandler

[Trait("Category", "Unit")]
public class UpdatePlatformCommissionRateHandlerTests
{
    private readonly Mock<IPlatformCommissionRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private UpdatePlatformCommissionRateHandler CreateHandler() => new(_repo.Object, _uow.Object);

    [Fact]
    public async Task Handle_ExistingCommission_ShouldUpdateAndReturnTrue()
    {
        // Arrange
        var commissionId = Guid.NewGuid();
        var existing = new PlatformCommission
        {
            Platform = PlatformType.Trendyol,
            Rate = 0.10m,
            IsActive = true
        };

        _repo.Setup(r => r.GetByIdAsync(commissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var handler = CreateHandler();
        var command = new UpdatePlatformCommissionRateCommand(commissionId, Rate: 0.15m);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        existing.Rate.Should().Be(0.15m);
        _repo.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotFoundCommission_ShouldReturnFalse()
    {
        // Arrange
        var commissionId = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(commissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlatformCommission?)null);

        var handler = CreateHandler();
        var command = new UpdatePlatformCommissionRateCommand(commissionId, Rate: 0.20m);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DeactivateCommission_ShouldSetIsActiveFalse()
    {
        // Arrange
        var commissionId = Guid.NewGuid();
        var existing = new PlatformCommission { IsActive = true };

        _repo.Setup(r => r.GetByIdAsync(commissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var handler = CreateHandler();
        var command = new UpdatePlatformCommissionRateCommand(commissionId, IsActive: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        existing.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

#endregion

#region GetPlatformCommissionRatesHandler

[Trait("Category", "Unit")]
public class GetPlatformCommissionRatesHandlerTests
{
    private readonly Mock<IPlatformCommissionRepository> _repo = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private GetPlatformCommissionRatesHandler CreateHandler() => new(_repo.Object);

    [Fact]
    public async Task Handle_NoCommissions_ShouldReturnEmptyList()
    {
        // Arrange
        _repo.Setup(r => r.GetByPlatformAsync(_tenantId, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlatformCommission>().AsReadOnly());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetPlatformCommissionRatesQuery(_tenantId), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithCommissions_ShouldMapToDto()
    {
        // Arrange
        var commission = new PlatformCommission
        {
            TenantId = _tenantId,
            Platform = PlatformType.Trendyol,
            Type = CommissionType.Percentage,
            Rate = 0.12m,
            Currency = "TRY",
            IsActive = true,
            EffectiveFrom = new DateTime(2026, 1, 1),
        };

        _repo.Setup(r => r.GetByPlatformAsync(_tenantId, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlatformCommission> { commission }.AsReadOnly());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetPlatformCommissionRatesQuery(_tenantId), CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Platform.Should().Be("Trendyol");
        result[0].Rate.Should().Be(0.12m);
        result[0].IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

#endregion

#region GetReconciliationDashboardHandler

[Trait("Category", "Unit")]
public class GetReconciliationDashboardHandlerTests
{
    private readonly Mock<IReconciliationMatchRepository> _matchRepo = new();
    private readonly Mock<ISettlementBatchRepository> _settlementRepo = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private GetReconciliationDashboardHandler CreateHandler() =>
        new(_matchRepo.Object, _settlementRepo.Object);

    [Fact]
    public async Task Handle_NoMatches_ShouldReturnZeroCounts()
    {
        // Arrange
        _matchRepo.Setup(r => r.GetByStatusAsync(_tenantId, It.IsAny<ReconciliationStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReconciliationMatch>().AsReadOnly());
        _settlementRepo.Setup(r => r.GetUnmatchedAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementBatch>().AsReadOnly());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetReconciliationDashboardQuery(_tenantId), CancellationToken.None);

        // Assert
        result.AutoMatchedCount.Should().Be(0);
        result.NeedsReviewCount.Should().Be(0);
        result.UnmatchedCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithUnmatchedSettlements_ShouldCalculateTotal()
    {
        // Arrange
        _matchRepo.Setup(r => r.GetByStatusAsync(_tenantId, It.IsAny<ReconciliationStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReconciliationMatch>().AsReadOnly());

        var settlements = new List<SettlementBatch>
        {
            SettlementBatch.Create(Guid.NewGuid(), "Trendyol", DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, 1200m, 200m, 1000m),
            SettlementBatch.Create(Guid.NewGuid(), "Hepsiburada", DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, 2400m, 400m, 2000m),
        }.AsReadOnly();
        _settlementRepo.Setup(r => r.GetUnmatchedAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settlements);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetReconciliationDashboardQuery(_tenantId), CancellationToken.None);

        // Assert
        result.UnmatchedCount.Should().Be(2);
        result.UnmatchedTotal.Should().Be(3000m);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

#endregion
