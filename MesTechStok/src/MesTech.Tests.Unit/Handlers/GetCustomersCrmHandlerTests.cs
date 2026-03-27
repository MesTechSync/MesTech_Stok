using FluentAssertions;
using MesTech.Application.Features.Crm.Queries.GetCustomersCrm;
using MesTech.Application.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetCustomersCrmHandlerTests
{
    [Fact]
    public async Task Handle_NullRequest_Throws()
    {
        var sut = new GetCustomersCrmHandler(Mock.Of<ICrmDashboardQueryService>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
