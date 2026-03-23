using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands.SyncSupplierPrices;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class SyncSupplierPricesValidatorTests
{
    private readonly SyncSupplierPricesValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new SyncSupplierPricesCommand(Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task SupplierId_WhenEmpty_ShouldFail()
    {
        var cmd = new SyncSupplierPricesCommand(Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SupplierId");
    }
}
