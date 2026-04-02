using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.RecordCommission;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using ICommissionRecordRepository = MesTech.Application.Interfaces.Accounting.ICommissionRecordRepository;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

/// <summary>
/// RecordCommissionHandler: Z6 komisyon→GL zinciri.
/// Kritik iş kuralları:
///   - Platform adı boş olamaz (domain guard)
///   - GrossAmount ve CommissionRate negatif olamaz (domain guard)
///   - CommissionAmount > 0 ise CommissionChargedEvent fırlatır
///   - NetAmount = GrossAmount - CommissionAmount - ServiceFee
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "AccountingChain")]
public class RecordCommissionHandlerTests
{
    private readonly Mock<ICommissionRecordRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    public RecordCommissionHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _repo.Setup(r => r.AddAsync(It.IsAny<CommissionRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private RecordCommissionHandler CreateHandler() => new(_repo.Object, _uow.Object);

    [Fact]
    public async Task Handle_ValidCommission_PersistsAndReturnsGuid()
    {
        var cmd = new RecordCommissionCommand(
            TenantId: Guid.NewGuid(),
            Platform: "Trendyol",
            GrossAmount: 1000m,
            CommissionRate: 0.15m,
            CommissionAmount: 150m,
            ServiceFee: 5m,
            OrderId: "ORD-001",
            Category: "Elektronik");

        var handler = CreateHandler();
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        _repo.Verify(r => r.AddAsync(It.IsAny<CommissionRecord>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyPlatform_ThrowsArgumentException()
    {
        // Domain guard: platform boş olamaz
        var cmd = new RecordCommissionCommand(
            Guid.NewGuid(), Platform: "", GrossAmount: 100m,
            CommissionRate: 0.1m, CommissionAmount: 10m, ServiceFee: 0m);

        var handler = CreateHandler();

        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NegativeGrossAmount_ThrowsArgumentOutOfRange()
    {
        var cmd = new RecordCommissionCommand(
            Guid.NewGuid(), Platform: "HepsiBurada", GrossAmount: -100m,
            CommissionRate: 0.1m, CommissionAmount: 10m, ServiceFee: 0m);

        var handler = CreateHandler();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NegativeCommissionRate_ThrowsArgumentOutOfRange()
    {
        var cmd = new RecordCommissionCommand(
            Guid.NewGuid(), Platform: "N11", GrossAmount: 100m,
            CommissionRate: -0.05m, CommissionAmount: 10m, ServiceFee: 0m);

        var handler = CreateHandler();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NetAmountCalculation_IsCorrect()
    {
        // Arrange — capture the persisted record
        CommissionRecord? captured = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<CommissionRecord>(), It.IsAny<CancellationToken>()))
            .Callback<CommissionRecord, CancellationToken>((cr, _) => captured = cr)
            .Returns(Task.CompletedTask);

        var cmd = new RecordCommissionCommand(
            Guid.NewGuid(), "Trendyol", GrossAmount: 1000m,
            CommissionRate: 0.15m, CommissionAmount: 150m, ServiceFee: 25m);

        var handler = CreateHandler();
        await handler.Handle(cmd, CancellationToken.None);

        // Assert — NetAmount = 1000 - 150 - 25 = 825
        captured.Should().NotBeNull();
        captured!.GetNetAmount().Should().Be(825m);
    }
}
