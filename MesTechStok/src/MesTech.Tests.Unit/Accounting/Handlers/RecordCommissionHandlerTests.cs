using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.RecordCommission;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Handlers;

/// <summary>
/// RecordCommissionHandler tests — valid commission recording and invalid rate rejection.
/// </summary>
[Trait("Category", "Unit")]
public class RecordCommissionHandlerTests
{
    private readonly Mock<ICommissionRecordRepository> _repoMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly RecordCommissionHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public RecordCommissionHandlerTests()
    {
        _repoMock = new Mock<ICommissionRecordRepository>();
        _uowMock = new Mock<IUnitOfWork>();

        _sut = new RecordCommissionHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsGuidAndPersists()
    {
        // Arrange
        var command = new RecordCommissionCommand(
            _tenantId,
            "Trendyol",
            1000m,    // gross
            0.15m,    // rate
            150m,     // commission amount
            10m,      // service fee
            "ORD-12345",
            "Elektronik");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);

        _repoMock.Verify(
            r => r.AddAsync(It.Is<CommissionRecord>(c =>
                c.Platform == "Trendyol" &&
                c.GrossAmount == 1000m &&
                c.CommissionRate == 0.15m &&
                c.CommissionAmount == 150m),
                It.IsAny<CancellationToken>()),
            Times.Once());

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_NegativeCommissionRate_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var command = new RecordCommissionCommand(
            _tenantId,
            "Hepsiburada",
            1000m,
            -0.05m,   // negative rate
            50m,
            5m);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*Commission rate must be non-negative*");

        _repoMock.Verify(
            r => r.AddAsync(It.IsAny<CommissionRecord>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_NegativeGrossAmount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var command = new RecordCommissionCommand(
            _tenantId,
            "N11",
            -500m,    // negative gross
            0.10m,
            50m,
            5m);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*Gross amount must be non-negative*");
    }

    [Fact]
    public async Task Handle_EmptyPlatform_ThrowsArgumentException()
    {
        // Arrange
        var command = new RecordCommissionCommand(
            _tenantId,
            "  ",     // whitespace platform
            1000m,
            0.15m,
            150m,
            10m);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_ZeroGrossAmount_Succeeds()
    {
        // Arrange — zero gross is valid (non-negative)
        var command = new RecordCommissionCommand(
            _tenantId,
            "Amazon",
            0m,
            0m,
            0m,
            0m);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);

        _repoMock.Verify(
            r => r.AddAsync(It.IsAny<CommissionRecord>(), It.IsAny<CancellationToken>()),
            Times.Once());
    }
}
