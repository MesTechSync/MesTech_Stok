using FluentAssertions;
using MesTech.Avalonia.ViewModels.Accounting;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class IncomeExpenseListViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly IncomeExpenseListViewModel _sut;

    public IncomeExpenseListViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _mediatorMock.Setup(m => m.Send(It.IsAny<MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseList.GetIncomeExpenseListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseList.IncomeExpenseListResultDto(
                Items: Array.Empty<MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseList.IncomeExpenseItemDto>(),
                TotalCount: 0));
        _sut = new IncomeExpenseListViewModel(_mediatorMock.Object, Mock.Of<MesTech.Domain.Interfaces.ICurrentUserService>());
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        _sut.Title.Should().Be("Gelir / Gider Listesi");
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.IsEmpty.Should().BeFalse();
        _sut.SelectedTypeFilter.Should().Be("Tumu");
        _sut.SelectedCategory.Should().Be("Tumu");
        _sut.CurrentPage.Should().Be(1);
        _sut.TotalPages.Should().Be(1);
        _sut.Items.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ShouldPopulateFilterOptions()
    {
        _sut.TypeFilters.Should().Contain("Tumu");
        _sut.TypeFilters.Should().Contain("Gelir");
        _sut.TypeFilters.Should().Contain("Gider");
        _sut.Categories.Should().Contain("Satis");
        _sut.Categories.Should().Contain("Kargo");
        _sut.Categories.Should().Contain("Komisyon");
    }
}
