using FluentAssertions;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Features.Accounting.Queries.GetSettlementBatches;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Moq;

namespace MesTech.Tests.Unit.Features.Accounting;

[Trait("Category", "Unit")]
public class GetSettlementBatchesHandlerTests
{
    private readonly Mock<ISettlementBatchRepository> _repoMock = new();
    private readonly GetSettlementBatchesHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetSettlementBatchesHandlerTests()
        => _sut = new GetSettlementBatchesHandler(_repoMock.Object);

    [Fact]
    public async Task Handle_WithPlatformFilter_QueriesByPlatform()
    {
        _repoMock.Setup(r => r.GetByPlatformAsync(_tenantId, "Trendyol", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementBatch>());

        var query = new GetSettlementBatchesQuery(_tenantId, Platform: "Trendyol");
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
        _repoMock.Verify(r => r.GetByPlatformAsync(_tenantId, "Trendyol", It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_WithoutPlatform_QueriesByDateRange()
    {
        _repoMock.Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementBatch>());

        var query = new GetSettlementBatchesQuery(_tenantId);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
        _repoMock.Verify(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
