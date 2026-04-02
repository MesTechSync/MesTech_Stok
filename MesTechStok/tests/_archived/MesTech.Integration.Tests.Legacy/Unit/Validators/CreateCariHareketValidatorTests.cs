using FluentAssertions;
using MesTech.Application.Commands.CreateCariHareket;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Validators;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateCariHareketValidatorTests
{
    private readonly CreateCariHareketValidator _validator = new();

    private static CreateCariHareketCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        CariHesapId: Guid.NewGuid(),
        Amount: 1500.00m,
        Direction: CariDirection.Debit,
        Description: "Satış faturası tahsilatı",
        Date: DateTime.UtcNow,
        InvoiceId: Guid.NewGuid(),
        OrderId: null);

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Empty_CariHesapId_Fails()
    {
        var cmd = ValidCommand() with { CariHesapId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CariHesapId");
    }

    [Fact]
    public void Negative_Amount_Fails()
    {
        var cmd = ValidCommand() with { Amount = -1m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void Zero_Amount_Passes()
    {
        var cmd = ValidCommand() with { Amount = 0m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Description_Fails()
    {
        var cmd = ValidCommand() with { Description = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public void Description_Over_500_Chars_Fails()
    {
        var cmd = ValidCommand() with { Description = new string('D', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }
}
