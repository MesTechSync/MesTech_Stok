using FluentAssertions;
using MesTech.Application.Features.Finance.Queries.GetCashFlow;
using MesTech.Application.Features.Finance.Queries.GetCashRegisters;
using MesTech.Application.Features.Accounting.Queries.GetBankTransactions;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Interfaces;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Tests for finance query handlers — CashFlow, CashRegisters, BankTransactions.
/// </summary>
[Trait("Category", "Unit")]
public class FinanceCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    // ══��════ GetCashFlowHandler ��══════

    [Fact]
    public async Task GetCashFlow_NullRequest_ThrowsArgumentNullException()
    {
        var expRepo = new Mock<IFinanceExpenseRepository>();
        var orderRepo = new Mock<IOrderRepository>();
        var sut = new GetCashFlowHandler(expRepo.Object, orderRepo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    // ═══════ GetCashRegistersHandler ═══════

    [Fact]
    public async Task GetCashRegisters_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<ICashRegisterRepository>();
        var sut = new GetCashRegistersHandler(repo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetCashRegisters_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<ICashRegisterRepository>();
        repo.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CashRegister>());

        var sut = new GetCashRegistersHandler(repo.Object);
        var result = await sut.Handle(
            new GetCashRegistersQuery(_tenantId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ═══════ GetBankTransactionsHandler ═══════

    [Fact]
    public async Task GetBankTransactions_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IBankTransactionRepository>();
        var sut = new GetBankTransactionsHandler(repo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetBankTransactions_EmptyRepo_ReturnsEmptyList()
    {
        var bankAccountId = Guid.NewGuid();
        var repo = new Mock<IBankTransactionRepository>();
        repo.Setup(r => r.GetByBankAccountAsync(
                _tenantId, bankAccountId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BankTransaction>());

        var sut = new GetBankTransactionsHandler(repo.Object);
        var result = await sut.Handle(
            new GetBankTransactionsQuery(_tenantId, bankAccountId), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
