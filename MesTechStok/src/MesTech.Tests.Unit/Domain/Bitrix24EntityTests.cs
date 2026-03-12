using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
public class Bitrix24EntityTests
{
    // ── Bitrix24Deal ──

    [Fact]
    public void Bitrix24Deal_DefaultSyncStatus_IsNotSynced()
    {
        var deal = new Bitrix24Deal();
        deal.SyncStatus.Should().Be(SyncStatus.NotSynced);
    }

    [Fact]
    public void Bitrix24Deal_ProductRows_InitializedEmpty()
    {
        var deal = new Bitrix24Deal();
        deal.ProductRows.Should().NotBeNull();
        deal.ProductRows.Should().BeEmpty();
    }

    [Fact]
    public void Bitrix24Deal_DefaultCurrency_IsTRY()
    {
        var deal = new Bitrix24Deal();
        deal.Currency.Should().Be("TRY");
    }

    [Fact]
    public void Bitrix24Deal_DefaultStageId_IsNEW()
    {
        var deal = new Bitrix24Deal();
        deal.StageId.Should().Be("NEW");
    }

    // ── Bitrix24DealProductRow ──

    [Fact]
    public void Bitrix24DealProductRow_LineTotal_CalculatedCorrectly_NoDiscount()
    {
        var row = new Bitrix24DealProductRow
        {
            Quantity = 3,
            UnitPrice = 100m,
            Discount = 0
        };
        row.LineTotal.Should().Be(300m);
    }

    [Fact]
    public void Bitrix24DealProductRow_LineTotal_WithDiscount()
    {
        var row = new Bitrix24DealProductRow
        {
            Quantity = 2,
            UnitPrice = 200m,
            Discount = 10 // 10%
        };
        // 2 * 200 * (1 - 10/100) = 400 * 0.9 = 360
        row.LineTotal.Should().Be(360m);
    }

    [Fact]
    public void Bitrix24DealProductRow_TaxAmount_CalculatedCorrectly()
    {
        var row = new Bitrix24DealProductRow
        {
            Quantity = 5,
            UnitPrice = 100m,
            TaxRate = 18 // 18% KDV
        };
        // 5 * 100 * 18/100 = 90
        row.TaxAmount.Should().Be(90m);
    }

    [Fact]
    public void Bitrix24DealProductRow_LineTotal_ZeroQuantity_ReturnsZero()
    {
        var row = new Bitrix24DealProductRow
        {
            Quantity = 0,
            UnitPrice = 100m,
            Discount = 0
        };
        row.LineTotal.Should().Be(0m);
    }

    // ── Bitrix24Contact ──

    [Fact]
    public void Bitrix24Contact_DefaultSyncStatus_IsNotSynced()
    {
        var contact = new Bitrix24Contact();
        contact.SyncStatus.Should().Be(SyncStatus.NotSynced);
    }

    [Fact]
    public void Bitrix24Contact_DefaultStringProperties_AreEmpty()
    {
        var contact = new Bitrix24Contact();
        contact.ExternalContactId.Should().BeEmpty();
        contact.Name.Should().BeEmpty();
    }

    [Fact]
    public void Bitrix24Contact_NullableProperties_AreNull()
    {
        var contact = new Bitrix24Contact();
        contact.LastName.Should().BeNull();
        contact.Phone.Should().BeNull();
        contact.Email.Should().BeNull();
        contact.CompanyTitle.Should().BeNull();
        contact.LastSyncDate.Should().BeNull();
        contact.SyncError.Should().BeNull();
    }
}
