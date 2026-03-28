using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateAccountingExpense;
using MesTech.Application.Features.Accounting.Commands.CreateFixedExpense;
using MesTech.Application.Features.Accounting.Commands.DeleteFixedExpense;
using MesTech.Application.Features.Accounting.Commands.UpdateFixedExpense;
using MesTech.Application.Features.Accounting.Commands.RecordCargoExpense;
using MesTech.Application.Features.Accounting.Queries.GetFixedExpenseById;
using MesTech.Application.Features.Finance.Commands.ApproveExpense;
using MesTech.Application.Features.Finance.Commands.MarkExpensePaid;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
[Trait("Group", "ExpenseIncome")]
public class ExpenseIncomeHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();

    public ExpenseIncomeHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    // Removed: CreateExpense, DeleteExpense, UpdateExpense, GetExpenseById, GetExpenses
    //   — handlers merged into GetIncomeExpenseList / removed
    // Removed: CreateIncome, DeleteIncome, UpdateIncome, GetIncomeById, GetIncomes
    //   — handlers merged into GetIncomeExpenseList / removed

    // ═══ ACCOUNTING EXPENSE ═══

    [Fact]
    public async Task CreateAccountingExpense_NullRequest_Throws()
    {
        var repo = new Mock<IPersonalExpenseRepository>();
        var handler = new CreateAccountingExpenseHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CreateFixedExpense_NullRequest_Throws()
    {
        var repo = new Mock<IFixedExpenseRepository>();
        var handler = new CreateFixedExpenseHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteFixedExpense_NullRequest_Throws()
    {
        var repo = new Mock<IFixedExpenseRepository>();
        var handler = new DeleteFixedExpenseHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateFixedExpense_NullRequest_Throws()
    {
        var repo = new Mock<IFixedExpenseRepository>();
        var handler = new UpdateFixedExpenseHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task RecordCargoExpense_NullRequest_Throws()
    {
        var repo = new Mock<ICargoExpenseRepository>();
        var handler = new RecordCargoExpenseHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetFixedExpenseById_NullRequest_Throws()
    {
        var repo = new Mock<IFixedExpenseRepository>();
        var handler = new GetFixedExpenseByIdHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    // ═══ FINANCE EXPENSE ═══

    [Fact]
    public async Task ApproveExpense_NullRequest_Throws()
    {
        var repo = new Mock<IFinanceExpenseRepository>();
        var handler = new ApproveExpenseHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task MarkExpensePaid_NullRequest_Throws()
    {
        var repo = new Mock<IFinanceExpenseRepository>();
        var handler = new MarkExpensePaidHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }
}
