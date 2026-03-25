using FluentAssertions;
using MesTech.Application.Features.Onboarding.Commands.RegisterTenant;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Platform;

/// <summary>
/// RegisterTenantValidator: SaaS kayıt formu doğrulaması.
/// Kritik: username regex, password strength, email format.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
[Trait("Group", "OnboardingChain")]
public class RegisterTenantValidatorTests
{
    private readonly RegisterTenantValidator _validator = new();

    private static RegisterTenantCommand ValidCommand() => new(
        CompanyName: "Test Firma",
        TaxNumber: "1234567890",
        AdminUsername: "admin.user",
        AdminEmail: "admin@test.com",
        AdminPassword: "Secure123!");

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    // ═══ CompanyName ═══

    [Fact]
    public void Empty_CompanyName_Fails()
    {
        var cmd = ValidCommand() with { CompanyName = "" };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CompanyName_Over200_Fails()
    {
        var cmd = ValidCommand() with { CompanyName = new string('C', 201) };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    // ═══ AdminUsername ═══

    [Fact]
    public void Username_TooShort_Fails()
    {
        var cmd = ValidCommand() with { AdminUsername = "ab" };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Username_Over50_Fails()
    {
        var cmd = ValidCommand() with { AdminUsername = new string('u', 51) };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Username_SpecialChars_Fails()
    {
        var cmd = ValidCommand() with { AdminUsername = "admin@user!" };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Username_WithDots_Passes()
    {
        var cmd = ValidCommand() with { AdminUsername = "admin.user" };
        _validator.Validate(cmd).Errors.Should().NotContain(e => e.PropertyName == "AdminUsername");
    }

    [Fact]
    public void Username_WithDash_Passes()
    {
        var cmd = ValidCommand() with { AdminUsername = "admin-user" };
        _validator.Validate(cmd).Errors.Should().NotContain(e => e.PropertyName == "AdminUsername");
    }

    // ═══ AdminEmail ═══

    [Fact]
    public void Invalid_Email_Fails()
    {
        var cmd = ValidCommand() with { AdminEmail = "not-an-email" };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_Email_Fails()
    {
        var cmd = ValidCommand() with { AdminEmail = "" };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    // ═══ AdminPassword ═══

    [Fact]
    public void Password_TooShort_Fails()
    {
        var cmd = ValidCommand() with { AdminPassword = "Abc1" };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Password_NoUppercase_Fails()
    {
        var cmd = ValidCommand() with { AdminPassword = "secure123!" };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Password_NoDigit_Fails()
    {
        var cmd = ValidCommand() with { AdminPassword = "SecurePass!" };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Password_Valid_Passes()
    {
        var cmd = ValidCommand() with { AdminPassword = "MyPass123" };
        _validator.Validate(cmd).Errors.Should().NotContain(e => e.PropertyName == "AdminPassword");
    }
}
