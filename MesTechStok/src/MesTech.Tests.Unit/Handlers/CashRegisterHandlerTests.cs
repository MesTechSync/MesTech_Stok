using FluentAssertions;
using MediatR;
using MesTech.Application.Features.Accounting.Commands.CreateAccountingBankAccount;
using MesTech.Application.Features.Accounting.Commands.CreateAccountingExpense;
using MesTech.Application.Features.Finance.Commands.CloseCashRegister;
using MesTech.Application.Features.Finance.Commands.CreateCashRegister;
using MesTech.Application.Features.Finance.Commands.MarkExpensePaid;
using MesTech.Application.Features.Finance.Commands.RecordCashTransaction;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class CashRegisterHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    // ── CreateCashRegisterHandler ──────────────────────────────

    [Fact]
    public async Task CreateCashRegister_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<ICashRegisterRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreateCashRegisterHandler(repo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CreateCashRegister_ValidCommand_ReturnsNonEmptyGuid()
    {
        var repo = new Mock<ICashRegisterRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreateCashRegisterHandler(repo.Object, uow.Object);

        var command = new CreateCashRegisterCommand(_tenantId, "Ana Kasa", "TRY", true, 1000m);
        var result = await sut.Handle(command, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
        repo.Verify(r => r.AddAsync(It.IsAny<CashRegister>(), It.IsAny<CancellationToken>()), Times.Once());
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task CreateCashRegister_DefaultValues_ReturnsGuid()
    {
        var repo = new Mock<ICashRegisterRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreateCashRegisterHandler(repo.Object, uow.Object);

        var command = new CreateCashRegisterCommand(_tenantId, "Yedek Kasa");
        var result = await sut.Handle(command, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
    }

    // ── CloseCashRegisterHandler ───────────────────────────────

    [Fact]
    public async Task CloseCashRegister_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<ICashRegisterRepository>();
        var uow = new Mock<IUnitOfWork>();
        var logger = NullLogger<CloseCashRegisterHandler>.Instance;
        var sut = new CloseCashRegisterHandler(repo.Object, uow.Object, logger);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CloseCashRegister_CashRegisterNotFound_ThrowsInvalidOperation()
    {
        var repo = new Mock<ICashRegisterRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CashRegister?)null);
        var uow = new Mock<IUnitOfWork>();
        var logger = NullLogger<CloseCashRegisterHandler>.Instance;
        var sut = new CloseCashRegisterHandler(repo.Object, uow.Object, logger);

        var command = new CloseCashRegisterCommand(_tenantId, Guid.NewGuid(), DateTime.UtcNow, 500m);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(command, CancellationToken.None));
    }

    // ── RecordCashTransactionHandler ───────────────────────────

    [Fact]
    public async Task RecordCashTransaction_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<ICashRegisterRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new RecordCashTransactionHandler(repo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task RecordCashTransaction_CashRegisterNotFound_ThrowsInvalidOperation()
    {
        var repo = new Mock<ICashRegisterRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CashRegister?)null);
        var uow = new Mock<IUnitOfWork>();
        var sut = new RecordCashTransactionHandler(repo.Object, uow.Object);

        var command = new RecordCashTransactionCommand(
            _tenantId, Guid.NewGuid(), CashTransactionType.Income, 100m, "Test gelir");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(command, CancellationToken.None));
    }

    // ── CreateAccountingBankAccountHandler ─────────────────────

    [Fact]
    public async Task CreateAccountingBankAccount_NullRequest_ThrowsArgumentNullException()
    {
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreateAccountingBankAccountHandler(uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAccountingBankAccount_ValidCommand_CallsSaveChanges()
    {
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreateAccountingBankAccountHandler(uow.Object);

        var command = new CreateAccountingBankAccountCommand(
            _tenantId, "Is Bankasi TRY", "TRY", "Is Bankasi", "TR000000000000000000000001");
        var result = await sut.Handle(command, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task CreateAccountingBankAccount_DefaultCurrency_UseTRY()
    {
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreateAccountingBankAccountHandler(uow.Object);

        var command = new CreateAccountingBankAccountCommand(_tenantId, "Garanti TRY");
        var result = await sut.Handle(command, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
    }

    // ── CreateAccountingExpenseHandler ──────────────────────────

    [Fact]
    public async Task CreateAccountingExpense_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IPersonalExpenseRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreateAccountingExpenseHandler(repo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAccountingExpense_ValidCommand_ReturnsNonEmptyGuid()
    {
        var repo = new Mock<IPersonalExpenseRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreateAccountingExpenseHandler(repo.Object, uow.Object);

        var command = new CreateAccountingExpenseCommand(
            _tenantId, "Ofis kirasi", 5000m, DateTime.UtcNow, ExpenseSource.Manual, "Kira");
        var result = await sut.Handle(command, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
        repo.Verify(r => r.AddAsync(It.IsAny<MesTech.Domain.Accounting.Entities.PersonalExpense>(), It.IsAny<CancellationToken>()), Times.Once());
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    // ── MarkExpensePaidHandler ─────────────────────────────────

    [Fact]
    public async Task MarkExpensePaid_ExpenseNotFound_ThrowsInvalidOperation()
    {
        var repo = new Mock<IFinanceExpenseRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FinanceExpense?)null);
        var uow = new Mock<IUnitOfWork>();
        var sut = new MarkExpensePaidHandler(repo.Object, uow.Object);

        var command = new MarkExpensePaidCommand(Guid.NewGuid(), Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task MarkExpensePaid_ValidExpense_SavesAndReturnsUnit()
    {
        var expense = FinanceExpense.Create(
            Guid.NewGuid(), "Test gider", 100m,
            MesTech.Domain.Enums.ExpenseCategory.Software, DateTime.UtcNow);
        expense.Submit();
        expense.Approve(Guid.NewGuid());
        var repo = new Mock<IFinanceExpenseRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expense);
        var uow = new Mock<IUnitOfWork>();
        var sut = new MarkExpensePaidHandler(repo.Object, uow.Object);

        var command = new MarkExpensePaidCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = await sut.Handle(command, CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }
}
