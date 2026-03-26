using FluentAssertions;
using MesTech.Application.Features.Onboarding.Commands.RegisterTenant;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Onboarding;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class RegisterTenantValidatorTests
{
    private readonly RegisterTenantValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValid();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyCompanyName_ShouldFail()
    {
        var cmd = CreateValid() with { CompanyName = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CompanyName");
    }

    [Fact]
    public async Task CompanyNameExceeds200_ShouldFail()
    {
        var cmd = CreateValid() with { CompanyName = new string('C', 201) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EmptyAdminUsername_ShouldFail()
    {
        var cmd = CreateValid() with { AdminUsername = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AdminUsername");
    }

    [Fact]
    public async Task UsernameTooShort_ShouldFail()
    {
        var cmd = CreateValid() with { AdminUsername = "ab" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task UsernameExceeds50_ShouldFail()
    {
        var cmd = CreateValid() with { AdminUsername = new string('u', 51) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task UsernameWithSpecialChars_ShouldFail()
    {
        var cmd = CreateValid() with { AdminUsername = "user@name!" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("admin.user")]
    [InlineData("admin-user")]
    [InlineData("admin_user")]
    [InlineData("admin123")]
    public async Task ValidUsernames_ShouldPass(string username)
    {
        var cmd = CreateValid() with { AdminUsername = username };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyEmail_ShouldFail()
    {
        var cmd = CreateValid() with { AdminEmail = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AdminEmail");
    }

    [Fact]
    public async Task InvalidEmail_ShouldFail()
    {
        var cmd = CreateValid() with { AdminEmail = "not-email" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EmptyPassword_ShouldFail()
    {
        var cmd = CreateValid() with { AdminPassword = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task PasswordTooShort_ShouldFail()
    {
        var cmd = CreateValid() with { AdminPassword = "Ab1" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task PasswordNoUppercase_ShouldFail()
    {
        var cmd = CreateValid() with { AdminPassword = "password1" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task PasswordNoDigit_ShouldFail()
    {
        var cmd = CreateValid() with { AdminPassword = "PasswordABC" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task StrongPassword_ShouldPass()
    {
        var cmd = CreateValid() with { AdminPassword = "SecurePass1" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    private static RegisterTenantCommand CreateValid() => new(
        CompanyName: "MesTech Test A.Ş.",
        TaxNumber: "1234567890",
        AdminUsername: "admin",
        AdminEmail: "admin@mestech.com",
        AdminPassword: "Secure1Pass"
    );
}
