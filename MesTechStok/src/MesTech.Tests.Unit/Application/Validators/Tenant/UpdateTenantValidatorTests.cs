using FluentAssertions;
using MesTech.Application.Features.Tenant.Commands.UpdateTenant;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Tenant;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class UpdateTenantValidatorTests
{
    private readonly UpdateTenantValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new UpdateTenantCommand(Guid.NewGuid(), "MesTech A.S.", "1234567890", true);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TenantId_WhenEmpty_ShouldFail()
    {
        var cmd = new UpdateTenantCommand(Guid.Empty, "MesTech A.S.", "1234567890", true);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task Name_WhenEmpty_ShouldFail()
    {
        var cmd = new UpdateTenantCommand(Guid.NewGuid(), "", "1234567890", true);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Name_WhenExceeds200Chars_ShouldFail()
    {
        var cmd = new UpdateTenantCommand(Guid.NewGuid(), new string('N', 201), "1234567890", true);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task TaxNumber_WhenNull_ShouldPass()
    {
        var cmd = new UpdateTenantCommand(Guid.NewGuid(), "MesTech A.S.", null, true);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TaxNumber_WhenExceeds20Chars_ShouldFail()
    {
        var cmd = new UpdateTenantCommand(Guid.NewGuid(), "MesTech A.S.", new string('0', 21), true);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TaxNumber");
    }
}
