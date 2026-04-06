using FluentAssertions;
using MesTech.Domain.Entities;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class WarehouseBinTests
{
    private static WarehouseBin CreateBin() => new()
    {
        TenantId = Guid.NewGuid(), Name = "A-1-1", Code = "BIN001",
        ShelfId = Guid.NewGuid(), BinNumber = 1
    };

    [Fact]
    public void NewBin_IsActiveAndAvailable()
    {
        var bin = CreateBin();
        bin.IsActive.Should().BeTrue();
        bin.IsReserved.Should().BeFalse();
        bin.IsLocked.Should().BeFalse();
        bin.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var bin = CreateBin();
        bin.Deactivate();
        bin.IsActive.Should().BeFalse();
        bin.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void Activate_AfterDeactivate_SetsActive()
    {
        var bin = CreateBin();
        bin.Deactivate();
        bin.Activate();
        bin.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Reserve_SetsReserved()
    {
        var bin = CreateBin();
        bin.Reserve();
        bin.IsReserved.Should().BeTrue();
        bin.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void Reserve_WhenLocked_Throws()
    {
        var bin = CreateBin();
        bin.Lock();
        var act = () => bin.Reserve();
        act.Should().Throw<InvalidOperationException>().WithMessage("*kilitli*");
    }

    [Fact]
    public void ReleaseReservation_ClearsReserved()
    {
        var bin = CreateBin();
        bin.Reserve();
        bin.ReleaseReservation();
        bin.IsReserved.Should().BeFalse();
        bin.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void Lock_SetsLockedAndClearsReservation()
    {
        var bin = CreateBin();
        bin.Reserve();
        bin.Lock();
        bin.IsLocked.Should().BeTrue();
        bin.IsReserved.Should().BeFalse(); // Lock clears reservation
        bin.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void Unlock_ClearsLocked()
    {
        var bin = CreateBin();
        bin.Lock();
        bin.Unlock();
        bin.IsLocked.Should().BeFalse();
        bin.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void IsAvailable_AllCombinations()
    {
        var bin = CreateBin();

        // Active + not reserved + not locked = available
        bin.IsAvailable.Should().BeTrue();

        // Reserved = not available
        bin.Reserve();
        bin.IsAvailable.Should().BeFalse();
        bin.ReleaseReservation();

        // Locked = not available
        bin.Lock();
        bin.IsAvailable.Should().BeFalse();
        bin.Unlock();

        // Deactivated = not available
        bin.Deactivate();
        bin.IsAvailable.Should().BeFalse();
    }
}
