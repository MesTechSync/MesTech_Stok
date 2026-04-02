using FluentAssertions;
using MesTech.Application.Features.Erp.Queries.GetErpSyncLogs;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Entities.Erp;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetErpSyncLogsHandlerTests
{
    [Fact]
    public async Task Handle_NullRequest_Throws()
    {
        var sut = new GetErpSyncLogsHandler(Mock.Of<IErpSyncLogRepository>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<NullReferenceException>();
    }
}
