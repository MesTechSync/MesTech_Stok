using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetCashFlowTrend;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetCashFlowTrendHandlerTests
{
    private readonly Mock<IIncomeRepository> _incomeRepoMock = new();
    private readonly Mock<IExpenseRepository> _expenseRepoMock = new();
    private readonly GetCashFlowTrendHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetCashFlowTrendHandlerTests()
    {
        _sut = new GetCashFlowTrendHandler(
            _incomeRepoMock.Object,
            _expenseRepoMock.Object,
            Mock.Of<ILogger<GetCashFlowTrendHandler>>());
    }

    [Fact]
    public async Task Handle_NoData_ReturnsEmptyMonths()
    {
        _incomeRepoMock.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), _tenantId))
            .ReturnsAsync(new List<Income>().AsReadOnly());
        _expenseRepoMock.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), _tenantId))
            .ReturnsAsync(new List<Expense>().AsReadOnly());

        var query = new GetCashFlowTrendQuery(_tenantId, 3);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Months.Should().HaveCount(3);
        result.CumulativeNet.Should().Be(0m);
    }
}
