using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetReconciliationMatches;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using Moq;

namespace MesTech.Tests.Unit.Features.Accounting;

[Trait("Category", "Unit")]
public class GetReconciliationMatchesHandlerTests
{
    private readonly Mock<IReconciliationMatchRepository> _repoMock = new();
    private readonly GetReconciliationMatchesHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetReconciliationMatchesHandlerTests()
        => _sut = new GetReconciliationMatchesHandler(_repoMock.Object);

    [Fact]
    public async Task Handle_DefaultStatus_QueriesNeedsReview()
    {
        _repoMock.Setup(r => r.GetByStatusAsync(_tenantId, ReconciliationStatus.NeedsReview, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReconciliationMatch>());

        var query = new GetReconciliationMatchesQuery(_tenantId);
        await _sut.Handle(query, CancellationToken.None);

        _repoMock.Verify(r => r.GetByStatusAsync(_tenantId, ReconciliationStatus.NeedsReview, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
