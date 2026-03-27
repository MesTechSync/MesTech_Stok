using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetReconciliationDashboard;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetReconciliationDashboardHandlerTests
{
    private readonly Mock<IReconciliationMatchRepository> _matchRepoMock = new();
    private readonly Mock<ISettlementBatchRepository> _settlementRepoMock = new();
    private readonly GetReconciliationDashboardHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetReconciliationDashboardHandlerTests()
    {
        _sut = new GetReconciliationDashboardHandler(_matchRepoMock.Object, _settlementRepoMock.Object);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
