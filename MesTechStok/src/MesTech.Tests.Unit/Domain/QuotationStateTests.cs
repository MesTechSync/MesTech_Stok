using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

// ════════════════════════════════════════════════════════
// Task 19: Quotation State Machine Tests
// ════════════════════════════════════════════════════════

[Trait("Category", "Unit")]
public class QuotationStateTests
{
    private static Quotation CreateDraftQuotation() => new()
    {
        QuotationNumber = "QT-001",
        CustomerName = "Test Customer",
    };

    private static Quotation CreateSentQuotation()
    {
        var q = CreateDraftQuotation();
        q.Send();
        return q;
    }

    private static Quotation CreateAcceptedQuotation()
    {
        var q = CreateSentQuotation();
        q.Accept();
        return q;
    }

    // ── Send ────────────────────────────────────────────

    [Fact]
    public void Send_FromDraft_ChangesStatusToSent()
    {
        // Arrange
        var quotation = CreateDraftQuotation();

        // Act
        quotation.Send();

        // Assert
        quotation.Status.Should().Be(QuotationStatus.Sent);
    }

    [Fact]
    public void Send_FromSent_ThrowsInvalidOperation()
    {
        // Arrange
        var quotation = CreateSentQuotation();

        // Act
        var act = () => quotation.Send();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Draft*");
    }

    // ── Accept ──────────────────────────────────────────

    [Fact]
    public void Accept_FromSent_ChangesStatusToAccepted()
    {
        // Arrange
        var quotation = CreateSentQuotation();

        // Act
        quotation.Accept();

        // Assert
        quotation.Status.Should().Be(QuotationStatus.Accepted);
    }

    [Fact]
    public void Accept_FromDraft_ThrowsInvalidOperation()
    {
        // Arrange
        var quotation = CreateDraftQuotation();

        // Act
        var act = () => quotation.Accept();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Sent*");
    }

    // ── Reject ──────────────────────────────────────────

    [Fact]
    public void Reject_FromSent_ChangesStatusToRejected()
    {
        // Arrange
        var quotation = CreateSentQuotation();

        // Act
        quotation.Reject();

        // Assert
        quotation.Status.Should().Be(QuotationStatus.Rejected);
    }

    [Fact]
    public void Reject_FromAccepted_ThrowsInvalidOperation()
    {
        // Arrange
        var quotation = CreateAcceptedQuotation();

        // Act
        var act = () => quotation.Reject();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Sent*");
    }

    // ── MarkAsExpired ───────────────────────────────────

    [Fact]
    public void MarkAsExpired_FromDraft_ChangesStatusToExpired()
    {
        // Arrange
        var quotation = CreateDraftQuotation();

        // Act
        quotation.MarkAsExpired();

        // Assert
        quotation.Status.Should().Be(QuotationStatus.Expired);
    }

    [Fact]
    public void MarkAsExpired_FromAccepted_DoesNotChange()
    {
        // Arrange
        var quotation = CreateAcceptedQuotation();

        // Act
        quotation.MarkAsExpired();

        // Assert — silently skips, stays Accepted
        quotation.Status.Should().Be(QuotationStatus.Accepted);
    }

    [Fact]
    public void MarkAsExpired_FromConverted_DoesNotChange()
    {
        // Arrange
        var quotation = CreateAcceptedQuotation();
        var invoiceId = Guid.NewGuid();
        quotation.MarkAsConverted(invoiceId);
        quotation.Status.Should().Be(QuotationStatus.Converted);

        // Act
        quotation.MarkAsExpired();

        // Assert — silently skips, stays Converted
        quotation.Status.Should().Be(QuotationStatus.Converted);
    }

    // ── MarkAsConverted ─────────────────────────────────

    [Fact]
    public void MarkAsConverted_FromAccepted_ChangesStatusAndSetsInvoiceId()
    {
        // Arrange
        var quotation = CreateAcceptedQuotation();
        var invoiceId = Guid.NewGuid();

        // Act
        quotation.MarkAsConverted(invoiceId);

        // Assert
        quotation.Status.Should().Be(QuotationStatus.Converted);
        quotation.ConvertedInvoiceId.Should().Be(invoiceId);
    }

    [Fact]
    public void MarkAsConverted_FromDraft_ThrowsInvalidOperation()
    {
        // Arrange
        var quotation = CreateDraftQuotation();

        // Act
        var act = () => quotation.MarkAsConverted(Guid.NewGuid());

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Accepted*");
    }

    // ── AddLine / CalculateTotals ───────────────────────

    [Fact]
    public void AddLine_CalculatesTotals()
    {
        // Arrange
        var quotation = CreateDraftQuotation();
        var line1 = new QuotationLine
        {
            ProductName = "Widget A",
            Quantity = 2,
            UnitPrice = 100m,
            TaxRate = 18m, // 18%
        };
        var line2 = new QuotationLine
        {
            ProductName = "Widget B",
            Quantity = 3,
            UnitPrice = 50m,
            TaxRate = 8m, // 8%
        };

        // Act
        quotation.AddLine(line1);
        quotation.AddLine(line2);

        // Assert
        // line1: LineTotal = 2 * 100 = 200, TaxAmount = 2 * 100 * 18 / 100 = 36
        // line2: LineTotal = 3 * 50 = 150,  TaxAmount = 3 * 50 * 8 / 100 = 12
        quotation.SubTotal.Should().Be(350m);  // 200 + 150
        quotation.TaxTotal.Should().Be(48m);   // 36 + 12
        quotation.GrandTotal.Should().Be(398m); // 350 + 48
        quotation.Lines.Should().HaveCount(2);
    }

    [Fact]
    public void MarkAsExpired_FromSent_ChangesStatusToExpired()
    {
        // Arrange
        var quotation = CreateSentQuotation();

        // Act
        quotation.MarkAsExpired();

        // Assert
        quotation.Status.Should().Be(QuotationStatus.Expired);
    }
}
