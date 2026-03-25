using FluentAssertions;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Events;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Domain.Exceptions;

namespace MesTech.Tests.Unit.Domain.Entities;

/// <summary>
/// Invoice entity domain behavior tests + JournalEntry balanced entry tests.
/// Approve, Cancel, MarkAsSent, DetermineInvoiceType, line calculations, JournalEntry.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "InvoiceEntity")]
[Trait("Phase", "Dalga15")]
public class InvoiceEntityTests
{
    private static Invoice CreateInvoice(InvoiceStatus status = InvoiceStatus.Draft, decimal grandTotal = 0m)
    {
        var invoice = new Invoice
        {
            TenantId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            InvoiceNumber = "INV-2026-001",
            Type = InvoiceType.EArsiv,
            CustomerName = "Test Customer",
            CustomerAddress = "Test Address",
            Currency = "TRY"
        };

        if (grandTotal > 0)
            invoice.SetFinancials(grandTotal * 0.85m, grandTotal * 0.15m, grandTotal);

        if (status == InvoiceStatus.Queued)
        {
            // Need lines and positive total to approve
            var line = CreateInvoiceLine(100m, 2, 0.18m);
            invoice.AddLine(line);
            invoice.Approve();
        }
        else if (status == InvoiceStatus.Sent)
        {
            var line = CreateInvoiceLine(100m, 2, 0.18m);
            invoice.AddLine(line);
            invoice.Approve();
            invoice.MarkAsSent("GIB-001", "https://pdf.example.com/inv.pdf");
        }
        else if (status == InvoiceStatus.Accepted)
        {
            var line = CreateInvoiceLine(100m, 2, 0.18m);
            invoice.AddLine(line);
            invoice.Approve();
            invoice.MarkAsSent("GIB-001", null);
            invoice.MarkAsAccepted();
        }

        return invoice;
    }

    private static InvoiceLine CreateInvoiceLine(
        decimal unitPrice = 100m, int quantity = 1, decimal taxRate = 0.18m, decimal? discount = null)
    {
        var line = new InvoiceLine
        {
            TenantId = Guid.NewGuid(),
            InvoiceId = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            SKU = "TST-001",
            TaxRate = taxRate,
            DiscountAmount = discount
        };
        line.SetQuantityAndPrice(quantity, unitPrice);
        return line;
    }

    // ═══════════════════════════════════════════
    // Invoice Creation Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void NewInvoice_HasDraftStatus()
    {
        var invoice = new Invoice();
        invoice.Status.Should().Be(InvoiceStatus.Draft);
    }

    [Fact]
    public void NewInvoice_DefaultCurrencyIsTRY()
    {
        var invoice = new Invoice();
        invoice.Currency.Should().Be("TRY");
    }

    [Fact]
    public void CreateForOrder_SetsFieldsFromOrder()
    {
        var order = new Order
        {
            TenantId = Guid.NewGuid(),
            CustomerName = "Ahmet",
            CustomerEmail = "ahmet@test.com"
        };
        order.SetFinancials(100m, 18m, 118m);

        var invoice = Invoice.CreateForOrder(order, InvoiceType.EFatura, "INV-F-001");

        invoice.OrderId.Should().Be(order.Id);
        invoice.TenantId.Should().Be(order.TenantId);
        invoice.Type.Should().Be(InvoiceType.EFatura);
        invoice.InvoiceNumber.Should().Be("INV-F-001");
        invoice.CustomerName.Should().Be("Ahmet");
        invoice.GrandTotal.Should().Be(118m);
        invoice.IsEInvoiceTaxpayer.Should().BeTrue();
    }

    [Fact]
    public void CreateForOrder_RaisesInvoiceCreatedEvent()
    {
        var order = new Order { TenantId = Guid.NewGuid() };
        order.SetFinancials(100m, 18m, 118m);

        var invoice = Invoice.CreateForOrder(order, InvoiceType.EArsiv, "INV-A-001");

        invoice.DomainEvents.Should().ContainSingle(e => e is InvoiceCreatedEvent);
    }

    // ═══════════════════════════════════════════
    // AddLine / CalculateTotals Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void AddLine_RecalculatesTotals()
    {
        var invoice = new Invoice { TenantId = Guid.NewGuid(), CustomerAddress = "addr" };
        var line = CreateInvoiceLine(unitPrice: 200m, quantity: 3, taxRate: 0.18m);

        invoice.AddLine(line);

        invoice.SubTotal.Should().Be(600m); // 200*3
        invoice.TaxTotal.Should().Be(108m); // 600*0.18
        invoice.GrandTotal.Should().Be(708m);
    }

    [Fact]
    public void AddLine_WithDiscount_SubtractsDiscount()
    {
        var invoice = new Invoice { TenantId = Guid.NewGuid(), CustomerAddress = "addr" };
        var line = CreateInvoiceLine(unitPrice: 100m, quantity: 2, taxRate: 0.18m, discount: 20m);

        invoice.AddLine(line);

        // SubTotal = (100*2) - 20 = 180
        invoice.SubTotal.Should().Be(180m);
        // TaxTotal uses line.TaxAmount which is (100*2-20)*0.18 = 32.40
        invoice.TaxTotal.Should().Be(32.40m);
        invoice.GrandTotal.Should().Be(212.40m);
    }

    [Fact]
    public void AddLine_MultipleLines_SumsCorrectly()
    {
        var invoice = new Invoice { TenantId = Guid.NewGuid(), CustomerAddress = "addr" };
        invoice.AddLine(CreateInvoiceLine(100m, 1, 0.18m));
        invoice.AddLine(CreateInvoiceLine(50m, 2, 0.08m));

        invoice.SubTotal.Should().Be(200m); // 100 + 100
        invoice.TaxTotal.Should().Be(26m); // 18 + 8
        invoice.GrandTotal.Should().Be(226m);
    }

    // ═══════════════════════════════════════════
    // InvoiceLine Validation Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void InvoiceLine_SetQuantityAndPrice_ZeroQuantity_Throws()
    {
        var line = new InvoiceLine();

        var act = () => line.SetQuantityAndPrice(0, 100m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void InvoiceLine_SetQuantityAndPrice_NegativePrice_Throws()
    {
        var line = new InvoiceLine();

        var act = () => line.SetQuantityAndPrice(1, -50m);

        act.Should().Throw<ArgumentException>();
    }

    // ═══════════════════════════════════════════
    // Approve Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void Approve_DraftWithLinesAndPositiveTotal_SetsQueuedStatus()
    {
        var invoice = CreateInvoice();
        invoice.AddLine(CreateInvoiceLine(100m, 1, 0.18m));
        invoice.ClearDomainEvents();

        invoice.Approve();

        invoice.Status.Should().Be(InvoiceStatus.Queued);
    }

    [Fact]
    public void Approve_RaisesInvoiceApprovedEvent()
    {
        var invoice = CreateInvoice();
        invoice.AddLine(CreateInvoiceLine(100m, 1, 0.18m));
        invoice.ClearDomainEvents();

        invoice.Approve();

        invoice.DomainEvents.Should().Contain(e => e is InvoiceApprovedEvent);
    }

    [Fact]
    public void Approve_RaisesInvoiceGeneratedForERPEvent()
    {
        var invoice = CreateInvoice();
        invoice.AddLine(CreateInvoiceLine(100m, 1, 0.18m));
        invoice.ClearDomainEvents();

        invoice.Approve();

        invoice.DomainEvents.Should().Contain(e => e is InvoiceGeneratedForERPEvent);
    }

    [Fact]
    public void Approve_NotDraft_ThrowsBusinessRuleException()
    {
        var invoice = CreateInvoice(InvoiceStatus.Queued);

        var act = () => invoice.Approve();

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Approve_NoLines_ThrowsBusinessRuleException()
    {
        var invoice = CreateInvoice();

        var act = () => invoice.Approve();

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Approve_ZeroGrandTotal_ThrowsBusinessRuleException()
    {
        var invoice = CreateInvoice();
        // Add a line with zero price to get zero total
        var line = new InvoiceLine
        {
            TenantId = Guid.NewGuid(),
            ProductName = "Free",
            TaxRate = 0m
        };
        line.SetQuantityAndPrice(1, 0.001m);
        // Override financials to zero
        invoice.AddLine(line);
        invoice.SetFinancials(0m, 0m, 0m);

        var act = () => invoice.Approve();

        act.Should().Throw<BusinessRuleException>();
    }

    // ═══════════════════════════════════════════
    // Cancel Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void Cancel_FromDraft_SetsCancelledStatus()
    {
        var invoice = CreateInvoice();

        invoice.Cancel("not needed");

        invoice.Status.Should().Be(InvoiceStatus.Cancelled);
        invoice.CancellationReason.Should().Be("not needed");
        invoice.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_RaisesInvoiceCancelledEvent()
    {
        var invoice = CreateInvoice();
        invoice.ClearDomainEvents();

        invoice.Cancel("test reason");

        invoice.DomainEvents.Should().ContainSingle(e => e is InvoiceCancelledEvent);
    }

    [Fact]
    public void Cancel_FromAccepted_ThrowsBusinessRuleException()
    {
        var invoice = CreateInvoice(InvoiceStatus.Accepted);

        var act = () => invoice.Cancel("too late");

        act.Should().Throw<BusinessRuleException>();
    }

    // ═══════════════════════════════════════════
    // MarkAsSent / MarkAsAccepted / MarkAsRejected Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void MarkAsSent_SetsSentStatusAndGibId()
    {
        var invoice = CreateInvoice(InvoiceStatus.Queued);
        invoice.ClearDomainEvents();

        invoice.MarkAsSent("GIB-12345", "https://pdf.example.com/inv.pdf");

        invoice.Status.Should().Be(InvoiceStatus.Sent);
        invoice.GibInvoiceId.Should().Be("GIB-12345");
        invoice.PdfUrl.Should().Be("https://pdf.example.com/inv.pdf");
        invoice.SentAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsSent_RaisesInvoiceSentEvent()
    {
        var invoice = CreateInvoice(InvoiceStatus.Queued);
        invoice.ClearDomainEvents();

        invoice.MarkAsSent("GIB-99", null);

        invoice.DomainEvents.Should().ContainSingle(e => e is InvoiceSentEvent);
    }

    [Fact]
    public void MarkAsAccepted_SetsAcceptedStatus()
    {
        var invoice = CreateInvoice(InvoiceStatus.Sent);

        invoice.MarkAsAccepted();

        invoice.Status.Should().Be(InvoiceStatus.Accepted);
        invoice.AcceptedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsRejected_SetsRejectedStatus()
    {
        var invoice = CreateInvoice(InvoiceStatus.Sent);

        invoice.MarkAsRejected();

        invoice.Status.Should().Be(InvoiceStatus.Rejected);
    }

    [Fact]
    public void MarkAsPlatformSent_SetsPlatformSentStatus()
    {
        var invoice = CreateInvoice();

        invoice.MarkAsPlatformSent("https://trendyol.com/inv/123");

        invoice.Status.Should().Be(InvoiceStatus.PlatformSent);
        invoice.PlatformInvoiceUrl.Should().Be("https://trendyol.com/inv/123");
    }

    // ═══════════════════════════════════════════
    // DetermineInvoiceType Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void DetermineInvoiceType_VKN10Digits_SetsEFatura()
    {
        var invoice = CreateInvoice();
        invoice.CustomerTaxNumber = "1234567890"; // 10 digits

        invoice.DetermineInvoiceType();

        invoice.Type.Should().Be(InvoiceType.EFatura);
        invoice.Scenario.Should().Be(InvoiceScenario.Commercial);
        invoice.IsEInvoiceTaxpayer.Should().BeTrue();
    }

    [Fact]
    public void DetermineInvoiceType_ForeignPlatform_SetsEIhracat()
    {
        var invoice = CreateInvoice();
        invoice.PlatformCode = PlatformType.AmazonEu.ToString();

        invoice.DetermineInvoiceType();

        invoice.Type.Should().Be(InvoiceType.EIhracat);
        invoice.Scenario.Should().Be(InvoiceScenario.Export);
    }

    [Fact]
    public void DetermineInvoiceType_Individual_SetsEArsiv()
    {
        var invoice = CreateInvoice();
        invoice.CustomerTaxNumber = null;
        invoice.PlatformCode = null;

        invoice.DetermineInvoiceType();

        invoice.Type.Should().Be(InvoiceType.EArsiv);
        invoice.Scenario.Should().Be(InvoiceScenario.Basic);
    }

    // ═══════════════════════════════════════════
    // Sign / GIB Status / Parasut Sync Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void Sign_SetsSignatureFields()
    {
        var invoice = CreateInvoice();

        invoice.Sign("admin@mestech.com", SignatureType.XAdES_BES);

        invoice.SignatureStatus.Should().Be(SignatureStatus.Signed);
        invoice.SignedBy.Should().Be("admin@mestech.com");
        invoice.SignedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateGibStatus_SetsStatusAndDate()
    {
        var invoice = CreateInvoice();

        invoice.UpdateGibStatus("KABUL", "ENV-001");

        invoice.GibStatus.Should().Be("KABUL");
        invoice.GibEnvelopeId.Should().Be("ENV-001");
        invoice.GibStatusDate.Should().NotBeNull();
    }

    [Fact]
    public void MarkParasutSynced_SetsSyncFields()
    {
        var invoice = CreateInvoice();

        invoice.MarkParasutSynced("PS-123", "PE-456");

        invoice.ParasutSalesInvoiceId.Should().Be("PS-123");
        invoice.ParasutEInvoiceId.Should().Be("PE-456");
        invoice.ParasutSyncStatus.Should().Be(SyncStatus.Synced);
        invoice.ParasutSyncedAt.Should().NotBeNull();
        invoice.ParasutSyncError.Should().BeNull();
    }

    [Fact]
    public void MarkParasutFailed_SetsErrorTruncatedTo500()
    {
        var invoice = CreateInvoice();
        var longError = new string('x', 600);

        invoice.MarkParasutFailed(longError);

        invoice.ParasutSyncStatus.Should().Be(SyncStatus.Failed);
        invoice.ParasutSyncError.Should().HaveLength(500);
    }

    // ═══════════════════════════════════════════
    // JournalEntry Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void JournalEntry_Create_SetsProperties()
    {
        var tenantId = Guid.NewGuid();
        var date = new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc);

        var entry = JournalEntry.Create(tenantId, date, "Sale invoice #001", "REF-001");

        entry.TenantId.Should().Be(tenantId);
        entry.EntryDate.Should().Be(date);
        entry.Description.Should().Be("Sale invoice #001");
        entry.ReferenceNumber.Should().Be("REF-001");
        entry.IsPosted.Should().BeFalse();
    }

    [Fact]
    public void JournalEntry_Create_EmptyTenant_Throws()
    {
        var act = () => JournalEntry.Create(Guid.Empty, DateTime.UtcNow, "test");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void JournalEntry_Create_EmptyDescription_Throws()
    {
        var act = () => JournalEntry.Create(Guid.NewGuid(), DateTime.UtcNow, "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void JournalEntry_AddLine_DebitLine_Works()
    {
        var entry = JournalEntry.Create(Guid.NewGuid(), DateTime.UtcNow, "test");
        var accountId = Guid.NewGuid();

        entry.AddLine(accountId, 1000m, 0m, "Cash debit");

        entry.Lines.Should().HaveCount(1);
        entry.Lines.First().Debit.Should().Be(1000m);
        entry.Lines.First().Credit.Should().Be(0m);
    }

    [Fact]
    public void JournalEntry_AddLine_BothDebitAndCredit_Throws()
    {
        var entry = JournalEntry.Create(Guid.NewGuid(), DateTime.UtcNow, "test");

        var act = () => entry.AddLine(Guid.NewGuid(), 100m, 100m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void JournalEntry_AddLine_BothZero_Throws()
    {
        var entry = JournalEntry.Create(Guid.NewGuid(), DateTime.UtcNow, "test");

        var act = () => entry.AddLine(Guid.NewGuid(), 0m, 0m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void JournalEntry_AddLine_NegativeAmount_Throws()
    {
        var entry = JournalEntry.Create(Guid.NewGuid(), DateTime.UtcNow, "test");

        var act = () => entry.AddLine(Guid.NewGuid(), -100m, 0m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void JournalEntry_AddLine_EmptyAccountId_Throws()
    {
        var entry = JournalEntry.Create(Guid.NewGuid(), DateTime.UtcNow, "test");

        var act = () => entry.AddLine(Guid.Empty, 100m, 0m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void JournalEntry_Validate_BalancedEntry_Succeeds()
    {
        var entry = JournalEntry.Create(Guid.NewGuid(), DateTime.UtcNow, "balanced");
        entry.AddLine(Guid.NewGuid(), 1000m, 0m, "Cash");
        entry.AddLine(Guid.NewGuid(), 0m, 1000m, "Revenue");

        var act = () => entry.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void JournalEntry_Validate_ImbalancedEntry_ThrowsImbalanceException()
    {
        var entry = JournalEntry.Create(Guid.NewGuid(), DateTime.UtcNow, "imbalanced");
        entry.AddLine(Guid.NewGuid(), 1000m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 500m);

        var act = () => entry.Validate();

        act.Should().Throw<JournalEntryImbalanceException>()
            .Which.TotalDebit.Should().Be(1000m);
    }

    [Fact]
    public void JournalEntry_Validate_SingleLine_Throws()
    {
        var entry = JournalEntry.Create(Guid.NewGuid(), DateTime.UtcNow, "single");
        entry.AddLine(Guid.NewGuid(), 100m, 0m);

        var act = () => entry.Validate();

        // Single line with only debit → imbalance check fires first
        act.Should().Throw<JournalEntryImbalanceException>();
    }

    [Fact]
    public void JournalEntry_Post_BalancedEntry_SetsPosted()
    {
        var entry = JournalEntry.Create(Guid.NewGuid(), DateTime.UtcNow, "post test");
        entry.AddLine(Guid.NewGuid(), 500m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 500m);

        entry.Post();

        entry.IsPosted.Should().BeTrue();
        entry.PostedAt.Should().NotBeNull();
    }

    [Fact]
    public void JournalEntry_Post_RaisesLedgerPostedEvent()
    {
        var entry = JournalEntry.Create(Guid.NewGuid(), DateTime.UtcNow, "event test");
        entry.AddLine(Guid.NewGuid(), 250m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 250m);
        entry.ClearDomainEvents();

        entry.Post();

        entry.DomainEvents.Should().ContainSingle(e => e is LedgerPostedEvent);
    }

    [Fact]
    public void JournalEntry_Post_AlreadyPosted_ThrowsInvalidOperation()
    {
        var entry = JournalEntry.Create(Guid.NewGuid(), DateTime.UtcNow, "double post");
        entry.AddLine(Guid.NewGuid(), 100m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 100m);
        entry.Post();

        var act = () => entry.Post();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void JournalEntry_AddLine_AfterPost_ThrowsInvalidOperation()
    {
        var entry = JournalEntry.Create(Guid.NewGuid(), DateTime.UtcNow, "locked");
        entry.AddLine(Guid.NewGuid(), 100m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 100m);
        entry.Post();

        var act = () => entry.AddLine(Guid.NewGuid(), 50m, 0m);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ToString_ContainsInvoiceNumber()
    {
        var invoice = new Invoice { InvoiceNumber = "INV-XYZ" };

        invoice.ToString().Should().Contain("INV-XYZ");
    }
}
