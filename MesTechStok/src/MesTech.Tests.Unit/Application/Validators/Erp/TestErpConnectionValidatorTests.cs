using FluentAssertions;
using MesTech.Application.Features.Settings.Commands.TestErpConnection;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Erp;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class TestErpConnectionValidatorTests
{
    private readonly TestErpConnectionValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TenantId_Empty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task ErpProvider_None_ShouldPass()
    {
        // IsInEnum — None=0 gecerli enum degeri
        var cmd = CreateValidCommand() with { ErpProvider = ErpProvider.None };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ErpProvider_InvalidEnum_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ErpProvider = (ErpProvider)999 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ErpProvider");
    }

    [Fact]
    public async Task ErpProvider_Parasut_ShouldPass()
    {
        var cmd = CreateValidCommand() with { ErpProvider = ErpProvider.Parasut };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ErpProvider_Logo_ShouldPass()
    {
        var cmd = CreateValidCommand() with { ErpProvider = ErpProvider.Logo };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ErpProvider_Netsis_ShouldPass()
    {
        var cmd = CreateValidCommand() with { ErpProvider = ErpProvider.Netsis };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TenantIdEmpty_And_InvalidEnum_ShouldFail_WithMultipleErrors()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty, ErpProvider = (ErpProvider)888 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task TenantId_Empty_ErrorMessage_ShouldNotBeEmpty()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId")
            .Which.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ErpProvider_InvalidEnum_ErrorMessage_ShouldNotBeEmpty()
    {
        var cmd = CreateValidCommand() with { ErpProvider = (ErpProvider)999 };
        var result = await _sut.ValidateAsync(cmd);
        result.Errors.Should().Contain(e => e.PropertyName == "ErpProvider")
            .Which.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    private static TestErpConnectionCommand CreateValidCommand() =>
        new(Guid.NewGuid(), ErpProvider.Parasut);
}
