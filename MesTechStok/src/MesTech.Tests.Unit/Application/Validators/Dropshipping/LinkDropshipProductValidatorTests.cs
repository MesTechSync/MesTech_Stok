using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands.LinkDropshipProduct;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class LinkDropshipProductValidatorTests
{
    private readonly LinkDropshipProductValidator _sut = new();

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
    public async Task DropshipProductId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { DropshipProductId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DropshipProductId");
    }

    [Fact]
    public async Task MesTechProductId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { MesTechProductId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MesTechProductId");
    }

    private static LinkDropshipProductCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        DropshipProductId: Guid.NewGuid(),
        MesTechProductId: Guid.NewGuid()
    );
}
