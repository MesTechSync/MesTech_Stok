using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateCounterparty;
using MesTech.Domain.Accounting.Enums;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateCounterpartyValidatorTests
{
    private readonly CreateCounterpartyValidator _validator = new();

    private static CreateCounterpartyCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Name: "ABC Ticaret Ltd.",
        CounterpartyType: CounterpartyType.Vendor);

    [Fact]
    public void Valid_Command_Passes()
    {
        _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_Name_Fails()
    {
        var cmd = ValidCommand() with { Name = "" };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Name_Over500_Fails()
    {
        var cmd = ValidCommand() with { Name = new string('A', 501) };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void VKN_Over500_Fails()
    {
        var cmd = ValidCommand() with { VKN = new string('1', 501) };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Phone_Over500_Fails()
    {
        var cmd = ValidCommand() with { Phone = new string('5', 501) };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Email_Over500_Fails()
    {
        var cmd = ValidCommand() with { Email = new string('e', 501) };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Address_Over500_Fails()
    {
        var cmd = ValidCommand() with { Address = new string('X', 501) };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Platform_Over500_Fails()
    {
        var cmd = ValidCommand() with { Platform = new string('P', 501) };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Null_Optionals_Pass()
    {
        var cmd = ValidCommand() with { VKN = null, Phone = null, Email = null };
        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Valid_VKN_Passes()
    {
        var cmd = ValidCommand() with { VKN = "1234567890" };
        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }
}
