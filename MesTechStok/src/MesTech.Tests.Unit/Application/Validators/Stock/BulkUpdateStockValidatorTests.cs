using FluentAssertions;
using MesTech.Application.Commands.BulkUpdateStock;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Stock;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class BulkUpdateStockValidatorTests
{
    private readonly BulkUpdateStockValidator _sut = new();

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

    private static BulkUpdateStockCommand CreateValidCommand() => new(
        Items: new List<BulkUpdateStockItem>
        {
            new("SKU-001", 50),
            new("SKU-002", 100)
        },
        TenantId: Guid.NewGuid()
    );
}
