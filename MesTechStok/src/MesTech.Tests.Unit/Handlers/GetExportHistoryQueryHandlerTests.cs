using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Queries;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// DEV 5 — G376: GetExportHistoryQueryHandler unit tests.
/// Handler is a STUB — returns empty PagedResult until IExportHistoryRepository is implemented (Sprint B-06).
/// Tests verify stub contract: empty items, correct page/pageSize, zero total count.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetExportHistoryQueryHandlerTests
{
    private readonly GetExportHistoryQueryHandler _sut = new();

    [Fact]
    public async Task Handle_DefaultParams_ReturnsEmptyPagedResult()
    {
        var query = new GetExportHistoryQuery();

        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
        result.TotalPages.Should().Be(0);
        result.HasPrevious.Should().BeFalse();
        result.HasNext.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithPoolId_ReturnsEmptyResult()
    {
        var query = new GetExportHistoryQuery(PoolId: Guid.NewGuid());

        var result = await _sut.Handle(query, CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_CustomPageSize_RespectsParameter()
    {
        var query = new GetExportHistoryQuery(Page: 3, PageSize: 50);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.Page.Should().Be(3);
        result.PageSize.Should().Be(50);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CancellationToken_DoesNotThrow()
    {
        var cts = new CancellationTokenSource();
        var query = new GetExportHistoryQuery();

        var act = () => _sut.Handle(query, cts.Token);

        await act.Should().NotThrowAsync();
    }
}
