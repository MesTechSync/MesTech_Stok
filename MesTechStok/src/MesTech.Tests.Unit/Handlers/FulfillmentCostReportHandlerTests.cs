using FluentAssertions;
using MesTech.Application.Features.Reports.FulfillmentCostReport;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class FulfillmentCostReportHandlerTests
{
    [Fact]
    public async Task Handle_NullRequest_Throws()
    {
        var sut = new FulfillmentCostReportHandler(
            Mock.Of<IFulfillmentProviderFactory>(),
            Mock.Of<IShipmentCostRepository>(),
            Mock.Of<ILogger<FulfillmentCostReportHandler>>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<NullReferenceException>();
    }
}
