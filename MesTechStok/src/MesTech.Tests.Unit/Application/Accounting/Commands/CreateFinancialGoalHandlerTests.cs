using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateFinancialGoal;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class CreateFinancialGoalHandlerTests
{
    private readonly Mock<IFinancialGoalRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateFinancialGoalHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public CreateFinancialGoalHandlerTests()
    {
        _sut = new CreateFinancialGoalHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidGoal_CreatesAndReturnsId()
    {
        // Arrange
        var command = new CreateFinancialGoalCommand(
            TenantId, "Aylik Gelir Hedefi",
            100_000m,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _repoMock.Verify(r => r.AddAsync(
            It.IsAny<FinancialGoal>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_ShortTermGoal_CreatesSuccessfully()
    {
        // Arrange
        var command = new CreateFinancialGoalCommand(
            TenantId, "Haftalik Satis Hedefi",
            10_000m,
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 7, 0, 0, 0, DateTimeKind.Utc));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);
    }
}
