using FluentAssertions;
using MesTech.Application.Queries.GetBarcodeScanLogs;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Queries;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetBarcodeScanLogsHandlerTests
{
    private readonly Mock<IBarcodeScanLogRepository> _repo = new();

    private GetBarcodeScanLogsHandler CreateHandler() => new(_repo.Object);

    [Fact]
    public void Constructor_NullRepository_ShouldThrow()
    {
        var act = () => new GetBarcodeScanLogsHandler(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrow()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_WithLogs_ShouldReturnPagedResult()
    {
        var logs = new List<BarcodeScanLog>
        {
            new() { Barcode = "8690000111111", Format = "EAN13", Source = "Scanner1", IsValid = true },
            new() { Barcode = "8690000222222", Format = "EAN13", Source = "Scanner2", IsValid = false }
        };

        _repo.Setup(r => r.GetPagedAsync(1, 50, null, null, null, null, null))
            .ReturnsAsync(logs.AsReadOnly());
        _repo.Setup(r => r.GetCountAsync(null, null, null, null, null))
            .ReturnsAsync(2);

        var handler = CreateHandler();
        var query = new GetBarcodeScanLogsQuery();

        var result = await handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(50);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_EmptyResult_ShouldReturnEmptyItems()
    {
        _repo.Setup(r => r.GetPagedAsync(1, 50, null, null, null, null, null))
            .ReturnsAsync(new List<BarcodeScanLog>().AsReadOnly());
        _repo.Setup(r => r.GetCountAsync(null, null, null, null, null))
            .ReturnsAsync(0);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetBarcodeScanLogsQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithFilters_ShouldPassFiltersToRepository()
    {
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 3, 31);

        _repo.Setup(r => r.GetPagedAsync(2, 10, "8690", "Scanner1", true, from, to))
            .ReturnsAsync(new List<BarcodeScanLog>().AsReadOnly());
        _repo.Setup(r => r.GetCountAsync("8690", "Scanner1", true, from, to))
            .ReturnsAsync(0);

        var handler = CreateHandler();
        var query = new GetBarcodeScanLogsQuery(
            Page: 2, PageSize: 10,
            BarcodeFilter: "8690", SourceFilter: "Scanner1",
            IsValidFilter: true, From: from, To: to);

        await handler.Handle(query, CancellationToken.None);

        _repo.Verify(r => r.GetPagedAsync(2, 10, "8690", "Scanner1", true, from, to), Times.Once);
        _repo.Verify(r => r.GetCountAsync("8690", "Scanner1", true, from, to), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectPagination()
    {
        _repo.Setup(r => r.GetPagedAsync(3, 25, null, null, null, null, null))
            .ReturnsAsync(new List<BarcodeScanLog>().AsReadOnly());
        _repo.Setup(r => r.GetCountAsync(null, null, null, null, null))
            .ReturnsAsync(100);

        var handler = CreateHandler();
        var query = new GetBarcodeScanLogsQuery(Page: 3, PageSize: 25);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Page.Should().Be(3);
        result.PageSize.Should().Be(25);
        result.TotalCount.Should().Be(100);
    }
}
