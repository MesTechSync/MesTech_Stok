using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Tests.Unit.Domain.Finance;

/// <summary>
/// Expense entity unit testleri — OnMuhasebe modulu icin basit gider kaydı.
/// DEV 5 — H27-5.3/5.6 (gercek entity: MesTech.Domain.Entities.Expense, basit setter bazlı)
///
/// NOT: Emirname'nin hedefledigi MesTech.Domain.Entities.Finance.Expense (DDD state machine)
/// DEV-1 H27 kapsaminda olusturulacak. O entity geldiginde bu testler Finance namespace'e
/// tasinacak ve Submit/Approve/Reject testleri de eklenecek.
/// Simdilik mevcut basit Expense entity'si test edilmektedir.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Domain", "Finance")]
public class ExpenseTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_ValidExpense_ShouldSetDescription()
    {
        var expense = new Expense
        {
            TenantId = _tenantId,
            Description = "Kargo Gideri",
            Amount = 150m,
            ExpenseType = ExpenseType.Kargo,
            Date = DateTime.Today
        };

        expense.Description.Should().Be("Kargo Gideri");
        expense.Amount.Should().Be(150m);
    }

    [Fact]
    public void Expense_TenantId_ShouldBeSetCorrectly()
    {
        var expense = new Expense { TenantId = _tenantId };

        expense.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public void Expense_IsRecurring_DefaultShouldBeFalse()
    {
        var expense = new Expense();

        expense.IsRecurring.Should().BeFalse();
    }

    [Fact]
    public void Expense_WithRecurringPeriod_ShouldPreserve()
    {
        var expense = new Expense
        {
            IsRecurring = true,
            RecurrencePeriod = "MONTHLY"
        };

        expense.IsRecurring.Should().BeTrue();
        expense.RecurrencePeriod.Should().Be("MONTHLY");
    }

    [Fact]
    public void Expense_ExpenseType_ShouldBeSetable()
    {
        var expense = new Expense { ExpenseType = ExpenseType.Reklam };

        expense.ExpenseType.Should().Be(ExpenseType.Reklam);
    }

    [Fact]
    public void Expense_WithNote_ShouldPreserve()
    {
        var expense = new Expense { Note = "Trendyol reklam faturasi" };

        expense.Note.Should().Be("Trendyol reklam faturasi");
    }
}
