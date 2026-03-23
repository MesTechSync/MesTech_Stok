using FluentAssertions;
using MesTech.Application.Features.Product.Commands.BulkUpdateProducts;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Product;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class BulkUpdateProductsValidatorTests
{
    private readonly BulkUpdateProductsValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new BulkUpdateProductsCommand(
            new List<Guid> { Guid.NewGuid() },
            BulkUpdateAction.PriceIncreasePercent,
            10m
        );
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyProductIds_ShouldFail()
    {
        var cmd = new BulkUpdateProductsCommand(
            new List<Guid>(),
            BulkUpdateAction.StatusActivate
        );
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductIds");
    }

    [Fact]
    public async Task MoreThan500Products_ShouldFail()
    {
        var ids = Enumerable.Range(0, 501).Select(_ => Guid.NewGuid()).ToList();
        var cmd = new BulkUpdateProductsCommand(ids, BulkUpdateAction.StatusActivate);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductIds");
    }

    [Fact]
    public async Task Exactly500Products_ShouldPass()
    {
        var ids = Enumerable.Range(0, 500).Select(_ => Guid.NewGuid()).ToList();
        var cmd = new BulkUpdateProductsCommand(ids, BulkUpdateAction.StatusActivate);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task InvalidAction_ShouldFail()
    {
        var cmd = new BulkUpdateProductsCommand(
            new List<Guid> { Guid.NewGuid() },
            (BulkUpdateAction)99
        );
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Action");
    }
}
