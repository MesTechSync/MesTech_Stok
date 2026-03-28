using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateExpense;
using MesTech.Application.Features.Accounting.Commands.DeleteExpense;
using MesTech.Application.Features.Accounting.Commands.UpdateExpense;
using MesTech.Application.Features.Accounting.Queries.GetExpenseById;
using MesTech.Application.Features.Accounting.Queries.GetExpenses;
using MesTech.Application.Features.Accounting.Commands.CreateIncome;
using MesTech.Application.Features.Accounting.Commands.DeleteIncome;
using MesTech.Application.Features.Accounting.Commands.UpdateIncome;
using MesTech.Application.Features.Accounting.Queries.GetIncomeById;
using MesTech.Application.Features.Accounting.Queries.GetIncomes;
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

    // ═══ EXPENSE ═══

    [Fact]
    public async Task CreateExpense_NullRequest_Throws()
    {
        var repo = new Mock<IExpenseRepository>();
        var handler = new CreateExpenseHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteExpense_NullRequest_Throws()
    {
        var repo = new Mock<IExpenseRepository>();
        var handler = new DeleteExpenseHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateExpense_NullRequest_Throws()
    {
        var repo = new Mock<IExpenseRepository>();
        var handler = new UpdateExpenseHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetExpenseById_NullRequest_Throws()
    {
        var repo = new Mock<IExpenseRepository>();
        var handler = new GetExpenseByIdHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetExpenses_NullRequest_Throws()
    {
        var repo = new Mock<IExpenseRepository>();
        var handler = new GetExpensesHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    // ═══ INCOME ═══

    [Fact]
    public async Task CreateIncome_NullRequest_Throws()
    {
        var repo = new Mock<IIncomeRepository>();
        var handler = new CreateIncomeHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteIncome_NullRequest_Throws()
    {
        var repo = new Mock<IIncomeRepository>();
        var handler = new DeleteIncomeHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateIncome_NullRequest_Throws()
    {
        var repo = new Mock<IIncomeRepository>();
        var handler = new UpdateIncomeHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetIncomeById_NullRequest_Throws()
    {
        var repo = new Mock<IIncomeRepository>();
        var handler = new GetIncomeByIdHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetIncomes_NullRequest_Throws()
    {
        var repo = new Mock<IIncomeRepository>();
        var handler = new GetIncomesHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

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
