using FluentAssertions;
using MesTech.Application.Features.EInvoice.Queries;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.EInvoice;
using MesTech.Domain.Enums;
using Moq;

namespace MesTech.Tests.Unit.Application.EInvoice.Queries;

[Trait("Category", "Unit")]
public class GetEInvoicesHandlerTests
{
    private readonly Mock<IEInvoiceDocumentRepository> _repoMock = new();
    private readonly GetEInvoicesHandler _sut;

    public GetEInvoicesHandlerTests()
    {
        _sut = new GetEInvoicesHandler(_repoMock.Object);
    }

    private static EInvoiceDocument CreateTestDocument(string ettnSuffix = "001")
    {
        return EInvoiceDocument.Create(
            gibUuid: Guid.NewGuid().ToString(),
            ettnNo: $"GGB2026LIST0000{ettnSuffix}",
            scenario: EInvoiceScenario.TICARIFATURA,
            type: EInvoiceType.SATIS,
            issueDate: DateTime.UtcNow,
            sellerVkn: "0000000000",
            sellerTitle: "MesTech",
            buyerTitle: "Alici Test",
            providerId: "sovos",
            createdBy: "system");
    }

    [Fact]
    public async Task Handle_DocumentsExist_ShouldReturnPagedResult()
    {
        // Arrange
        var doc = CreateTestDocument();
        _repoMock.Setup(r => r.GetPagedAsync(
                It.IsAny<EInvoiceStatus?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<EInvoiceDocument> { doc } as IReadOnlyList<EInvoiceDocument>, 1));

        var query = new GetEInvoicesQuery(null, null, null, null, 1, 50);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task Handle_NoDocuments_ShouldReturnEmptyPagedResult()
    {
        // Arrange
        _repoMock.Setup(r => r.GetPagedAsync(
                It.IsAny<EInvoiceStatus?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<EInvoiceDocument>() as IReadOnlyList<EInvoiceDocument>, 0));

        // Act
        var result = await _sut.Handle(
            new GetEInvoicesQuery(null, null, null, null), CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => _sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ShouldPassFilterToRepository()
    {
        // Arrange
        _repoMock.Setup(r => r.GetPagedAsync(
                EInvoiceStatus.Draft, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<EInvoiceDocument>() as IReadOnlyList<EInvoiceDocument>, 0));

        var query = new GetEInvoicesQuery(null, null, EInvoiceStatus.Draft, null, 1, 10);

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        _repoMock.Verify(r => r.GetPagedAsync(
            EInvoiceStatus.Draft, null, null, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }
}
