using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.ImportSettlement;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Handlers;

/// <summary>
/// ImportSettlementHandler tests — settlement batch creation with lines.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class ImportSettlementHandlerTests
{
    private readonly Mock<ISettlementBatchRepository> _repoMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly ImportSettlementHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ImportSettlementHandlerTests()
    {
        _repoMock = new Mock<ISettlementBatchRepository>();
        _uowMock = new Mock<IUnitOfWork>();
        _sut = new ImportSettlementHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommandWithLines_ReturnsNonEmptyGuidAndCallsAddAsync()
    {
        // Arrange
        var periodStart = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc);

        var lines = new List<SettlementLineInput>
        {
            new(OrderId: "ORD-001", GrossAmount: 500m, CommissionAmount: 50m,
                ServiceFee: 5m, CargoDeduction: 10m, RefundDeduction: 0m, NetAmount: 435m),
            new(OrderId: "ORD-002", GrossAmount: 300m, CommissionAmount: 30m,
                ServiceFee: 3m, CargoDeduction: 10m, RefundDeduction: 0m, NetAmount: 257m)
        };

        var command = new ImportSettlementCommand(
            TenantId: _tenantId,
            Platform: "Trendyol",
            PeriodStart: periodStart,
            PeriodEnd: periodEnd,
            TotalGross: 800m,
            TotalCommission: 80m,
            TotalNet: 692m,
            Lines: lines);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);

        _repoMock.Verify(
            r => r.AddAsync(
                It.Is<SettlementBatch>(b =>
                    b.TenantId == _tenantId &&
                    b.Platform == "Trendyol" &&
                    b.Lines.Count == 2),
                It.IsAny<CancellationToken>()),
            Times.Once());

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_EmptyPlatform_ThrowsArgumentException()
    {
        // Arrange
        var periodStart = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc);

        var command = new ImportSettlementCommand(
            TenantId: _tenantId,
            Platform: "",
            PeriodStart: periodStart,
            PeriodEnd: periodEnd,
            TotalGross: 100m,
            TotalCommission: 10m,
            TotalNet: 90m,
            Lines: new List<SettlementLineInput>());

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();

        _repoMock.Verify(
            r => r.AddAsync(It.IsAny<SettlementBatch>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }
}
