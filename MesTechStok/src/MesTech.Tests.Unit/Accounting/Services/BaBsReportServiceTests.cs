using FluentAssertions;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Infrastructure.Finance;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Services;

[Trait("Category", "Unit")]
public class BaBsReportServiceTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Mock<IAccountingDocumentRepository> _docRepoMock = new();
    private readonly Mock<ICounterpartyRepository> _cpRepoMock = new();
    private readonly Mock<ILogger<BaBsReportService>> _loggerMock = new();
    private readonly BaBsReportService _sut;

    public BaBsReportServiceTests()
    {
        _sut = new BaBsReportService(
            _docRepoMock.Object,
            _cpRepoMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GenerateBaBsReport_NoDocs_ShouldReturnEmptyReport()
    {
        SetupEmptyRepos();

        var result = await _sut.GenerateBaBsReportAsync(_tenantId, 2026, 3);

        result.BaEntries.Should().BeEmpty();
        result.BsEntries.Should().BeEmpty();
        result.Year.Should().Be(2026);
        result.Month.Should().Be(3);
    }

    [Fact]
    public async Task GenerateBaBsReport_PurchasesBelow5000_ShouldBeExcluded()
    {
        var cpId = Guid.NewGuid();
        SetupCounterparty(cpId, "Tedarikci A", "1234567890");
        SetupPurchaseDocs(cpId, 4_999m); // Below threshold
        SetupSalesDocs();

        var result = await _sut.GenerateBaBsReportAsync(_tenantId, 2026, 3);

        result.BaEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateBaBsReport_PurchasesAbove5000_ShouldBeIncluded()
    {
        var cpId = Guid.NewGuid();
        SetupCounterparty(cpId, "Tedarikci A", "1234567890");
        SetupPurchaseDocs(cpId, 10_000m); // Above threshold
        SetupSalesDocs();

        var result = await _sut.GenerateBaBsReportAsync(_tenantId, 2026, 3);

        result.BaEntries.Should().HaveCount(1);
        result.BaEntries[0].Name.Should().Be("Tedarikci A");
        result.BaEntries[0].VKN.Should().Be("1234567890");
        result.BaEntries[0].TotalAmount.Should().Be(10_000m);
    }

    [Fact]
    public async Task GenerateBaBsReport_SalesAbove5000_ShouldBeInBsForm()
    {
        var cpId = Guid.NewGuid();
        SetupCounterparty(cpId, "Musteri X", "9876543210");
        SetupPurchaseDocs();
        SetupSalesDocs(cpId, 8_000m);

        var result = await _sut.GenerateBaBsReportAsync(_tenantId, 2026, 3);

        result.BsEntries.Should().HaveCount(1);
        result.BsEntries[0].Name.Should().Be("Musteri X");
        result.BsEntries[0].TotalAmount.Should().Be(8_000m);
    }

    [Fact]
    public async Task GenerateBaBsReport_InvalidMonth_ShouldThrow()
    {
        var act = () => _sut.GenerateBaBsReportAsync(_tenantId, 2026, 13);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task GenerateBaBsReport_EmptyTenantId_ShouldThrow()
    {
        var act = () => _sut.GenerateBaBsReportAsync(Guid.Empty, 2026, 3);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void GenerateBaBsReport_MinimumThreshold_Is5000()
    {
        BaBsReportService.MinimumThreshold.Should().Be(5_000m);
    }

    // ── Setup Helpers ──

    private void SetupEmptyRepos()
    {
        _docRepoMock.Setup(r => r.GetByTypeAsync(_tenantId, DocumentType.PurchaseInvoice, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AccountingDocument>());
        _docRepoMock.Setup(r => r.GetByTypeAsync(_tenantId, DocumentType.SalesInvoice, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AccountingDocument>());
        _cpRepoMock.Setup(r => r.GetAllAsync(_tenantId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Counterparty>());
    }

    private void SetupCounterparty(Guid cpId, string name, string vkn)
    {
        var cp = Counterparty.Create(_tenantId, name, CounterpartyType.Supplier, vkn);
        // Use reflection to set the Id since it's protected
        typeof(MesTech.Domain.Common.BaseEntity)
            .GetProperty("Id")!.SetValue(cp, cpId);

        _cpRepoMock.Setup(r => r.GetAllAsync(_tenantId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { cp });
    }

    private void SetupPurchaseDocs(Guid? cpId = null, decimal amount = 0)
    {
        var docs = new List<AccountingDocument>();
        if (cpId.HasValue && amount > 0)
        {
            var doc = AccountingDocument.Create(
                _tenantId, "fatura.pdf", "application/pdf", 1000, "/store/fatura.pdf",
                DocumentType.PurchaseInvoice, DocumentSource.Upload,
                cpId, amount);
            // Set CreatedAt to March 2026
            doc.CreatedAt = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc);
            docs.Add(doc);
        }
        _docRepoMock.Setup(r => r.GetByTypeAsync(_tenantId, DocumentType.PurchaseInvoice, It.IsAny<CancellationToken>()))
            .ReturnsAsync(docs.AsReadOnly());
    }

    private void SetupSalesDocs(Guid? cpId = null, decimal amount = 0)
    {
        var docs = new List<AccountingDocument>();
        if (cpId.HasValue && amount > 0)
        {
            var doc = AccountingDocument.Create(
                _tenantId, "satis-fatura.pdf", "application/pdf", 1000, "/store/satis.pdf",
                DocumentType.SalesInvoice, DocumentSource.Upload,
                cpId, amount);
            doc.CreatedAt = new DateTime(2026, 3, 20, 10, 0, 0, DateTimeKind.Utc);
            docs.Add(doc);
        }
        _docRepoMock.Setup(r => r.GetByTypeAsync(_tenantId, DocumentType.SalesInvoice, It.IsAny<CancellationToken>()))
            .ReturnsAsync(docs.AsReadOnly());
    }
}
