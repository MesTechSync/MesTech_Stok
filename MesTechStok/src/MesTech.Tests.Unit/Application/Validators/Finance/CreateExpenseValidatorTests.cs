using FluentAssertions;
using MesTech.Application.Features.Finance.Commands.CreateExpense;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Finance;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateExpenseValidatorTests
{
    private readonly CreateExpenseValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyTitle_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Title = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public async Task TitleExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Title = new string('T', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public async Task NegativeAmount_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Amount = -1 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public async Task ZeroAmount_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Amount = 0 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task NotesExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Notes = new string('N', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Notes");
    }

    [Fact]
    public async Task NotesNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Notes = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    private static CreateExpenseCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Title: "Kargo masrafı",
        Amount: 150m,
        Category: ExpenseCategory.Travel,
        ExpenseDate: DateTime.UtcNow
    );
}
