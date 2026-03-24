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
        _sut = new IncomeExpenseListViewModel(_mediatorMock.Object);
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

    [Fact]
    public async Task LoadAsync_ShouldPopulateItems()
    {
        await _sut.LoadAsync();

        _sut.Items.Should().NotBeEmpty();
        _sut.TotalCount.Should().BeGreaterThan(0);
        _sut.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldSetLoadingStates()
    {
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IncomeExpenseListViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        await _sut.LoadAsync();

        loadingStates.Should().ContainInOrder(true, false);
        _sut.IsLoading.Should().BeFalse();
    }
}
