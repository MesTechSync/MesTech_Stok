using MesTech.Domain.Entities.EInvoice;
using MesTech.Domain.Enums;
using MesTech.Domain.Events.EInvoice;
using MesTech.Domain.Exceptions;

namespace MesTech.Tests.Unit.Domain;

public class EInvoiceDocumentTests
{
    [Fact]
    public void Create_ValidData_Succeeds()
    {
        var uuid = Guid.NewGuid().ToString();
        var doc = EInvoiceDocument.Create(
            gibUuid: uuid, ettnNo: "ABC2026000000001",
            scenario: EInvoiceScenario.TEMELFATURA, type: EInvoiceType.SATIS,
            issueDate: DateTime.UtcNow, sellerVkn: "1234567890",
            sellerTitle: "Test Sirketi A.S.", buyerTitle: "Alici Ltd.",
            providerId: "Sovos", createdBy: "dev1");
        Assert.Equal(uuid, doc.GibUuid);
        Assert.Equal("ABC2026000000001", doc.EttnNo);
        Assert.Equal(EInvoiceStatus.Draft, doc.Status);
    }

    [Fact]
    public void Create_InvalidUuid_ThrowsDomainValidation()
    {
        Assert.Throws<DomainValidationException>(() =>
            EInvoiceDocument.Create("not-a-guid", "ETTN001",
                EInvoiceScenario.TEMELFATURA, EInvoiceType.SATIS,
                DateTime.UtcNow, "1234567890", "Satici", "Alici", "Sovos", "dev1"));
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("123456789012")]
    [InlineData("")]
    public void Create_InvalidVkn_ThrowsDomainValidation(string vkn)
    {
        Assert.Throws<DomainValidationException>(() =>
            EInvoiceDocument.Create(Guid.NewGuid().ToString(), "ETTN001",
                EInvoiceScenario.TEMELFATURA, EInvoiceType.SATIS,
                DateTime.UtcNow, vkn, "Satici", "Alici", "Sovos", "dev1"));
    }

    [Theory]
    [InlineData("1234567890")]
    [InlineData("12345678901")]
    public void Create_ValidVkn_Succeeds(string vkn)
    {
        var doc = EInvoiceDocument.Create(Guid.NewGuid().ToString(), "ETTN001",
            EInvoiceScenario.TEMELFATURA, EInvoiceType.SATIS,
            DateTime.UtcNow, vkn, "Satici", "Alici", "Sovos", "dev1");
        Assert.Equal(vkn, doc.SellerVkn);
    }

    [Fact]
    public void Create_RaisesEInvoiceCreatedEvent()
    {
        var doc = CreateValidDoc();
        Assert.Single(doc.DomainEvents);
        Assert.IsType<EInvoiceCreatedEvent>(doc.DomainEvents[0]);
    }

    [Fact]
    public void MarkAsSent_DraftDoc_TransitionsToSent()
    {
        var doc = CreateValidDoc();
        doc.MarkAsSent("sovos-ref-001", creditUsed: 1);
        Assert.Equal(EInvoiceStatus.Sent, doc.Status);
        Assert.Equal("sovos-ref-001", doc.ProviderRef);
        Assert.Equal(1, doc.CreditUsed);
    }

    [Fact]
    public void MarkAsSent_RaisesEInvoiceSentEvent()
    {
        var doc = CreateValidDoc();
        doc.ClearDomainEvents();
        doc.MarkAsSent("ref-001", 1);
        Assert.Single(doc.DomainEvents);
        Assert.IsType<EInvoiceSentEvent>(doc.DomainEvents[0]);
    }

    [Fact]
    public void Cancel_SentDoc_TransitionsToCancelled()
    {
        var doc = CreateValidDoc();
        doc.MarkAsSent("ref-001", 1);
        doc.Cancel("Test iptal", "dev1");
        Assert.Equal(EInvoiceStatus.Cancelled, doc.Status);
    }

    [Fact]
    public void Cancel_AlreadyCancelled_ThrowsBusinessRule()
    {
        var doc = CreateValidDoc();
        doc.Cancel("Ilk iptal", "dev1");
        Assert.Throws<BusinessRuleException>(() => doc.Cancel("Ikinci iptal", "dev1"));
    }

    [Fact]
    public void MarkAsSent_CancelledDoc_ThrowsBusinessRule()
    {
        var doc = CreateValidDoc();
        doc.Cancel("iptal", "dev1");
        Assert.Throws<BusinessRuleException>(() => doc.MarkAsSent("ref", 1));
    }

    [Fact]
    public void SetFinancials_NegativePayable_ThrowsDomainValidation()
    {
        var doc = CreateValidDoc();
        Assert.Throws<DomainValidationException>(() =>
            doc.SetFinancials(100, 80, 96, 20, 16, -5));
    }

    [Fact]
    public void SetPdfUrl_ValidUrl_Sets()
    {
        var doc = CreateValidDoc();
        doc.SetPdfUrl("https://sovos.com/pdf/test.pdf");
        Assert.StartsWith("https://", doc.PdfUrl);
    }

    [Fact]
    public void SetPdfUrl_EmptyUrl_ThrowsDomainValidation()
    {
        var doc = CreateValidDoc();
        Assert.Throws<DomainValidationException>(() => doc.SetPdfUrl(""));
    }

    private static EInvoiceDocument CreateValidDoc() =>
        EInvoiceDocument.Create(Guid.NewGuid().ToString(), "ABC2026000000001",
            EInvoiceScenario.TEMELFATURA, EInvoiceType.SATIS,
            DateTime.UtcNow, "1234567890", "Test Satici A.S.",
            "Test Alici Ltd.", "Sovos", "dev1");
}
