using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreatePlatformCommissionRate;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class CreatePlatformCommissionRateHandlerTests
{
    private readonly Mock<IPlatformCommissionRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreatePlatformCommissionRateHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public CreatePlatformCommissionRateHandlerTests()
    {
        _sut = new CreatePlatformCommissionRateHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateAndReturnId()
    {
        // Arrange
        var command = new CreatePlatformCommissionRateCommand(
            TenantId, PlatformType.Trendyol, 0.15m, CommissionType.Percentage, "Elektronik");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<PlatformCommission>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_FixedAmount_ShouldCreateSuccessfully()
    {
        // Arrange
        var command = new CreatePlatformCommissionRateCommand(
            TenantId, PlatformType.Hepsiburada, 5.0m, CommissionType.FixedAmount,
            MinAmount: 0m, MaxAmount: 1000m);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
