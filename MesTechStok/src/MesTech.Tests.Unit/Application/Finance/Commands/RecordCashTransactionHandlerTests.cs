using FluentAssertions;
using MesTech.Application.Features.Finance.Commands.RecordCashTransaction;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Finance.Commands;

[Trait("Category", "Unit")]
public class RecordCashTransactionHandlerTests
{
    private readonly Mock<ICashRegisterRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly RecordCashTransactionHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public RecordCashTransactionHandlerTests()
    {
        _sut = new RecordCashTransactionHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_IncomeTransaction_ShouldReturnTransactionId()
    {
        // Arrange
        var cashRegister = CashRegister.Create(TenantId, "Ana Kasa", "TRY", true, 1000m);
        _repoMock.Setup(r => r.GetByIdAsync(cashRegister.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cashRegister);
        var command = new RecordCashTransactionCommand(
            TenantId, cashRegister.Id, CashTransactionType.Income, 500m, "Satis geliri", "Satis");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        cashRegister.Balance.Should().Be(1500m);
        _repoMock.Verify(r => r.UpdateAsync(cashRegister, It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExpenseTransaction_ShouldDecreaseBalance()
    {
        // Arrange
        var cashRegister = CashRegister.Create(TenantId, "Ana Kasa", "TRY", true, 2000m);
        _repoMock.Setup(r => r.GetByIdAsync(cashRegister.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cashRegister);
        var command = new RecordCashTransactionCommand(
            TenantId, cashRegister.Id, CashTransactionType.Expense, 300m, "Fatura odemesi");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        cashRegister.Balance.Should().Be(1700m);
    }

    [Fact]
    public async Task Handle_NonExistentRegister_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CashRegister?)null);
        var command = new RecordCashTransactionCommand(
            TenantId, Guid.NewGuid(), CashTransactionType.Income, 100m, "Test");

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*bulunamadi*");
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
