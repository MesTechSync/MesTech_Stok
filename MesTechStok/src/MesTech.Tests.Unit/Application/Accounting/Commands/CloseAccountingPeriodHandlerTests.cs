using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CloseAccountingPeriod;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class CloseAccountingPeriodHandlerTests
{
    private readonly Mock<IAccountingPeriodRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<CloseAccountingPeriodHandler>> _loggerMock = new();
    private readonly CloseAccountingPeriodHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public CloseAccountingPeriodHandlerTests()
    {
        _sut = new CloseAccountingPeriodHandler(
            _repoMock.Object, _uowMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingPeriod_ClosesAndReturnsId()
    {
        // Arrange
        var period = AccountingPeriod.Create(TenantId, 2026, 3);
        _repoMock
            .Setup(r => r.GetByYearMonthAsync(TenantId, 2026, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(period);

        var command = new CloseAccountingPeriodCommand(TenantId, 2026, 3, "admin");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(period.Id);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<AccountingPeriod>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NoPeriodExists_CreatesNewPeriodAndCloses()
    {
        // Arrange
        _repoMock
            .Setup(r => r.GetByYearMonthAsync(TenantId, 2026, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountingPeriod?)null);

        var command = new CloseAccountingPeriodCommand(TenantId, 2026, 1, "admin");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<AccountingPeriod>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsNonEmptyGuid()
    {
        // Arrange
        _repoMock
            .Setup(r => r.GetByYearMonthAsync(TenantId, 2025, 12, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountingPeriod?)null);

        var command = new CloseAccountingPeriodCommand(TenantId, 2025, 12, "user-1");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);
    }
}
