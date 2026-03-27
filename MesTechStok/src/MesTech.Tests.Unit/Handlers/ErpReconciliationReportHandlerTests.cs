using FluentAssertions;
using MesTech.Application.Features.Reports.ErpReconciliationReport;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Application.Interfaces.Erp;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class ErpReconciliationReportHandlerTests
{
    [Fact]
    public async Task Handle_NullRequest_Throws()
    {
        var sut = new ErpReconciliationReportHandler(
            Mock.Of<IErpAdapterFactory>(), Mock.Of<ICounterpartyRepository>(),
            Mock.Of<ILogger<ErpReconciliationReportHandler>>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
