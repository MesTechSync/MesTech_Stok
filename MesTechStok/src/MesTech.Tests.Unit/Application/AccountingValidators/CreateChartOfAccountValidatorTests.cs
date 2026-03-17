using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateChartOfAccount;
using AccountingAccountType = MesTech.Domain.Accounting.Enums.AccountType;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class CreateChartOfAccountValidatorTests
{
    private readonly CreateChartOfAccountValidator _validator = new();

    private static CreateChartOfAccountCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Code: "100.01",
        Name: "Kasa Hesabi",
        AccountType: AccountingAccountType.Asset,
        ParentId: null,
        Level: 2
    );

    [Fact]
    public async Task ValidCommand_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_FailsValidation()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EmptyCode_FailsValidation()
    {
        var cmd = ValidCommand() with { Code = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CodeTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Code = new string('1', 21) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("10-01")]
    [InlineData("100_01")]
    public async Task CodeWithInvalidChars_FailsValidation(string code)
    {
        var cmd = ValidCommand() with { Code = code };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("100")]
    [InlineData("100.01")]
    [InlineData("100.01.001")]
    public async Task CodeWithValidFormat_PassesValidation(string code)
    {
        var cmd = ValidCommand() with { Code = code };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyName_FailsValidation()
    {
        var cmd = ValidCommand() with { Name = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NameTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Name = new string('N', 201) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task InvalidLevel_FailsValidation(int level)
    {
        var cmd = ValidCommand() with { Level = level };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task ValidLevel_PassesValidation(int level)
    {
        var cmd = ValidCommand() with { Level = level };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
