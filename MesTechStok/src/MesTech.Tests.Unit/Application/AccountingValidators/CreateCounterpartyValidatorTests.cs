using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateCounterparty;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class CreateCounterpartyValidatorTests
{
    private readonly CreateCounterpartyValidator _validator = new();

    private static CreateCounterpartyCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Name: "Trendyol Marketplace",
        CounterpartyType: CounterpartyType.Platform,
        VKN: "1234567890",
        Phone: "+905551234567",
        Email: "muhasebe@trendyol.com",
        Address: "Maslak, Istanbul",
        Platform: "Trendyol"
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
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyName_FailsValidation()
    {
        var cmd = ValidCommand() with { Name = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task NameTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Name = new string('N', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task InvalidCounterpartyType_FailsValidation()
    {
        var cmd = ValidCommand() with { CounterpartyType = (CounterpartyType)99 };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CounterpartyType");
    }

    [Theory]
    [InlineData(CounterpartyType.Platform)]
    [InlineData(CounterpartyType.Supplier)]
    [InlineData(CounterpartyType.Customer)]
    [InlineData(CounterpartyType.Bank)]
    [InlineData(CounterpartyType.Carrier)]
    public async Task ValidCounterpartyType_PassesValidation(CounterpartyType type)
    {
        var cmd = ValidCommand() with { CounterpartyType = type };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task VKNTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { VKN = new string('V', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "VKN");
    }

    [Fact]
    public async Task NullVKN_PassesValidation()
    {
        var cmd = ValidCommand() with { VKN = null };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task PhoneTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Phone = new string('P', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Phone");
    }

    [Fact]
    public async Task NullPhone_PassesValidation()
    {
        var cmd = ValidCommand() with { Phone = null };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmailTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Email = new string('E', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task NullEmail_PassesValidation()
    {
        var cmd = ValidCommand() with { Email = null };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task AddressTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Address = new string('A', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Address");
    }

    [Fact]
    public async Task NullAddress_PassesValidation()
    {
        var cmd = ValidCommand() with { Address = null };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task PlatformTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Platform = new string('P', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Platform");
    }

    [Fact]
    public async Task NullPlatform_PassesValidation()
    {
        var cmd = ValidCommand() with { Platform = null };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
