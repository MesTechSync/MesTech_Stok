using FluentAssertions;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Accounting.Events;

namespace MesTech.Tests.Unit.Accounting.Entities;

[Trait("Category", "Unit")]
public class AccountingDocumentTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var doc = AccountingDocument.Create(
            _tenantId, "fatura.pdf", "application/pdf", 1024,
            "/docs/fatura.pdf", DocumentType.Invoice, DocumentSource.Upload);

        doc.Should().NotBeNull();
        doc.FileName.Should().Be("fatura.pdf");
        doc.MimeType.Should().Be("application/pdf");
        doc.FileSize.Should().Be(1024);
        doc.StoragePath.Should().Be("/docs/fatura.pdf");
        doc.DocumentType.Should().Be(DocumentType.Invoice);
        doc.DocumentSource.Should().Be(DocumentSource.Upload);
    }

    [Fact]
    public void Create_ShouldRaiseDocumentReceivedEvent()
    {
        var doc = AccountingDocument.Create(
            _tenantId, "test.pdf", "application/pdf", 512,
            "/docs/test.pdf", DocumentType.Invoice, DocumentSource.Email);

        doc.DomainEvents.Should().ContainSingle(e => e is DocumentReceivedEvent);
        var evt = doc.DomainEvents.OfType<DocumentReceivedEvent>().Single();
        evt.FileName.Should().Be("test.pdf");
        evt.DocumentType.Should().Be(DocumentType.Invoice);
        evt.Source.Should().Be(DocumentSource.Email);
    }

    [Fact]
    public void Create_WithEmptyFileName_ShouldThrow()
    {
        var act = () => AccountingDocument.Create(
            _tenantId, "", "application/pdf", 512,
            "/docs/test.pdf", DocumentType.Invoice, DocumentSource.Upload);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyStoragePath_ShouldThrow()
    {
        var act = () => AccountingDocument.Create(
            _tenantId, "test.pdf", "application/pdf", 512,
            "", DocumentType.Invoice, DocumentSource.Upload);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithCounterpartyId_ShouldSetCorrectly()
    {
        var cpId = Guid.NewGuid();
        var doc = AccountingDocument.Create(
            _tenantId, "test.pdf", "application/pdf", 512,
            "/docs/test.pdf", DocumentType.Invoice, DocumentSource.Upload,
            counterpartyId: cpId);

        doc.CounterpartyId.Should().Be(cpId);
    }

    [Fact]
    public void Create_WithAmount_ShouldSetCorrectly()
    {
        var doc = AccountingDocument.Create(
            _tenantId, "test.pdf", "application/pdf", 512,
            "/docs/test.pdf", DocumentType.Invoice, DocumentSource.Upload,
            amount: 1500.50m);

        doc.Amount.Should().Be(1500.50m);
    }

    [Fact]
    public void UpdateExtractedData_ShouldSetCorrectly()
    {
        var doc = AccountingDocument.Create(
            _tenantId, "test.pdf", "application/pdf", 512,
            "/docs/test.pdf", DocumentType.Invoice, DocumentSource.Upload);

        doc.UpdateExtractedData("{\"total\": 1500}");

        doc.ExtractedData.Should().Be("{\"total\": 1500}");
    }

    [Fact]
    public void UpdateExtractedData_ShouldUpdateUpdatedAt()
    {
        var doc = AccountingDocument.Create(
            _tenantId, "test.pdf", "application/pdf", 512,
            "/docs/test.pdf", DocumentType.Invoice, DocumentSource.Upload);

        doc.UpdateExtractedData("data");

        doc.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(DocumentType.Invoice)]
    [InlineData(DocumentType.Receipt)]
    [InlineData(DocumentType.BankStatement)]
    [InlineData(DocumentType.Settlement)]
    [InlineData(DocumentType.Contract)]
    [InlineData(DocumentType.Other)]
    public void Create_WithDifferentDocumentTypes_ShouldSetCorrectly(DocumentType type)
    {
        var doc = AccountingDocument.Create(
            _tenantId, "test.pdf", "application/pdf", 512,
            "/docs/test.pdf", type, DocumentSource.Upload);

        doc.DocumentType.Should().Be(type);
    }

    [Theory]
    [InlineData(DocumentSource.WhatsApp)]
    [InlineData(DocumentSource.Telegram)]
    [InlineData(DocumentSource.Email)]
    [InlineData(DocumentSource.Upload)]
    [InlineData(DocumentSource.Scanner)]
    [InlineData(DocumentSource.API)]
    public void Create_WithDifferentSources_ShouldSetCorrectly(DocumentSource source)
    {
        var doc = AccountingDocument.Create(
            _tenantId, "test.pdf", "application/pdf", 512,
            "/docs/test.pdf", DocumentType.Invoice, source);

        doc.DocumentSource.Should().Be(source);
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        var d1 = AccountingDocument.Create(_tenantId, "a.pdf", "application/pdf", 100, "/a.pdf", DocumentType.Invoice, DocumentSource.Upload);
        var d2 = AccountingDocument.Create(_tenantId, "b.pdf", "application/pdf", 200, "/b.pdf", DocumentType.Invoice, DocumentSource.Upload);

        d1.Id.Should().NotBe(d2.Id);
    }
}
