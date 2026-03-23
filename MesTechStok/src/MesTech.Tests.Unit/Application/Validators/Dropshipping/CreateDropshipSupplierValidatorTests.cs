using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands.CreateDropshipSupplier;
using MesTech.Domain.Dropshipping.Enums;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateDropshipSupplierValidatorTests
{
    private readonly CreateDropshipSupplierValidator _sut = new();

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
    public async Task Name_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Name = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Name_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Name = new string('N', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task WebsiteUrl_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { WebsiteUrl = new string('W', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "WebsiteUrl");
    }

    [Fact]
    public async Task MarkupType_WhenInvalidEnum_ShouldFail()
    {
        var cmd = CreateValidCommand() with { MarkupType = (DropshipMarkupType)99 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MarkupType");
    }

    private static CreateDropshipSupplierCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Name: "Test Supplier",
        WebsiteUrl: "https://supplier.com",
        MarkupType: DropshipMarkupType.Percentage,
        MarkupValue: 15.0m
    );
}
