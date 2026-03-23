using FluentAssertions;
using MesTech.Application.Features.Tenant.Commands.CreateTenant;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Tenant;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateTenantValidatorTests
{
    private readonly CreateTenantValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new CreateTenantCommand("MesTech A.S.", "1234567890");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Name_WhenEmpty_ShouldFail()
    {
        var cmd = new CreateTenantCommand("", "1234567890");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Name_WhenNull_ShouldFail()
    {
        var cmd = new CreateTenantCommand(null!, "1234567890");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Name_WhenExceeds200Chars_ShouldFail()
    {
        var cmd = new CreateTenantCommand(new string('N', 201), "1234567890");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Name_WhenExactly200Chars_ShouldPass()
    {
        var cmd = new CreateTenantCommand(new string('N', 200), "1234567890");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TaxNumber_WhenNull_ShouldPass()
    {
        var cmd = new CreateTenantCommand("MesTech A.S.", null);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TaxNumber_WhenExceeds20Chars_ShouldFail()
    {
        var cmd = new CreateTenantCommand("MesTech A.S.", new string('0', 21));
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TaxNumber");
    }

    [Fact]
    public async Task TaxNumber_WhenExactly20Chars_ShouldPass()
    {
        var cmd = new CreateTenantCommand("MesTech A.S.", new string('0', 20));
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
