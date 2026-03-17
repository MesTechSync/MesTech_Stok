using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreatePenaltyRecord;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Handlers;

/// <summary>
/// CreatePenaltyRecordHandler tests — valid penalty creation and negative amount rejection.
/// </summary>
[Trait("Category", "Unit")]
public class CreatePenaltyRecordHandlerTests
{
    private readonly Mock<IPenaltyRecordRepository> _repoMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly CreatePenaltyRecordHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public CreatePenaltyRecordHandlerTests()
    {
        _repoMock = new Mock<IPenaltyRecordRepository>();
        _uowMock = new Mock<IUnitOfWork>();

        _sut = new CreatePenaltyRecordHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsGuidAndPersists()
    {
        // Arrange
        var command = new CreatePenaltyRecordCommand(
            _tenantId,
            PenaltySource.Trendyol,
            "Gec gonderim cezasi",
            250m,
            new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc),
            "PEN-001",
            Guid.NewGuid(),
            "TRY",
            "3 gun gecikme");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);

        _repoMock.Verify(
            r => r.AddAsync(It.Is<PenaltyRecord>(p =>
                p.Source == PenaltySource.Trendyol &&
                p.Description == "Gec gonderim cezasi" &&
                p.Amount == 250m),
                It.IsAny<CancellationToken>()),
            Times.Once());

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_NegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var command = new CreatePenaltyRecordCommand(
            _tenantId,
            PenaltySource.Hepsiburada,
            "Negatif ceza",
            -100m,
            DateTime.UtcNow);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*Amount must be positive*");

        _repoMock.Verify(
            r => r.AddAsync(It.IsAny<PenaltyRecord>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_ZeroAmount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var command = new CreatePenaltyRecordCommand(
            _tenantId,
            PenaltySource.N11,
            "Sifir ceza",
            0m,
            DateTime.UtcNow);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task Handle_EmptyDescription_ThrowsArgumentException()
    {
        // Arrange
        var command = new CreatePenaltyRecordCommand(
            _tenantId,
            PenaltySource.TaxAuthority,
            "  ",  // whitespace description
            500m,
            DateTime.UtcNow);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
