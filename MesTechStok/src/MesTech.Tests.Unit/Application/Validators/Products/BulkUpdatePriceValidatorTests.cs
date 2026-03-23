using FluentAssertions;
using MesTech.Application.Commands.BulkUpdatePrice;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Products;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class BulkUpdatePriceValidatorTests
{
    private readonly BulkUpdatePriceValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TenantId_WhenNull_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    private static BulkUpdatePriceCommand CreateValidCommand() => new(
        Items: new List<BulkUpdatePriceItem>
        {
            new("SKU-001", 29.90m),
            new("SKU-002", 49.90m)
        },
        TenantId: Guid.NewGuid()
    );
}
