using FluentAssertions;
using MesTech.Application.Commands.BulkUpdateStock;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class BulkUpdateStockValidatorTests
{
    private readonly BulkUpdateStockValidator _validator = new();

    private static BulkUpdateStockCommand ValidCommand() => new(
        Items: new List<BulkUpdateStockItem>
        {
            new("SKU-001", 100),
            new("SKU-002", 50)
        },
        TenantId: Guid.NewGuid());

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Null_TenantId_Fails()
    {
        var cmd = ValidCommand() with { TenantId = null };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Empty_Guid_TenantId_Passes_Because_Nullable()
    {
        // Guid? NotEmpty() does not catch Guid.Empty — only null
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "TenantId");
    }
}
