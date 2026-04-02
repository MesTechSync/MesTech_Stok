using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// DEV 1 Röntgen taraması — 40 untested entity'den kritik olanların testleri.
/// [PAYLAŞIM-DEV5] DEV1 yazdı.
/// </summary>

#region Customer

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class CustomerTests
{
    [Fact]
    public void Create_ValidInput_ShouldSetProperties()
    {
        var tenantId = Guid.NewGuid();
        var customer = Customer.Create(tenantId, "Test Müşteri", "MUS-001", "test@test.com", "+905551234567");

        customer.Should().NotBeNull();
        customer.TenantId.Should().Be(tenantId);
        customer.Name.Should().Be("Test Müşteri");
        customer.Code.Should().Be("MUS-001");
        customer.Email.Should().Be("test@test.com");
        customer.IsActive.Should().BeTrue();
        customer.IsBlocked.Should().BeFalse();
    }

    [Fact]
    public void Create_EmptyName_ShouldThrow()
    {
        var act = () => Customer.Create(Guid.NewGuid(), "", "MUS-001");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_EmptyCode_ShouldThrow()
    {
        var act = () => Customer.Create(Guid.NewGuid(), "Test", "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Block_WithReason_ShouldSetBlocked()
    {
        var customer = Customer.Create(Guid.NewGuid(), "Test", "C-001");
        customer.Block("Ödeme gecikti");

        customer.IsBlocked.Should().BeTrue();
        customer.BlockReason.Should().Be("Ödeme gecikti");
    }

    [Fact]
    public void Block_EmptyReason_ShouldThrow()
    {
        var customer = Customer.Create(Guid.NewGuid(), "Test", "C-001");
        var act = () => customer.Block("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Unblock_ShouldClearBlockStatus()
    {
        var customer = Customer.Create(Guid.NewGuid(), "Test", "C-001");
        customer.Block("Test");
        customer.Unblock();

        customer.IsBlocked.Should().BeFalse();
        customer.BlockReason.Should().BeNull();
    }

    [Fact]
    public void AdjustBalance_ZeroAmount_ShouldThrow()
    {
        var customer = Customer.Create(Guid.NewGuid(), "Test", "C-001");
        var act = () => customer.AdjustBalance(0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AdjustBalance_PositiveAmount_ShouldIncrease()
    {
        var customer = Customer.Create(Guid.NewGuid(), "Test", "C-001");
        customer.AdjustBalance(100m);
        customer.CurrentBalance.Should().Be(100m);
    }

    [Fact]
    public void PromoteToVip_ShouldSetVipTrue()
    {
        var customer = Customer.Create(Guid.NewGuid(), "Test", "C-001");
        customer.PromoteToVip();
        customer.IsVip.Should().BeTrue();
    }

    [Fact]
    public void DemoteFromVip_ShouldSetVipFalse()
    {
        var customer = Customer.Create(Guid.NewGuid(), "Test", "C-001");
        customer.PromoteToVip();
        customer.DemoteFromVip();
        customer.IsVip.Should().BeFalse();
    }

    [Fact]
    public void RecordOrderPlaced_ShouldSetLastOrderDate()
    {
        var customer = Customer.Create(Guid.NewGuid(), "Test", "C-001");
        customer.RecordOrderPlaced();
        customer.LastOrderDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}

#endregion

#region Income

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class IncomeTests
{
    [Fact]
    public void Create_NegativeAmount_ShouldThrowOrReject()
    {
        var income = new Income
        {
            TenantId = Guid.NewGuid(),
            Description = "Test"
        };
        var act = () => income.SetAmount(-100m);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Income_ShouldHaveRequiredProperties()
    {
        var income = new Income
        {
            TenantId = Guid.NewGuid(),
            Description = "Platform satış geliri"
        };
        income.SetAmount(500m);

        income.Amount.Should().Be(500m);
        income.Description.Should().Be("Platform satış geliri");
    }
}

#endregion

#region CompanySettings

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class CompanySettingsTests
{
    [Fact]
    public void CompanySettings_DefaultValues_ShouldBeCorrect()
    {
        var settings = new CompanySettings();

        settings.AutoSyncInvoice.Should().BeTrue();
        settings.IsErpConnected.Should().BeFalse();
    }
}

#endregion

#region CashTransaction

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class CashTransactionTests
{
    [Fact]
    public void CashTransaction_ShouldSetProperties()
    {
        var tx = CashTransaction.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            CashTransactionType.Income, 250m,
            "Kasa girişi");

        tx.Amount.Should().Be(250m);
        tx.TenantId.Should().NotBe(Guid.Empty);
        tx.Description.Should().Be("Kasa girişi");
    }
}

#endregion

#region Campaign

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class CampaignTests
{
    [Fact]
    public void Campaign_ShouldHaveDefaults()
    {
        var campaign = Campaign.Create(
            Guid.NewGuid(), "Yaz İndirimi",
            DateTime.UtcNow, DateTime.UtcNow.AddDays(30),
            15m);

        campaign.Name.Should().Be("Yaz İndirimi");
        campaign.IsActive.Should().BeFalse();
    }
}

#endregion
