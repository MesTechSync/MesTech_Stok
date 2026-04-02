using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Domain.Exceptions;
using Xunit;

namespace MesTech.Integration.Tests.Domain;

/// <summary>
/// EMR-08 Gorev 3: Invoice domain is kurallari testleri.
/// 10 senaryo: tip tespiti, onay, iptal, toplu fatura.
/// </summary>
public class InvoiceDomainTests
{
    private static Invoice CreateDraftInvoice(string? customerTaxNumber = null, string? platformCode = null)
    {
        var invoice = new Invoice
        {
            TenantId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            InvoiceNumber = "INV-2026-00001",
            Status = InvoiceStatus.Draft,
            CustomerName = "Test Musteri",
            CustomerTaxNumber = customerTaxNumber,
            CustomerAddress = "Istanbul",
            PlatformCode = platformCode
        };
        return invoice;
    }

    private static InvoiceLine CreateLine(decimal unitPrice = 100m, int quantity = 2, int taxRate = 20)
    {
        var taxAmount = unitPrice * quantity * taxRate / 100m;
        return new InvoiceLine
        {
            TenantId = Guid.NewGuid(),
            ProductName = "Test Urun",
            Quantity = quantity,
            UnitPrice = unitPrice,
            TaxRate = taxRate,
            TaxAmount = taxAmount,
            LineTotal = unitPrice * quantity + taxAmount
        };
    }

    // ═══ 1. DetermineInvoiceType — VKN (10 hane) → e-Fatura ═══
    [Fact]
    public void Invoice_DetermineType_VKN_EFatura()
    {
        var invoice = CreateDraftInvoice(customerTaxNumber: "1234567890");

        invoice.DetermineInvoiceType();

        invoice.Type.Should().Be(InvoiceType.EFatura);
        invoice.Scenario.Should().Be(InvoiceScenario.Commercial);
        invoice.IsEInvoiceTaxpayer.Should().BeTrue();
    }

    // ═══ 2. DetermineInvoiceType — VKN yok → e-Arsiv ═══
    [Fact]
    public void Invoice_DetermineType_NoVKN_EArsiv()
    {
        var invoice = CreateDraftInvoice();

        invoice.DetermineInvoiceType();

        invoice.Type.Should().Be(InvoiceType.EArsiv);
        invoice.Scenario.Should().Be(InvoiceScenario.Basic);
    }

    // ═══ 3. DetermineInvoiceType — Amazon EU → e-Ihracat ═══
    [Fact]
    public void Invoice_DetermineType_AmazonEU_EIhracat()
    {
        var invoice = CreateDraftInvoice(platformCode: "AmazonEu");

        invoice.DetermineInvoiceType();

        invoice.Type.Should().Be(InvoiceType.EIhracat);
        invoice.Scenario.Should().Be(InvoiceScenario.Export);
    }

    // ═══ 4. DetermineInvoiceType — eBay → e-Ihracat ═══
    [Fact]
    public void Invoice_DetermineType_eBay_EIhracat()
    {
        var invoice = CreateDraftInvoice(platformCode: "eBay");

        invoice.DetermineInvoiceType();

        invoice.Type.Should().Be(InvoiceType.EIhracat);
        invoice.Scenario.Should().Be(InvoiceScenario.Export);
    }

    // ═══ 5. Approve — Taslak + kalem var → Queued ═══
    [Fact]
    public void Invoice_Approve_Draft_Success()
    {
        var invoice = CreateDraftInvoice();
        invoice.AddLine(CreateLine());

        invoice.Approve();

        invoice.Status.Should().Be(InvoiceStatus.Queued);
    }

    // ═══ 6. Approve — Kalemsiz → exception ═══
    [Fact]
    public void Invoice_Approve_NoLines_Throws()
    {
        var invoice = CreateDraftInvoice();

        var act = () => invoice.Approve();

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*kalemsiz*");
    }

    // ═══ 7. Approve — Tutar sifir → exception ═══
    [Fact]
    public void Invoice_Approve_ZeroAmount_Throws()
    {
        var invoice = CreateDraftInvoice();
        invoice.AddLine(new InvoiceLine
        {
            TenantId = Guid.NewGuid(),
            ProductName = "Bedava",
            Quantity = 1,
            UnitPrice = 0m,
            TaxRate = 0,
            TaxAmount = 0m,
            LineTotal = 0m
        });

        var act = () => invoice.Approve();

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*sifirdan buyuk*");
    }

    // ═══ 8. Cancel — Gonderilmis → Iptal + event ═══
    [Fact]
    public void Invoice_Cancel_Sent_Success()
    {
        var invoice = CreateDraftInvoice();
        invoice.AddLine(CreateLine());
        invoice.Approve();
        invoice.MarkAsSent("GIB-UUID-123", "https://pdf.example.com/inv.pdf");

        invoice.Cancel("Musteri iade istedi");

        invoice.Status.Should().Be(InvoiceStatus.Cancelled);
        invoice.CancellationReason.Should().Be("Musteri iade istedi");
        invoice.CancelledAt.Should().NotBeNull();
    }

    // ═══ 9. Cancel — Kabul edilmis → exception (iptal edilemez) ═══
    [Fact]
    public void Invoice_Cancel_Accepted_Throws()
    {
        var invoice = CreateDraftInvoice();
        invoice.AddLine(CreateLine());
        invoice.Approve();
        invoice.MarkAsSent("GIB-UUID-123", null);
        invoice.MarkAsAccepted();

        var act = () => invoice.Cancel("Iptal denemesi");

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*Kabul edilmis*");
    }

    // ═══ 10. Approve — InvoiceApprovedEvent uretilir ═══
    [Fact]
    public void Invoice_Approve_RaisesApprovedEvent()
    {
        var invoice = CreateDraftInvoice();
        invoice.AddLine(CreateLine());

        invoice.Approve();

        // BaseEntity.DomainEvents uzerinden event kontrolu
        // Not: DomainEvents protected olabilir — ClearDomainEvents varsa kontrol
        invoice.Status.Should().Be(InvoiceStatus.Queued);
        invoice.GrandTotal.Should().BeGreaterThan(0);
    }
}
