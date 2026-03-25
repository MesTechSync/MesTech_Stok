using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Billing;
using MesTech.Domain.Entities.EInvoice;
using MesTech.Domain.Enums;
using MesTech.Domain.Exceptions;

namespace MesTech.Tests.Unit.Domain.Entities;

/// <summary>
/// E-commerce entity domain behavior tests.
/// Quotation, QuotationLine, ReturnRequest, ReturnRequestLine,
/// ShipmentCost, DropshippingPool, DropshippingPoolProduct,
/// EInvoiceDocument, EInvoiceLine, EInvoiceSendLog,
/// BillingInvoice, InvoiceTemplate.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "ECommerceEntities")]
[Trait("Phase", "Dalga15")]
public class ECommerceEntityTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    // ═══════════════════════════════════════════
    // Quotation
    // ═══════════════════════════════════════════

    [Fact]
    public void Quotation_AddLine_CalculatesTotals()
    {
        var quotation = new Quotation { TenantId = TenantId, QuotationNumber = "Q-001" };
        var line = new QuotationLine { Quantity = 10, UnitPrice = 100m, TaxRate = 18m };

        quotation.AddLine(line);

        quotation.SubTotal.Should().Be(1000m);
        quotation.TaxTotal.Should().Be(180m);
        quotation.GrandTotal.Should().Be(1180m);
    }

    [Fact]
    public void Quotation_Send_FromDraft_SetsSentStatus()
    {
        var quotation = new Quotation { TenantId = TenantId };

        quotation.Send();

        quotation.Status.Should().Be(QuotationStatus.Sent);
    }

    [Fact]
    public void Quotation_Send_FromSent_Throws()
    {
        var quotation = new Quotation { TenantId = TenantId };
        quotation.Send();

        var act = () => quotation.Send();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Quotation_Accept_FromSent_SetsAccepted()
    {
        var quotation = new Quotation { TenantId = TenantId };
        quotation.Send();

        quotation.Accept();

        quotation.Status.Should().Be(QuotationStatus.Accepted);
    }

    [Fact]
    public void Quotation_Reject_FromSent_SetsRejected()
    {
        var quotation = new Quotation { TenantId = TenantId };
        quotation.Send();

        quotation.Reject();

        quotation.Status.Should().Be(QuotationStatus.Rejected);
    }

    [Fact]
    public void Quotation_MarkAsConverted_FromAccepted_SetsConverted()
    {
        var quotation = new Quotation { TenantId = TenantId };
        quotation.Send();
        quotation.Accept();
        var invoiceId = Guid.NewGuid();

        quotation.MarkAsConverted(invoiceId);

        quotation.Status.Should().Be(QuotationStatus.Converted);
        quotation.ConvertedInvoiceId.Should().Be(invoiceId);
    }

    [Fact]
    public void Quotation_MarkAsConverted_FromDraft_Throws()
    {
        var quotation = new Quotation { TenantId = TenantId };

        var act = () => quotation.MarkAsConverted(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Quotation_MarkAsExpired_FromDraft_SetsExpired()
    {
        var quotation = new Quotation { TenantId = TenantId };

        quotation.MarkAsExpired();

        quotation.Status.Should().Be(QuotationStatus.Expired);
    }

    [Fact]
    public void Quotation_MarkAsExpired_FromAccepted_DoesNotChange()
    {
        var quotation = new Quotation { TenantId = TenantId };
        quotation.Send();
        quotation.Accept();

        quotation.MarkAsExpired();

        quotation.Status.Should().Be(QuotationStatus.Accepted);
    }

    // ═══════════════════════════════════════════
    // QuotationLine (computed properties)
    // ═══════════════════════════════════════════

    [Fact]
    public void QuotationLine_LineTotal_ComputesCorrectly()
    {
        var line = new QuotationLine { Quantity = 5, UnitPrice = 200m, TaxRate = 18m };

        line.LineTotal.Should().Be(1000m);
        line.TaxAmount.Should().Be(180m);
    }

    // ═══════════════════════════════════════════
    // ReturnRequest
    // ═══════════════════════════════════════════

    [Fact]
    public void ReturnRequest_Create_SetsPendingAndRaisesEvent()
    {
        var request = ReturnRequest.Create(Guid.NewGuid(), TenantId,
            PlatformType.Trendyol, ReturnReason.DefectiveProduct, "John Doe");

        request.Status.Should().Be(ReturnStatus.Pending);
        request.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "ReturnCreatedEvent");
    }

    [Fact]
    public void ReturnRequest_AddLine_UpdatesRefundAmount()
    {
        var request = ReturnRequest.Create(Guid.NewGuid(), TenantId,
            PlatformType.Trendyol, ReturnReason.DefectiveProduct, "John");
        var line = new ReturnRequestLine
        {
            TenantId = TenantId, Quantity = 2, UnitPrice = 100m, RefundAmount = 200m
        };

        request.AddLine(line);

        request.RefundAmount.Should().Be(200m);
    }

    [Fact]
    public void ReturnRequest_Approve_FromPending_RaisesEvent()
    {
        var request = ReturnRequest.Create(Guid.NewGuid(), TenantId,
            PlatformType.Trendyol, ReturnReason.DefectiveProduct, "John");
        request.ClearDomainEvents();

        request.Approve();

        request.Status.Should().Be(ReturnStatus.Approved);
        request.ApprovedAt.Should().NotBeNull();
        request.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "ReturnApprovedEvent");
    }

    [Fact]
    public void ReturnRequest_Approve_FromApproved_Throws()
    {
        var request = ReturnRequest.Create(Guid.NewGuid(), TenantId,
            PlatformType.Trendyol, ReturnReason.DefectiveProduct, "John");
        request.Approve();

        var act = () => request.Approve();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ReturnRequest_Reject_FromPending_SetsRejected()
    {
        var request = ReturnRequest.Create(Guid.NewGuid(), TenantId,
            PlatformType.Trendyol, ReturnReason.DefectiveProduct, "John");

        request.Reject("Not eligible");

        request.Status.Should().Be(ReturnStatus.Rejected);
        request.Notes.Should().Be("Not eligible");
    }

    [Fact]
    public void ReturnRequest_FullLifecycle_PendingToRefunded()
    {
        var request = ReturnRequest.Create(Guid.NewGuid(), TenantId,
            PlatformType.Trendyol, ReturnReason.DefectiveProduct, "John");

        request.Approve();
        request.SetCargoInfo("TR123", CargoProvider.YurticiKargo);
        request.MarkAsReceived();
        request.MarkAsRefunded();

        request.Status.Should().Be(ReturnStatus.Refunded);
        request.RefundedAt.Should().NotBeNull();
    }

    [Fact]
    public void ReturnRequest_MarkStockRestored_SetsFlag()
    {
        var request = ReturnRequest.Create(Guid.NewGuid(), TenantId,
            PlatformType.Trendyol, ReturnReason.DefectiveProduct, "John");

        request.MarkStockRestored();

        request.StockRestored.Should().BeTrue();
    }

    [Fact]
    public void ReturnRequest_SetCargoInfo_SetsInTransitStatus()
    {
        var request = ReturnRequest.Create(Guid.NewGuid(), TenantId,
            PlatformType.Trendyol, ReturnReason.DefectiveProduct, "John");

        request.SetCargoInfo("TR123", CargoProvider.ArasKargo);

        request.TrackingNumber.Should().Be("TR123");
        request.CargoProvider.Should().Be(CargoProvider.ArasKargo);
        request.Status.Should().Be(ReturnStatus.InTransit);
    }

    // ═══════════════════════════════════════════
    // ReturnRequestLine
    // ═══════════════════════════════════════════

    [Fact]
    public void ReturnRequestLine_CalculateRefund_ComputesCorrectly()
    {
        var line = new ReturnRequestLine { UnitPrice = 150m, Quantity = 3 };

        line.CalculateRefund();

        line.RefundAmount.Should().Be(450m);
    }

    // ═══════════════════════════════════════════
    // ShipmentCost
    // ═══════════════════════════════════════════

    [Fact]
    public void ShipmentCost_Create_SetsFieldsCorrectly()
    {
        var cost = ShipmentCost.Create(TenantId, Guid.NewGuid(),
            CargoProvider.YurticiKargo, 25.50m, "TR456");

        cost.Provider.Should().Be(CargoProvider.YurticiKargo);
        cost.Cost.Should().Be(25.50m);
        cost.TrackingNumber.Should().Be("TR456");
    }

    [Fact]
    public void ShipmentCost_Create_NegativeCost_Throws()
    {
        var act = () => ShipmentCost.Create(TenantId, Guid.NewGuid(),
            CargoProvider.YurticiKargo, -10m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ShipmentCost_NetCost_WhenNotChargedToCustomer_EqualsCost()
    {
        var cost = ShipmentCost.Create(TenantId, Guid.NewGuid(),
            CargoProvider.ArasKargo, 30m);

        cost.NetCost.Should().Be(30m);
    }

    [Fact]
    public void ShipmentCost_NetCost_WhenChargedToCustomer_ReturnsDifference()
    {
        var cost = ShipmentCost.Create(TenantId, Guid.NewGuid(),
            CargoProvider.ArasKargo, 30m, isChargedToCustomer: true,
            customerChargeAmount: 20m);

        cost.NetCost.Should().Be(10m);
    }

    // ═══════════════════════════════════════════
    // DropshippingPool
    // ═══════════════════════════════════════════

    [Fact]
    public void DropshippingPool_Create_SetsFieldsAndActiveByDefault()
    {
        var pool = new DropshippingPool(TenantId, "Electronics Pool");

        pool.Name.Should().Be("Electronics Pool");
        pool.IsActive.Should().BeTrue();
        pool.IsPublic.Should().BeFalse();
    }

    [Fact]
    public void DropshippingPool_Create_EmptyName_Throws()
    {
        var act = () => new DropshippingPool(TenantId, "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DropshippingPool_Deactivate_SetsInactive()
    {
        var pool = new DropshippingPool(TenantId, "Pool");

        pool.Deactivate();

        pool.IsActive.Should().BeFalse();
    }

    [Fact]
    public void DropshippingPool_Update_ChangesFields()
    {
        var pool = new DropshippingPool(TenantId, "Old");

        pool.Update("New", "Desc", true, PoolPricingStrategy.Fixed);

        pool.Name.Should().Be("New");
        pool.Description.Should().Be("Desc");
        pool.IsPublic.Should().BeTrue();
        pool.PricingStrategy.Should().Be(PoolPricingStrategy.Fixed);
    }

    // ═══════════════════════════════════════════
    // DropshippingPoolProduct
    // ═══════════════════════════════════════════

    [Fact]
    public void DropshippingPoolProduct_Create_SetsFieldsCorrectly()
    {
        var poolId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var pp = new DropshippingPoolProduct(TenantId, poolId, productId, 99.90m);

        pp.PoolId.Should().Be(poolId);
        pp.ProductId.Should().Be(productId);
        pp.PoolPrice.Should().Be(99.90m);
        pp.IsActive.Should().BeTrue();
    }

    [Fact]
    public void DropshippingPoolProduct_Create_EmptyPoolId_Throws()
    {
        var act = () => new DropshippingPoolProduct(TenantId, Guid.Empty, Guid.NewGuid(), 10m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DropshippingPoolProduct_Create_NegativePrice_Throws()
    {
        var act = () => new DropshippingPoolProduct(TenantId, Guid.NewGuid(), Guid.NewGuid(), -5m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void DropshippingPoolProduct_UpdatePrice_SetsNewPrice()
    {
        var pp = new DropshippingPoolProduct(TenantId, Guid.NewGuid(), Guid.NewGuid(), 50m);

        pp.UpdatePrice(75m);

        pp.PoolPrice.Should().Be(75m);
    }

    [Fact]
    public void DropshippingPoolProduct_UpdatePrice_Negative_Throws()
    {
        var pp = new DropshippingPoolProduct(TenantId, Guid.NewGuid(), Guid.NewGuid(), 50m);

        var act = () => pp.UpdatePrice(-10m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void DropshippingPoolProduct_UpdateReliability_ValidScore_Succeeds()
    {
        var pp = new DropshippingPoolProduct(TenantId, Guid.NewGuid(), Guid.NewGuid(), 50m);

        pp.UpdateReliability(85.5m, 2);

        pp.ReliabilityScore.Should().Be(85.5m);
        pp.ReliabilityColor.Should().Be(2);
    }

    [Fact]
    public void DropshippingPoolProduct_UpdateReliability_OutOfRange_Throws()
    {
        var pp = new DropshippingPoolProduct(TenantId, Guid.NewGuid(), Guid.NewGuid(), 50m);

        var act = () => pp.UpdateReliability(150m, 1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ═══════════════════════════════════════════
    // EInvoiceDocument
    // ═══════════════════════════════════════════

    [Fact]
    public void EInvoiceDocument_Create_SetsDraftAndRaisesEvent()
    {
        var uuid = Guid.NewGuid().ToString();
        var doc = EInvoiceDocument.Create(uuid, "ETTN001",
            EInvoiceScenario.TEMELFATURA, EInvoiceType.SATIS,
            DateTime.UtcNow, "1234567890", "Seller Inc",
            "Buyer Inc", "Sovos", "admin");

        doc.Status.Should().Be(EInvoiceStatus.Draft);
        doc.GibUuid.Should().Be(uuid);
        doc.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "EInvoiceCreatedEvent");
    }

    [Fact]
    public void EInvoiceDocument_Create_InvalidGibUuid_Throws()
    {
        var act = () => EInvoiceDocument.Create("not-a-guid", "ETTN001",
            EInvoiceScenario.TEMELFATURA, EInvoiceType.SATIS,
            DateTime.UtcNow, "1234567890", "Seller", "Buyer", "Sovos", "admin");

        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void EInvoiceDocument_Create_InvalidVkn_Throws()
    {
        var act = () => EInvoiceDocument.Create(Guid.NewGuid().ToString(), "ETTN001",
            EInvoiceScenario.TEMELFATURA, EInvoiceType.SATIS,
            DateTime.UtcNow, "123", "Seller", "Buyer", "Sovos", "admin");

        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void EInvoiceDocument_MarkAsSent_FromDraft_Succeeds()
    {
        var doc = CreateEInvoiceDocument();
        doc.ClearDomainEvents();

        doc.MarkAsSent("REF-001", 1);

        doc.Status.Should().Be(EInvoiceStatus.Sent);
        doc.ProviderRef.Should().Be("REF-001");
        doc.CreditUsed.Should().Be(1);
    }

    [Fact]
    public void EInvoiceDocument_MarkAsSent_FromCancelled_Throws()
    {
        var doc = CreateEInvoiceDocument();
        doc.Cancel("Test", "admin");

        var act = () => doc.MarkAsSent("REF", 1);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void EInvoiceDocument_Cancel_FromDraft_Succeeds()
    {
        var doc = CreateEInvoiceDocument();
        doc.ClearDomainEvents();

        doc.Cancel("Wrong data", "admin");

        doc.Status.Should().Be(EInvoiceStatus.Cancelled);
        doc.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "EInvoiceCancelledEvent");
    }

    [Fact]
    public void EInvoiceDocument_Cancel_AlreadyCancelled_Throws()
    {
        var doc = CreateEInvoiceDocument();
        doc.Cancel("reason", "admin");

        var act = () => doc.Cancel("again", "admin");

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void EInvoiceDocument_SetWithholding_ValidRate_CalculatesAmount()
    {
        var doc = CreateEInvoiceDocument();
        doc.SetFinancials(1000, 1000, 1180, 0, 180, 1180);

        doc.SetWithholding(0.50m);

        doc.WithholdingRate.Should().Be(0.50m);
        doc.WithholdingAmount.Should().Be(90m);
        doc.NetPayable.Should().Be(1090m);
    }

    [Fact]
    public void EInvoiceDocument_SetWithholding_InvalidRate_Throws()
    {
        var doc = CreateEInvoiceDocument();

        var act = () => doc.SetWithholding(1.5m);

        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void EInvoiceDocument_SetFinancials_NegativePayable_Throws()
    {
        var doc = CreateEInvoiceDocument();

        var act = () => doc.SetFinancials(0, 0, 0, 0, 0, -100);

        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void EInvoiceDocument_SetPdfUrl_EmptyUrl_Throws()
    {
        var doc = CreateEInvoiceDocument();

        var act = () => doc.SetPdfUrl("");

        act.Should().Throw<DomainValidationException>();
    }

    // ═══════════════════════════════════════════
    // EInvoiceLine
    // ═══════════════════════════════════════════

    [Fact]
    public void EInvoiceLine_Create_CalculatesTaxAmount()
    {
        var line = EInvoiceLine.Create(Guid.NewGuid(), 1, "Product A",
            10, "C62", 100m, 18, 0m, null);

        line.LineExtensionAmount.Should().Be(1000m);
        line.TaxAmount.Should().Be(180m);
        line.TaxPercent.Should().Be(18);
    }

    [Fact]
    public void EInvoiceLine_Create_WithAllowance_ReducesTaxBase()
    {
        var line = EInvoiceLine.Create(Guid.NewGuid(), 1, "Product B",
            10, "C62", 100m, 18, 100m, null);

        line.LineExtensionAmount.Should().Be(900m);
        line.TaxAmount.Should().Be(162m);
    }

    // ═══════════════════════════════════════════
    // EInvoiceSendLog (POCO)
    // ═══════════════════════════════════════════

    [Fact]
    public void EInvoiceSendLog_CreationWithDefaults()
    {
        var log = new EInvoiceSendLog();

        log.ProviderId.Should().BeEmpty();
        log.Action.Should().BeEmpty();
        log.Success.Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    // BillingInvoice
    // ═══════════════════════════════════════════

    [Fact]
    public void BillingInvoice_Create_CalculatesTaxAndTotal()
    {
        var invoice = BillingInvoice.Create(TenantId, Guid.NewGuid(), "MEST-2026-000001", 1000m);

        invoice.Amount.Should().Be(1000m);
        invoice.TaxAmount.Should().Be(200m);
        invoice.TotalAmount.Should().Be(1200m);
        invoice.Status.Should().Be(BillingInvoiceStatus.Draft);
    }

    [Fact]
    public void BillingInvoice_Create_ZeroAmount_Throws()
    {
        var act = () => BillingInvoice.Create(TenantId, Guid.NewGuid(), "INV-001", 0m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void BillingInvoice_Send_FromDraft_SetsSent()
    {
        var invoice = BillingInvoice.Create(TenantId, Guid.NewGuid(), "INV-001", 500m);

        invoice.Send();

        invoice.Status.Should().Be(BillingInvoiceStatus.Sent);
    }

    [Fact]
    public void BillingInvoice_Send_FromSent_Throws()
    {
        var invoice = BillingInvoice.Create(TenantId, Guid.NewGuid(), "INV-001", 500m);
        invoice.Send();

        var act = () => invoice.Send();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void BillingInvoice_MarkPaid_SetsStatusAndTimestamp()
    {
        var invoice = BillingInvoice.Create(TenantId, Guid.NewGuid(), "INV-001", 500m);

        invoice.MarkPaid("TXN-123");

        invoice.Status.Should().Be(BillingInvoiceStatus.Paid);
        invoice.PaidAt.Should().NotBeNull();
        invoice.PaymentTransactionId.Should().Be("TXN-123");
    }

    [Fact]
    public void BillingInvoice_MarkOverdue_FromDraft_SetsOverdue()
    {
        var invoice = BillingInvoice.Create(TenantId, Guid.NewGuid(), "INV-001", 500m);

        invoice.MarkOverdue();

        invoice.Status.Should().Be(BillingInvoiceStatus.Overdue);
    }

    [Fact]
    public void BillingInvoice_MarkOverdue_FromPaid_DoesNotChange()
    {
        var invoice = BillingInvoice.Create(TenantId, Guid.NewGuid(), "INV-001", 500m);
        invoice.MarkPaid();

        invoice.MarkOverdue();

        invoice.Status.Should().Be(BillingInvoiceStatus.Paid);
    }

    [Fact]
    public void BillingInvoice_GenerateInvoiceNumber_FormatsCorrectly()
    {
        var number = BillingInvoice.GenerateInvoiceNumber(42);

        number.Should().StartWith("MEST-");
        number.Should().EndWith("000042");
    }

    // ═══════════════════════════════════════════
    // InvoiceTemplate (POCO)
    // ═══════════════════════════════════════════

    [Fact]
    public void InvoiceTemplate_CreationWithDefaults()
    {
        var template = new InvoiceTemplate
        {
            TenantId = TenantId,
            StoreId = Guid.NewGuid(),
            TemplateName = "Default"
        };

        template.TemplateName.Should().Be("Default");
        template.IsDefault.Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════

    private static EInvoiceDocument CreateEInvoiceDocument()
    {
        return EInvoiceDocument.Create(
            Guid.NewGuid().ToString(), "ETTN-TEST",
            EInvoiceScenario.TEMELFATURA, EInvoiceType.SATIS,
            DateTime.UtcNow, "1234567890", "Seller Inc",
            "Buyer Inc", "Sovos", "admin");
    }
}
