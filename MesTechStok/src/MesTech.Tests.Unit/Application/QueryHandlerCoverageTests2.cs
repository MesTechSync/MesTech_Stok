using FluentAssertions;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Queries.GetCariHareketler;
using MesTech.Application.Queries.GetCariHesaplar;
using MesTech.Application.Queries.GetExpenseById;
using MesTech.Application.Queries.GetIncomeById;
using MesTech.Application.Queries.GetKarZarar;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application;

// ═══════════════════════════════════════════════════════════════
// DEV 5 — Handler test kapsam borcu kapatma Batch 2
// ═══════════════════════════════════════════════════════════════

#region GetExpenseByIdHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class GetExpenseByIdHandlerTests
{
    private readonly Mock<IExpenseRepository> _repoMock = new();
    private readonly GetExpenseByIdHandler _sut;

    public GetExpenseByIdHandlerTests()
    {
        _sut = new GetExpenseByIdHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingExpense_ReturnsDto()
    {
        var id = Guid.NewGuid();
        var expense = new Expense
        {
            TenantId = Guid.NewGuid(),
            Description = "Ofis kirası",
            ExpenseType = ExpenseType.Kira,
            Date = DateTime.UtcNow
        };
        expense.SetAmount(5000m);
        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(expense);

        var result = await _sut.Handle(new GetExpenseByIdQuery(id), CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_NonExistent_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Expense?)null);

        var result = await _sut.Handle(new GetExpenseByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }
}

#endregion

#region GetIncomeByIdHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class GetIncomeByIdHandlerTests
{
    private readonly Mock<IIncomeRepository> _repoMock = new();
    private readonly GetIncomeByIdHandler _sut;

    public GetIncomeByIdHandlerTests()
    {
        _sut = new GetIncomeByIdHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingIncome_ReturnsDto()
    {
        var id = Guid.NewGuid();
        var income = new Income
        {
            TenantId = Guid.NewGuid(),
            Description = "Trendyol satış",
            IncomeType = IncomeType.Satis,
            Date = DateTime.UtcNow
        };
        income.SetAmount(8000m);
        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(income);

        var result = await _sut.Handle(new GetIncomeByIdQuery(id), CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_NonExistent_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Income?)null);

        var result = await _sut.Handle(new GetIncomeByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }
}

#endregion

#region GetKarZararHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class GetKarZararHandlerTests
{
    private readonly Mock<IIncomeRepository> _incomeRepoMock = new();
    private readonly Mock<IExpenseRepository> _expenseRepoMock = new();
    private readonly GetKarZararHandler _sut;

    public GetKarZararHandlerTests()
    {
        _sut = new GetKarZararHandler(_incomeRepoMock.Object, _expenseRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WithData_CalculatesCorrectNetKar()
    {
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 3, 31);
        var tenantId = Guid.NewGuid();

        var income1 = new Income { TenantId = tenantId, IncomeType = IncomeType.Satis, Date = DateTime.UtcNow };
        income1.SetAmount(50_000m);
        var income2 = new Income { TenantId = tenantId, IncomeType = IncomeType.Diger, Date = DateTime.UtcNow };
        income2.SetAmount(10_000m);
        var incomes = new List<Income> { income1, income2 };

        var expense1 = new Expense { TenantId = tenantId, ExpenseType = ExpenseType.Kira, Date = DateTime.UtcNow };
        expense1.SetAmount(15_000m);
        var expense2 = new Expense { TenantId = tenantId, ExpenseType = ExpenseType.Diger, Date = DateTime.UtcNow };
        expense2.SetAmount(5_000m);
        var expenses = new List<Expense> { expense1, expense2 };

        _incomeRepoMock.Setup(r => r.GetByDateRangeAsync(from, to, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incomes.AsReadOnly());
        _expenseRepoMock.Setup(r => r.GetByDateRangeAsync(from, to, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expenses.AsReadOnly());

        var result = await _sut.Handle(
            new GetKarZararQuery(from, to, tenantId), CancellationToken.None);

        result.ToplamGelir.Should().Be(60_000m);
        result.ToplamGider.Should().Be(20_000m);
        result.NetKar.Should().Be(40_000m);
        result.DönemBasi.Should().Be(from);
        result.DönemSonu.Should().Be(to);
    }

    [Fact]
    public async Task Handle_NoData_ReturnsZeros()
    {
        var from = DateTime.UtcNow.AddMonths(-1);
        var to = DateTime.UtcNow;

        _incomeRepoMock.Setup(r => r.GetByDateRangeAsync(from, to, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Income>().AsReadOnly());
        _expenseRepoMock.Setup(r => r.GetByDateRangeAsync(from, to, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Expense>().AsReadOnly());

        var result = await _sut.Handle(new GetKarZararQuery(from, to), CancellationToken.None);

        result.ToplamGelir.Should().Be(0);
        result.ToplamGider.Should().Be(0);
        result.NetKar.Should().Be(0);
    }

    [Fact]
    public async Task Handle_LossScenario_NegativeNetKar()
    {
        var from = DateTime.UtcNow.AddMonths(-1);
        var to = DateTime.UtcNow;

        var income = new Income { IncomeType = IncomeType.Satis, Date = DateTime.UtcNow };
        income.SetAmount(10_000m);

        var expense = new Expense { ExpenseType = ExpenseType.Kira, Date = DateTime.UtcNow };
        expense.SetAmount(25_000m);

        _incomeRepoMock.Setup(r => r.GetByDateRangeAsync(from, to, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Income> { income }.AsReadOnly());
        _expenseRepoMock.Setup(r => r.GetByDateRangeAsync(from, to, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Expense> { expense }.AsReadOnly());

        var result = await _sut.Handle(new GetKarZararQuery(from, to), CancellationToken.None);

        result.NetKar.Should().BeNegative("gider gelirden fazla = zarar");
    }
}

#endregion

#region GetCariHesaplarHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class GetCariHesaplarHandlerTests
{
    private readonly Mock<ICariHesapRepository> _repoMock = new();
    private readonly GetCariHesaplarHandler _sut;

    public GetCariHesaplarHandlerTests()
    {
        _sut = new GetCariHesaplarHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_NoFilter_ReturnsAll()
    {
        var tenantId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetAllAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MesTech.Domain.Entities.CariHesap>().AsReadOnly());

        var result = await _sut.Handle(new GetCariHesaplarQuery(TenantId: tenantId), CancellationToken.None);

        result.Should().BeEmpty();
        _repoMock.Verify(r => r.GetAllAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region GetCariHareketlerHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class GetCariHareketlerHandlerTests
{
    private readonly Mock<ICariHareketRepository> _repoMock = new();
    private readonly GetCariHareketlerHandler _sut;

    public GetCariHareketlerHandlerTests()
    {
        _sut = new GetCariHareketlerHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_NoDateRange_ReturnsByCariHesapId()
    {
        var cariHesapId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByCariHesapIdAsync(cariHesapId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MesTech.Domain.Entities.CariHareket>().AsReadOnly());

        var result = await _sut.Handle(
            new GetCariHareketlerQuery(cariHesapId), CancellationToken.None);

        result.Should().BeEmpty();
        _repoMock.Verify(r => r.GetByCariHesapIdAsync(cariHesapId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDateRange_ReturnsByDateRange()
    {
        var cariHesapId = Guid.NewGuid();
        var from = DateTime.UtcNow.AddMonths(-1);
        var to = DateTime.UtcNow;

        _repoMock.Setup(r => r.GetByDateRangeAsync(cariHesapId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MesTech.Domain.Entities.CariHareket>().AsReadOnly());

        var result = await _sut.Handle(
            new GetCariHareketlerQuery(cariHesapId, from, to), CancellationToken.None);

        result.Should().BeEmpty();
        _repoMock.Verify(r => r.GetByDateRangeAsync(cariHesapId, from, to, It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

