using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using InvoiceEntity = MesTech.Domain.Entities.Invoice;

namespace MesTech.Tests.Unit.Domain.EArsiv;

/// <summary>
/// S1-DEV5-04: e-Arşiv tutar sınırı=0 regression testleri.
/// Tüm tutarlarda e-Arşiv belirlenmesi doğru çalışmalı.
/// DetermineInvoiceType tutar-bağımsız — sadece VKN/platform bazlı.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Domain", "Invoice")]
public class EArsivRegressionTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static InvoiceEntity MakeInvoice(
        decimal grandTotal = 100m, string? customerTaxNumber = null, string? platformCode = null)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(), TenantId = TenantId, OrderNumber = "ORD-EARSIV",
            CustomerId = Guid.NewGuid(), SourcePlatform = PlatformType.Trendyol
        };
        order.SetFinancials(grandTotal * 0.82m, grandTotal * 0.18m, grandTotal);

        var inv = InvoiceEntity.CreateForOrder(order, InvoiceType.EArsiv, $"MES-{Guid.NewGuid().ToString()[..8]}");
        if (customerTaxNumber is not null) inv.CustomerTaxNumber = customerTaxNumber;
        if (platformCode is not null) inv.PlatformCode = platformCode;
        return inv;
    }

    [Fact]
    public void DetermineType_NoVKN_ShouldBeEArsiv()
    {
        var inv = MakeInvoice(grandTotal: 500m);
        inv.DetermineInvoiceType();

        inv.Type.Should().Be(InvoiceType.EArsiv);
        inv.Scenario.Should().Be(InvoiceScenario.Basic);
    }

    [Fact]
    public void DetermineType_ZeroAmount_ShouldStillBeEArsiv()
    {
        var inv = MakeInvoice(grandTotal: 0m);
        inv.DetermineInvoiceType();

        inv.Type.Should().Be(InvoiceType.EArsiv,
            "0 TL fatura da e-Arşiv olmalı — tutar sınırı yok");
    }

    [Fact]
    public void DetermineType_SmallAmount_ShouldBeEArsiv()
    {
        var inv = MakeInvoice(grandTotal: 1m);
        inv.DetermineInvoiceType();

        inv.Type.Should().Be(InvoiceType.EArsiv);
    }

    [Fact]
    public void DetermineType_LargeAmount_ShouldBeEArsiv()
    {
        var inv = MakeInvoice(grandTotal: 999999.99m);
        inv.DetermineInvoiceType();

        inv.Type.Should().Be(InvoiceType.EArsiv,
            "tutar ne olursa olsun bireysel müşteri → e-Arşiv");
    }

    [Fact]
    public void DetermineType_WithVKN10Digit_ShouldBeEFatura()
    {
        var inv = MakeInvoice(grandTotal: 1000m, customerTaxNumber: "1234567890");
        inv.DetermineInvoiceType();

        inv.Type.Should().Be(InvoiceType.EFatura);
        inv.IsEInvoiceTaxpayer.Should().BeTrue();
    }

    [Fact]
    public void DetermineType_WithTCKN11Digit_ShouldBeEArsiv()
    {
        var inv = MakeInvoice(grandTotal: 500m, customerTaxNumber: "12345678901");
        inv.DetermineInvoiceType();

        inv.Type.Should().Be(InvoiceType.EArsiv,
            "TCKN (11 hane) bireysel müşteri → e-Arşiv, e-Fatura değil");
    }

    [Fact]
    public void DetermineType_AmazonEu_ShouldBeEIhracat()
    {
        var inv = MakeInvoice(grandTotal: 200m, platformCode: "AmazonEu");
        inv.DetermineInvoiceType();

        inv.Type.Should().Be(InvoiceType.EIhracat);
        inv.Scenario.Should().Be(InvoiceScenario.Export);
    }

    [Fact]
    public void DetermineType_Trendyol_NoVKN_ShouldBeEArsiv()
    {
        var inv = MakeInvoice(grandTotal: 300m, platformCode: "Trendyol");
        inv.DetermineInvoiceType();

        inv.Type.Should().Be(InvoiceType.EArsiv,
            "Trendyol bireysel müşteri → e-Arşiv");
    }

    [Fact]
    public void DetermineType_EmptyVKN_ShouldBeEArsiv()
    {
        var inv = MakeInvoice(grandTotal: 100m, customerTaxNumber: "");
        inv.DetermineInvoiceType();

        inv.Type.Should().Be(InvoiceType.EArsiv);
    }

    [Fact]
    public void DetermineType_ShortVKN_ShouldBeEArsiv()
    {
        var inv = MakeInvoice(grandTotal: 100m, customerTaxNumber: "12345");
        inv.DetermineInvoiceType();

        inv.Type.Should().Be(InvoiceType.EArsiv,
            "5 haneli VKN geçersiz — e-Arşiv'e düşmeli");
    }
}
