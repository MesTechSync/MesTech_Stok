using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetPendingReviews;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Queries;

/// <summary>
/// GetPendingReviewsHandler tests — paginated reconciliation matches needing review.
/// Verifies pagination, enrichment from settlement/bank repositories, and empty results.
/// </summary>
[Trait("Category", "Unit")]
public class GetPendingReviewsHandlerTests
{
    private readonly Mock<IReconciliationMatchRepository> _matchRepoMock;
    private readonly Mock<ISettlementBatchRepository> _settlementRepoMock;
    private readonly Mock<IBankTransactionRepository> _bankTxRepoMock;
    private readonly GetPendingReviewsHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetPendingReviewsHandlerTests()
    {
        _matchRepoMock = new Mock<IReconciliationMatchRepository>();
        _settlementRepoMock = new Mock<ISettlementBatchRepository>();
        _bankTxRepoMock = new Mock<IBankTransactionRepository>();

        _sut = new GetPendingReviewsHandler(
            _matchRepoMock.Object,
            _settlementRepoMock.Object,
            _bankTxRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WithMatches_ReturnsPaginatedEnrichedResults()
    {
        // Arrange
        var settlementId = Guid.NewGuid();
        var bankTxId = Guid.NewGuid();

        var match = ReconciliationMatch.Create(
            _tenantId,
            DateTime.UtcNow,
            0.85m,
            ReconciliationStatus.NeedsReview,
            settlementBatchId: settlementId,
            bankTransactionId: bankTxId);

        var settlement = SettlementBatch.Create(
            _tenantId, "Trendyol",
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc),
            10000m, 1500m, 8500m);

        var bankTx = BankTransaction.Create(
            _tenantId, Guid.NewGuid(),
            new DateTime(2026, 3, 16, 0, 0, 0, DateTimeKind.Utc),
            8500m, "Trendyol hesap kesimi");

        _matchRepoMock
            .Setup(r => r.GetPendingReviewsPagedAsync(_tenantId, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ReconciliationMatch> { match } as IReadOnlyList<ReconciliationMatch>, 1));

        _settlementRepoMock
            .Setup(r => r.GetByIdAsync(settlementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settlement);

        _bankTxRepoMock
            .Setup(r => r.GetByIdAsync(bankTxId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bankTx);

        var query = new GetPendingReviewsQuery(_tenantId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.TotalPages.Should().Be(1);
        result.Page.Should().Be(1);

        var item = result.Items[0];
        item.Confidence.Should().Be(0.85m);
        item.SettlementPlatform.Should().Be("Trendyol");
        item.SettlementTotalNet.Should().Be(8500m);
        item.BankTransactionAmount.Should().Be(8500m);
        item.BankTransactionDescription.Should().Be("Trendyol hesap kesimi");
    }

    [Fact]
    public async Task Handle_NoMatches_ReturnsEmptyResult()
    {
        // Arrange
        _matchRepoMock
            .Setup(r => r.GetPendingReviewsPagedAsync(_tenantId, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<ReconciliationMatch>() as IReadOnlyList<ReconciliationMatch>, 0));

        var query = new GetPendingReviewsQuery(_tenantId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task Handle_MatchWithoutSettlementOrBankTx_ReturnsPartialDto()
    {
        // Arrange — match with no settlement or bank transaction IDs
        var match = ReconciliationMatch.Create(
            _tenantId,
            DateTime.UtcNow,
            0.50m,
            ReconciliationStatus.NeedsReview,
            settlementBatchId: null,
            bankTransactionId: null);

        _matchRepoMock
            .Setup(r => r.GetPendingReviewsPagedAsync(_tenantId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ReconciliationMatch> { match } as IReadOnlyList<ReconciliationMatch>, 1));

        var query = new GetPendingReviewsQuery(_tenantId, PageSize: 10);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.SettlementPlatform.Should().BeNull();
        item.SettlementTotalNet.Should().BeNull();
        item.BankTransactionAmount.Should().BeNull();
        item.BankTransactionDescription.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Pagination_CalculatesTotalPagesCorrectly()
    {
        // Arrange — 25 total items, page size 10 = 3 pages
        _matchRepoMock
            .Setup(r => r.GetPendingReviewsPagedAsync(_tenantId, 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<ReconciliationMatch>() as IReadOnlyList<ReconciliationMatch>, 25));

        var query = new GetPendingReviewsQuery(_tenantId, PageSize: 10, Page: 2);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(25);
        result.TotalPages.Should().Be(3);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
    }
}
