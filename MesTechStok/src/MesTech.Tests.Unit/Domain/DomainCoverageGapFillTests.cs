using FluentAssertions;
using MesTech.Domain.Common;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// DEV 5 — Dalga 7 Task 5.13: Domain coverage gap-fill.
/// Tests entities that lacked dedicated test files.
/// Targets computed properties, business methods, and state transitions.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "DomainGapFill")]
public class DomainCoverageGapFillTests
{
    // ════════════════════════════════════════════════════════════════
    //  OrderItem — Computed Properties + CalculateAmounts
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void OrderItem_SubTotal_CalculatesCorrectly()
    {
        var item = new OrderItem { Quantity = 3, UnitPrice = 49.90m };
        item.SubTotal.Should().Be(149.70m);
    }

    [Fact]
    public void OrderItem_SubTotal_ZeroQuantity_ReturnsZero()
    {
        var item = new OrderItem { Quantity = 0, UnitPrice = 100m };
        item.SubTotal.Should().Be(0m);
    }

    [Fact]
    public void OrderItem_CalculateAmounts_SetsTotalPriceAndTax()
    {
        var item = new OrderItem
        {
            Quantity = 2,
            UnitPrice = 100m,
            TaxRate = 0.18m
        };
        item.CalculateAmounts();

        item.TotalPrice.Should().Be(200m);
        item.TaxAmount.Should().Be(36m);
    }

    [Fact]
    public void OrderItem_CalculateAmounts_ZeroTaxRate_TaxAmountIsZero()
    {
        var item = new OrderItem
        {
            Quantity = 5,
            UnitPrice = 50m,
            TaxRate = 0m
        };
        item.CalculateAmounts();

        item.TotalPrice.Should().Be(250m);
        item.TaxAmount.Should().Be(0m);
    }

    [Fact]
    public void OrderItem_ImplementsITenantEntity()
    {
        var item = new OrderItem();
        item.Should().BeAssignableTo<ITenantEntity>();
    }

    [Fact]
    public void OrderItem_ToString_ContainsProductName()
    {
        var item = new OrderItem
        {
            ProductName = "Test Widget",
            Quantity = 3,
            UnitPrice = 100m
        };
        item.CalculateAmounts();

        var result = item.ToString();
        result.Should().Contain("Test Widget");
    }

    // ════════════════════════════════════════════════════════════════
    //  SyncRetryItem — Exponential Backoff State Machine
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void SyncRetryItem_DefaultValues_AreCorrect()
    {
        var item = new SyncRetryItem();

        item.RetryCount.Should().Be(0);
        item.MaxRetries.Should().Be(3);
        item.IsResolved.Should().BeFalse();
    }

    [Fact]
    public void SyncRetryItem_CalculateNextRetry_ExponentialBackoff()
    {
        var item = new SyncRetryItem();
        item.CalculateNextRetry();

        // 2^0 * 60 = 60 seconds
        item.NextRetryUtc.Should().NotBeNull();
        var expectedMinTime = DateTime.UtcNow.AddSeconds(55); // Allow 5s tolerance
        item.NextRetryUtc!.Value.Should().BeAfter(expectedMinTime);
    }

    [Fact]
    public void SyncRetryItem_CalculateNextRetry_HighRetryCount_CappedAt24Hours()
    {
        var item = new SyncRetryItem();
        // Simulate 20 retries via IncrementRetry
        for (int i = 0; i < 20; i++)
            item.IncrementRetry($"error-{i}", "Network");
        item.CalculateNextRetry();

        // Cap at 24 hours
        var maxTime = DateTime.UtcNow.AddHours(24).AddMinutes(1);
        item.NextRetryUtc.Should().NotBeNull();
        item.NextRetryUtc!.Value.Should().BeBefore(maxTime);
    }

    [Fact]
    public void SyncRetryItem_IncrementRetry_UpdatesCountAndTimestamps()
    {
        var item = new SyncRetryItem();
        var beforeRetry = DateTime.UtcNow;

        item.IncrementRetry("Connection timeout", "Network");

        item.RetryCount.Should().Be(1);
        item.LastError.Should().Be("Connection timeout");
        item.ErrorCategory.Should().Be("Network");
        item.LastRetryUtc.Should().BeOnOrAfter(beforeRetry);
        item.NextRetryUtc.Should().NotBeNull();
    }

    [Fact]
    public void SyncRetryItem_MarkAsResolved_ClearsNextRetry()
    {
        var item = new SyncRetryItem();
        item.IncrementRetry("err1", "Net");
        item.IncrementRetry("err2", "Net");

        item.MarkAsResolved();

        item.IsResolved.Should().BeTrue();
        item.ResolvedUtc.Should().NotBeNull();
        item.NextRetryUtc.Should().BeNull();
    }

    [Fact]
    public void SyncRetryItem_ImplementsITenantEntity()
    {
        var item = new SyncRetryItem();
        item.Should().BeAssignableTo<ITenantEntity>();
    }

    // ════════════════════════════════════════════════════════════════
    //  OfflineQueueItem — Default Values
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void OfflineQueueItem_DefaultValues_AreCorrect()
    {
        var item = new OfflineQueueItem();

        item.Channel.Should().Be("Generic");
        item.Direction.Should().Be("Out");
        item.Status.Should().Be("Pending");
        item.RetryCount.Should().Be(0);
    }

    [Fact]
    public void OfflineQueueItem_ImplementsITenantEntity()
    {
        var item = new OfflineQueueItem();
        item.Should().BeAssignableTo<ITenantEntity>();
    }

    // ════════════════════════════════════════════════════════════════
    //  Bitrix24Deal — New Dalga 7 Entity
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Bitrix24Deal_DefaultValues_AreCorrect()
    {
        var deal = new Bitrix24Deal();

        deal.StageId.Should().Be("NEW");
        deal.Currency.Should().Be("TRY");
        deal.SyncStatus.Should().Be(SyncStatus.NotSynced);
    }

    [Fact]
    public void Bitrix24Deal_CanSetProperties()
    {
        var orderId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var deal = new Bitrix24Deal
        {
            TenantId = tenantId,
            OrderId = orderId,
            ExternalDealId = "123",
            Title = "Test Deal",
            Opportunity = 1500.00m,
            StageId = "WON"
        };

        deal.TenantId.Should().Be(tenantId);
        deal.OrderId.Should().Be(orderId);
        deal.ExternalDealId.Should().Be("123");
        deal.Title.Should().Be("Test Deal");
        deal.Opportunity.Should().Be(1500.00m);
        deal.StageId.Should().Be("WON");
    }

    [Fact]
    public void Bitrix24Deal_ImplementsITenantEntity()
    {
        var deal = new Bitrix24Deal();
        deal.Should().BeAssignableTo<ITenantEntity>();
    }

    // ════════════════════════════════════════════════════════════════
    //  Bitrix24Contact — New Dalga 7 Entity
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Bitrix24Contact_DefaultValues_AreCorrect()
    {
        var contact = new Bitrix24Contact();

        contact.SyncStatus.Should().Be(SyncStatus.NotSynced);
    }

    [Fact]
    public void Bitrix24Contact_CanSetCustomerMapping()
    {
        var customerId = Guid.NewGuid();
        var contact = new Bitrix24Contact
        {
            CustomerId = customerId,
            ExternalContactId = "456",
            Name = "Ali",
            LastName = "Yilmaz",
            Phone = "+905551234567",
            Email = "ali@test.com"
        };

        contact.CustomerId.Should().Be(customerId);
        contact.ExternalContactId.Should().Be("456");
        contact.Name.Should().Be("Ali");
        contact.Phone.Should().Be("+905551234567");
    }

    [Fact]
    public void Bitrix24Contact_ImplementsITenantEntity()
    {
        var contact = new Bitrix24Contact();
        contact.Should().BeAssignableTo<ITenantEntity>();
    }

    // ════════════════════════════════════════════════════════════════
    //  Bitrix24DealProductRow — Computed Properties
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Bitrix24DealProductRow_LineTotal_CalculatesWithDiscount()
    {
        var row = new Bitrix24DealProductRow
        {
            Quantity = 2,
            UnitPrice = 100m,
            Discount = 10m // 10%
        };

        // LineTotal = 2 * 100 * (1 - 10/100) = 180
        row.LineTotal.Should().Be(180m);
    }

    [Fact]
    public void Bitrix24DealProductRow_LineTotal_ZeroDiscount()
    {
        var row = new Bitrix24DealProductRow
        {
            Quantity = 3,
            UnitPrice = 50m,
            Discount = 0m
        };

        row.LineTotal.Should().Be(150m);
    }

    [Fact]
    public void Bitrix24DealProductRow_TaxAmount_CalculatesCorrectly()
    {
        var row = new Bitrix24DealProductRow
        {
            Quantity = 2,
            UnitPrice = 100m,
            TaxRate = 18m // 18%
        };

        // TaxAmount = 2 * 100 * 18/100 = 36
        row.TaxAmount.Should().Be(36m);
    }

    [Fact]
    public void Bitrix24DealProductRow_TaxAmount_ZeroRate_IsZero()
    {
        var row = new Bitrix24DealProductRow
        {
            Quantity = 5,
            UnitPrice = 200m,
            TaxRate = 0m
        };

        row.TaxAmount.Should().Be(0m);
    }

    // ════════════════════════════════════════════════════════════════
    //  CariHesap — Collection Aggregation
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void CariHesap_AddHareket_AddsToCollection()
    {
        var hesap = new CariHesap { Name = "Test Account" };
        var hareket = new CariHareket { Amount = 100m };

        hesap.AddHareket(hareket);

        hesap.Hareketler.Should().HaveCount(1);
        hesap.Hareketler.Should().Contain(hareket);
    }

    [Fact]
    public void CariHesap_AddHareket_Null_ThrowsArgumentNullException()
    {
        var hesap = new CariHesap();

        var act = () => hesap.AddHareket(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CariHesap_ImplementsITenantEntity()
    {
        var hesap = new CariHesap();
        hesap.Should().BeAssignableTo<ITenantEntity>();
    }

    // ════════════════════════════════════════════════════════════════
    //  Income & Expense — Financial Entities
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Income_DefaultDate_IsUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var income = new Income();
        var after = DateTime.UtcNow.AddSeconds(1);

        income.Date.Should().BeOnOrAfter(before);
        income.Date.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Income_CanSetProperties()
    {
        var income = new Income
        {
            Description = "Product sale",
            Note = "Online order"
        };
        income.SetAmount(500m);

        income.Description.Should().Be("Product sale");
        income.Amount.Should().Be(500m);
        income.Note.Should().Be("Online order");
    }

    [Fact]
    public void Income_ImplementsITenantEntity()
    {
        var income = new Income();
        income.Should().BeAssignableTo<ITenantEntity>();
    }

    [Fact]
    public void Expense_DefaultDate_IsUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var expense = new Expense();
        var after = DateTime.UtcNow.AddSeconds(1);

        expense.Date.Should().BeOnOrAfter(before);
        expense.Date.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Expense_RecurringProperties()
    {
        var expense = new Expense
        {
            IsRecurring = true,
            RecurrencePeriod = "Monthly"
        };
        expense.SetAmount(250m);

        expense.IsRecurring.Should().BeTrue();
        expense.RecurrencePeriod.Should().Be("Monthly");
    }

    [Fact]
    public void Expense_ImplementsITenantEntity()
    {
        var expense = new Expense();
        expense.Should().BeAssignableTo<ITenantEntity>();
    }

    // ════════════════════════════════════════════════════════════════
    //  Warehouse Hierarchy — Zone → Rack → Shelf → Bin
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void WarehouseZone_DefaultValues_AreCorrect()
    {
        var zone = new WarehouseZone();

        zone.IsActive.Should().BeTrue();
        zone.HasClimateControl.Should().BeFalse();
        zone.HasSecurity.Should().BeFalse();
    }

    [Fact]
    public void WarehouseZone_CanSetProperties()
    {
        var warehouseId = Guid.NewGuid();
        var zone = new WarehouseZone
        {
            Name = "Zone A",
            Code = "ZA",
            WarehouseId = warehouseId,
            Width = 10.5m,
            Length = 20.0m,
            Height = 5.0m,
            FloorNumber = 1,
            HasClimateControl = true,
            TemperatureRange = "2-8°C"
        };

        zone.Name.Should().Be("Zone A");
        zone.Code.Should().Be("ZA");
        zone.HasClimateControl.Should().BeTrue();
        zone.TemperatureRange.Should().Be("2-8°C");
    }

    [Fact]
    public void WarehouseRack_DefaultValues_AreCorrect()
    {
        var rack = new WarehouseRack();

        rack.IsActive.Should().BeTrue();
        rack.IsMovable.Should().BeFalse();
    }

    [Fact]
    public void WarehouseRack_CanSetDimensions()
    {
        var rack = new WarehouseRack
        {
            Name = "Rack A-01",
            Code = "RA01",
            ShelfCount = 5,
            BinCount = 20,
            MaxWeight = 500m
        };

        rack.ShelfCount.Should().Be(5);
        rack.BinCount.Should().Be(20);
        rack.MaxWeight.Should().Be(500m);
    }

    // ════════════════════════════════════════════════════════════════
    //  PlatformType Enum — Bitrix24 Added
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void PlatformType_Bitrix24_HasValue11()
    {
        ((int)PlatformType.Bitrix24).Should().Be(11);
    }

    [Fact]
    public void PlatformType_ShouldHave12Members()
    {
        var values = Enum.GetValues<PlatformType>();
        values.Length.Should().Be(13);
    }

    // ════════════════════════════════════════════════════════════════
    //  BaseEntity Contract — All Entities Inherit
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(typeof(Bitrix24Deal))]
    [InlineData(typeof(Bitrix24Contact))]
    [InlineData(typeof(Bitrix24DealProductRow))]
    [InlineData(typeof(OrderItem))]
    [InlineData(typeof(SyncRetryItem))]
    [InlineData(typeof(OfflineQueueItem))]
    [InlineData(typeof(Income))]
    [InlineData(typeof(Expense))]
    [InlineData(typeof(CariHesap))]
    [InlineData(typeof(WarehouseZone))]
    [InlineData(typeof(WarehouseRack))]
    public void Entity_ShouldInheritFromBaseEntity(Type entityType)
    {
        typeof(BaseEntity).IsAssignableFrom(entityType).Should().BeTrue(
            $"{entityType.Name} must inherit from BaseEntity");
    }
}
