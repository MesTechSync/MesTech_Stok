using FluentAssertions;
using MesTech.Application.Features.Reports.CargoPerformanceReport;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class CargoPerformanceReportHandlerTests
{
    [Fact]
    public async Task Handle_NullRequest_Throws()
    {
        var sut = new CargoPerformanceReportHandler(
            Mock.Of<ICargoExpenseRepository>(), Mock.Of<IOrderRepository>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
