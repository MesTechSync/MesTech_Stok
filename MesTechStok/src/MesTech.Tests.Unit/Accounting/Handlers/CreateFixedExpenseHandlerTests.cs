using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateFixedExpense;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Handlers;

/// <summary>
/// CreateFixedExpenseHandler tests — valid expense creation and negative amount rejection.
/// </summary>
[Trait("Category", "Unit")]
public class CreateFixedExpenseHandlerTests
{
    private readonly Mock<IFixedExpenseRepository> _repoMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly CreateFixedExpenseHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public CreateFixedExpenseHandlerTests()
    {
        _repoMock = new Mock<IFixedExpenseRepository>();
        _uowMock = new Mock<IUnitOfWork>();

        _sut = new CreateFixedExpenseHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsGuidAndPersists()
    {
        // Arrange
        var command = new CreateFixedExpenseCommand(
            _tenantId,
            "Ofis Kirasi",
            15000m,
            1,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            "TRY",
            null,
            "ABC Ltd.",
            Guid.NewGuid(),
            "Aylik kira bedeli");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);

        _repoMock.Verify(
            r => r.AddAsync(It.Is<FixedExpense>(e =>
                e.Name == "Ofis Kirasi" && e.MonthlyAmount == 15000m && e.DayOfMonth == 1),
                It.IsAny<CancellationToken>()),
            Times.Once());

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_NegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var command = new CreateFixedExpenseCommand(
            _tenantId,
            "Negatif Gider",
            -500m,
            15,
            DateTime.UtcNow);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*Monthly amount must be positive*");

        _repoMock.Verify(
            r => r.AddAsync(It.IsAny<FixedExpense>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_ZeroAmount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var command = new CreateFixedExpenseCommand(
            _tenantId,
            "Sifir Gider",
            0m,
            1,
            DateTime.UtcNow);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task Handle_InvalidDayOfMonth_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var command = new CreateFixedExpenseCommand(
            _tenantId,
            "Gecersiz Gun",
            1000m,
            32, // invalid day
            DateTime.UtcNow);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*Day of month must be between 1 and 31*");
    }
}
