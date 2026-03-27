using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetProfitReport;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Services;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetProfitReportHandlerTests
{
    private readonly Mock<IProfitReportRepository> _repoMock = new();
    private readonly Mock<IProfitCalculationService> _profitServiceMock = new();
    private readonly GetProfitReportHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetProfitReportHandlerTests()
    {
        _sut = new GetProfitReportHandler(_repoMock.Object, _profitServiceMock.Object);
    }

    [Fact]
    public async Task Handle_NoReport_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByPeriodAsync(_tenantId, "2026-03", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProfitReport>().AsReadOnly());

        var query = new GetProfitReportQuery(_tenantId, "2026-03");
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
