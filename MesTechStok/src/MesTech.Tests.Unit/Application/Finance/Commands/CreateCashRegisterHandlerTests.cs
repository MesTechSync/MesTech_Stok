using FluentAssertions;
using MesTech.Application.Features.Finance.Commands.CreateCashRegister;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Finance.Commands;

[Trait("Category", "Unit")]
public class CreateCashRegisterHandlerTests
{
    private readonly Mock<ICashRegisterRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateCashRegisterHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public CreateCashRegisterHandlerTests()
    {
        _sut = new CreateCashRegisterHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateAndReturnId()
    {
        // Arrange
        var command = new CreateCashRegisterCommand(TenantId, "Ana Kasa");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<CashRegister>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithOpeningBalanceAndDefault_ShouldCreateSuccessfully()
    {
        // Arrange
        var command = new CreateCashRegisterCommand(
            TenantId, "USD Kasa", "USD", IsDefault: true, OpeningBalance: 5000m);

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
