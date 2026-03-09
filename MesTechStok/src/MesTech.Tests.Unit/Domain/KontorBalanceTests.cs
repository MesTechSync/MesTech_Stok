using MesTech.Domain.Common;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

public class KontorBalanceTests
{
    [Fact]
    public void KontorBalance_Should_Implement_ITenantEntity()
    {
        var balance = new KontorBalance();
        Assert.IsAssignableFrom<ITenantEntity>(balance);
    }

    [Fact]
    public void UpdateBalance_Should_Set_Fields()
    {
        var balance = new KontorBalance();
        balance.UpdateBalance(remaining: 450, total: 500);

        Assert.Equal(450, balance.RemainingKontor);
        Assert.Equal(500, balance.TotalKontor);
        Assert.True(balance.LastCheckedAt <= DateTime.UtcNow);
        Assert.True(balance.LastCheckedAt > DateTime.UtcNow.AddSeconds(-2));
    }

    [Fact]
    public void IsLow_Default_Threshold_50()
    {
        var balance = new KontorBalance { RemainingKontor = 50 };
        Assert.True(balance.IsLow());

        balance.RemainingKontor = 51;
        Assert.False(balance.IsLow());
    }

    [Fact]
    public void IsLow_Custom_Threshold()
    {
        var balance = new KontorBalance { RemainingKontor = 10 };
        Assert.True(balance.IsLow(threshold: 20));
        Assert.False(balance.IsLow(threshold: 5));
    }
}
