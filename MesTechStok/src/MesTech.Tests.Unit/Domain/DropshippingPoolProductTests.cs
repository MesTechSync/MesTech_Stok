using FluentAssertions;
using MesTech.Domain.Entities;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class DropshippingPoolProductTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _poolId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();

    [Fact]
    public void Constructor_ValidInput_SetsProperties()
    {
        var product = new DropshippingPoolProduct(_tenantId, _poolId, _productId, 150m);

        product.TenantId.Should().Be(_tenantId);
        product.PoolId.Should().Be(_poolId);
        product.ProductId.Should().Be(_productId);
        product.PoolPrice.Should().Be(150m);
        product.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Constructor_EmptyPoolId_Throws()
    {
        var act = () => new DropshippingPoolProduct(_tenantId, Guid.Empty, _productId, 100m);
        act.Should().Throw<ArgumentException>().WithParameterName("poolId");
    }

    [Fact]
    public void Constructor_EmptyProductId_Throws()
    {
        var act = () => new DropshippingPoolProduct(_tenantId, _poolId, Guid.Empty, 100m);
        act.Should().Throw<ArgumentException>().WithParameterName("productId");
    }

    [Fact]
    public void Constructor_NegativePrice_Throws()
    {
        var act = () => new DropshippingPoolProduct(_tenantId, _poolId, _productId, -1m);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("poolPrice");
    }

    [Fact]
    public void Constructor_ZeroPrice_Allowed()
    {
        var product = new DropshippingPoolProduct(_tenantId, _poolId, _productId, 0m);
        product.PoolPrice.Should().Be(0m);
    }

    [Fact]
    public void UpdatePrice_ValidPrice_Updates()
    {
        var product = new DropshippingPoolProduct(_tenantId, _poolId, _productId, 100m);
        product.UpdatePrice(200m);
        product.PoolPrice.Should().Be(200m);
    }

    [Fact]
    public void UpdatePrice_NegativePrice_Throws()
    {
        var product = new DropshippingPoolProduct(_tenantId, _poolId, _productId, 100m);
        var act = () => product.UpdatePrice(-5m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var product = new DropshippingPoolProduct(_tenantId, _poolId, _productId, 100m);
        product.Deactivate();
        product.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_AfterDeactivate_SetsActive()
    {
        var product = new DropshippingPoolProduct(_tenantId, _poolId, _productId, 100m);
        product.Deactivate();
        product.Activate();
        product.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UpdateReliability_ValidScore_Updates()
    {
        var product = new DropshippingPoolProduct(_tenantId, _poolId, _productId, 100m);
        product.UpdateReliability(85.5m, 2);
        product.ReliabilityScore.Should().Be(85.5m);
        product.ReliabilityColor.Should().Be(2);
    }

    [Fact]
    public void UpdateReliability_ScoreAbove100_Throws()
    {
        var product = new DropshippingPoolProduct(_tenantId, _poolId, _productId, 100m);
        var act = () => product.UpdateReliability(101m, 1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateReliability_NegativeScore_Throws()
    {
        var product = new DropshippingPoolProduct(_tenantId, _poolId, _productId, 100m);
        var act = () => product.UpdateReliability(-1m, 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
