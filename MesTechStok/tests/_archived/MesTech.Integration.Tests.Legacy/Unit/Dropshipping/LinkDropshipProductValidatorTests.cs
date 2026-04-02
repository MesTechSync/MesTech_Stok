using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands.LinkDropshipProduct;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class LinkDropshipProductValidatorTests
{
    private readonly LinkDropshipProductValidator _validator = new();

    private static LinkDropshipProductCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        DropshipProductId: Guid.NewGuid(),
        MesTechProductId: Guid.NewGuid());

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Empty_DropshipProductId_Fails()
    {
        var cmd = ValidCommand() with { DropshipProductId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DropshipProductId");
    }

    [Fact]
    public void Empty_MesTechProductId_Fails()
    {
        var cmd = ValidCommand() with { MesTechProductId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MesTechProductId");
    }
}
