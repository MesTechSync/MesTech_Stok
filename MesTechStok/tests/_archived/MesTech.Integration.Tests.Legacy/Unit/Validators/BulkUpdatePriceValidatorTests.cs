using FluentAssertions;
using MesTech.Application.Commands.BulkUpdatePrice;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Validators;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class BulkUpdatePriceValidatorTests
{
    private readonly BulkUpdatePriceValidator _validator = new();

    private static BulkUpdatePriceCommand ValidCommand() => new(
        Items: new List<BulkUpdatePriceItem>
        {
            new("SKU-001", 99.90m),
            new("SKU-002", 149.90m)
        },
        TenantId: Guid.NewGuid());

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
    public void Null_TenantId_Fails()
    {
        var cmd = ValidCommand() with { TenantId = null };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }
}
