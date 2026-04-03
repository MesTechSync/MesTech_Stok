using FluentAssertions;
using MesTech.Avalonia.ViewModels.Accounting;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class IncomeExpenseDashboardViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly IncomeExpenseDashboardViewModel _sut;

    public IncomeExpenseDashboardViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _mediatorMock.Setup(m => m.Send(It.IsAny<MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseSummary.GetIncomeExpenseSummaryQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseSummary.IncomeExpenseSummaryDto(0, 0, 0, 0, 0));
        _mediatorMock.Setup(m => m.Send(It.IsAny<MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseList.GetIncomeExpenseListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseList.IncomeExpenseListResultDto(
                Items: Array.Empty<MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseList.IncomeExpenseItemDto>(),
                TotalCount: 0));
        _sut = new IncomeExpenseDashboardViewModel(_mediatorMock.Object, Mock.Of<ICurrentUserService>());
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        _sut.Title.Should().Be("Gelir / Gider Ozeti");
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.IsEmpty.Should().BeFalse();
        _sut.TotalIncome.Should().Be(0);
        _sut.TotalExpense.Should().Be(0);
        _sut.NetProfit.Should().Be(0);
        _sut.RecentTransactions.Should().BeEmpty();
    }
}
