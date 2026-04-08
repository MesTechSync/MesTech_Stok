using FluentAssertions;
using MesTech.Domain.Entities.Crm;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class LoyaltyProgramTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_ValidInput_ReturnsActiveProgram()
    {
        var program = LoyaltyProgram.Create(_tenantId, "Gold", 1.5m, 100);
        program.Name.Should().Be("Gold");
        program.PointsPerPurchase.Should().Be(1.5m);
        program.MinRedeemPoints.Should().Be(100);
        program.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_EmptyName_Throws()
    {
        var act = () => LoyaltyProgram.Create(_tenantId, "", 1m, 50);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ZeroPoints_Throws()
    {
        var act = () => LoyaltyProgram.Create(_tenantId, "Test", 0m, 50);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_ZeroMinRedeem_Throws()
    {
        var act = () => LoyaltyProgram.Create(_tenantId, "Test", 1m, 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateRules_ValidInput_UpdatesValues()
    {
        var program = LoyaltyProgram.Create(_tenantId, "Silver", 1m, 50);
        program.UpdateRules(2.5m, 200);
        program.PointsPerPurchase.Should().Be(2.5m);
        program.MinRedeemPoints.Should().Be(200);
    }

    [Fact]
    public void UpdateRules_NegativePoints_Throws()
    {
        var program = LoyaltyProgram.Create(_tenantId, "Test", 1m, 50);
        var act = () => program.UpdateRules(-1m, 50);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var program = LoyaltyProgram.Create(_tenantId, "Test", 1m, 50);
        program.Deactivate();
        program.IsActive.Should().BeFalse();
    }
}
