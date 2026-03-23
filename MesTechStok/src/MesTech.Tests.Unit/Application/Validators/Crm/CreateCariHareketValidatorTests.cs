using FluentAssertions;
using MesTech.Application.Commands.CreateCariHareket;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Crm;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateCariHareketValidatorTests
{
    private readonly CreateCariHareketValidator _sut = new();

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
    public async Task CariHesapId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { CariHesapId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CariHesapId");
    }

    [Fact]
    public async Task Amount_WhenNegative_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Amount = -1m };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public async Task Amount_WhenZero_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Amount = 0m };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Description_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Description = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task Description_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Description = new string('A', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task Description_WhenExactly500Chars_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Description = new string('A', 500) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    private static CreateCariHareketCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        CariHesapId: Guid.NewGuid(),
        Amount: 100m,
        Direction: CariDirection.Borc,
        Description: "Test hareket",
        Date: DateTime.UtcNow,
        InvoiceId: null,
        OrderId: null
    );
}
