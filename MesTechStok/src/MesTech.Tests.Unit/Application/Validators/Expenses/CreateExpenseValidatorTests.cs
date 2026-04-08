using FluentAssertions;
using MesTech.Application.Features.Finance.Commands.CreateExpense;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Expenses;

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
    public async Task TenantId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task Description_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Description = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task Description_WhenNull_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Description = null! };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task Description_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Description = new string('D', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task Description_WhenExactly500Chars_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Description = new string('D', 500) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Amount_WhenNegative_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Amount = -1m };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public async Task Amount_WhenZero_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Amount = 0m };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public async Task ExpenseType_WhenInvalidEnum_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ExpenseType = (ExpenseType)999 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ExpenseType");
    }

    [Fact]
    public async Task Note_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Note = new string('N', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Note");
    }

    [Fact]
    public async Task Note_WhenNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Note = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Note_WhenExactly500Chars_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Note = new string('N', 500) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RecurrencePeriod_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { RecurrencePeriod = new string('R', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RecurrencePeriod");
    }

    [Fact]
    public async Task RecurrencePeriod_WhenNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { RecurrencePeriod = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    private static CreateExpenseCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        StoreId: Guid.NewGuid(),
        Description: "Kargo masrafi",
        Amount: 150m,
        ExpenseType: ExpenseType.Kargo,
        Date: DateTime.UtcNow,
        Note: "Test note"
    );
}
