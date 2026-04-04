using FluentAssertions;
using MesTech.Application.Queries.GetKarZarar;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Queries;

/// <summary>
/// DEV5: GetKarZararHandler testi — Kâr/Zarar hesaplama.
/// P1 iş-kritik: muhasebe raporunun doğruluğu.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetKarZararHandlerTests
{
    private readonly Mock<IIncomeRepository> _incomeRepo = new();
    private readonly Mock<IExpenseRepository> _expenseRepo = new();

    private GetKarZararHandler CreateSut() => new(_incomeRepo.Object, _expenseRepo.Object);

    [Fact]
    public async Task Handle_NoData_ShouldReturnZeros()
    {
        _incomeRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Income>());
        _expenseRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Expense>());

        var query = new GetKarZararQuery(DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);
        var result = await CreateSut().Handle(query, CancellationToken.None);

        result.ToplamGelir.Should().Be(0);
        result.ToplamGider.Should().Be(0);
        result.NetKar.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithData_ShouldCalculateCorrectNetProfit()
    {
        var income1 = new Income { TenantId = Guid.NewGuid() };
        income1.SetAmount(5000m);
        var income2 = new Income { TenantId = Guid.NewGuid() };
        income2.SetAmount(3000m);

        var expense1 = new Expense { TenantId = Guid.NewGuid() };
        expense1.SetAmount(2000m);

        _incomeRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Income> { income1, income2 });
        _expenseRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Expense> { expense1 });

        var query = new GetKarZararQuery(DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);
        var result = await CreateSut().Handle(query, CancellationToken.None);

        result.ToplamGelir.Should().Be(8000m);
        result.ToplamGider.Should().Be(2000m);
        result.NetKar.Should().Be(6000m);
    }

    [Fact]
    public async Task Handle_Loss_ShouldReturnNegativeNetKar()
    {
        var income = new Income { TenantId = Guid.NewGuid() };
        income.SetAmount(1000m);

        var expense = new Expense { TenantId = Guid.NewGuid() };
        expense.SetAmount(5000m);

        _incomeRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Income> { income });
        _expenseRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Expense> { expense });

        var query = new GetKarZararQuery(DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);
        var result = await CreateSut().Handle(query, CancellationToken.None);

        result.NetKar.Should().Be(-4000m);
    }

    [Fact]
    public async Task Handle_ShouldPassDateRangeToRepositories()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc);
        var tenantId = Guid.NewGuid();

        _incomeRepo.Setup(r => r.GetByDateRangeAsync(from, to, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Income>());
        _expenseRepo.Setup(r => r.GetByDateRangeAsync(from, to, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Expense>());

        var query = new GetKarZararQuery(from, to, tenantId);
        await CreateSut().Handle(query, CancellationToken.None);

        _incomeRepo.Verify(r => r.GetByDateRangeAsync(from, to, tenantId, It.IsAny<CancellationToken>()), Times.Once);
        _expenseRepo.Verify(r => r.GetByDateRangeAsync(from, to, tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSetDateRange()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        _incomeRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Income>());
        _expenseRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Expense>());

        var query = new GetKarZararQuery(from, to);
        var result = await CreateSut().Handle(query, CancellationToken.None);

        result.DönemBasi.Should().Be(from);
        result.DönemSonu.Should().Be(to);
    }
}
