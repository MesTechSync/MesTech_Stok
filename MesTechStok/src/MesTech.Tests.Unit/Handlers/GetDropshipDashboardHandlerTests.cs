using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipDashboard;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetDropshipDashboardHandlerTests
{
    [Fact]
    public async Task Handle_NullRequest_Throws()
    {
        var sut = new GetDropshipDashboardHandler(
            Mock.Of<IDropshipSupplierRepository>(), Mock.Of<ISupplierFeedRepository>(),
            Mock.Of<IDropshipProductRepository>(), Mock.Of<IDropshipOrderRepository>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
