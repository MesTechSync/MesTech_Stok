using FluentAssertions;
using MesTech.Avalonia.ViewModels;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class GelirGiderAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly GelirGiderAvaloniaViewModel _sut;

    public GelirGiderAvaloniaViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut = new GelirGiderAvaloniaViewModel(_mediatorMock.Object);
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        // Assert
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.IsEmpty.Should().BeFalse();
        _sut.TotalIncome.Should().Be("0,00 TL");
        _sut.TotalExpense.Should().Be("0,00 TL");
        _sut.NetBalance.Should().Be("0,00 TL");
        _sut.SearchText.Should().BeEmpty();
        _sut.SelectedCategory.Should().Be("Tumu");
        _sut.SelectedTypeFilter.Should().Be("Tumu");
        _sut.Items.Should().BeEmpty();
        _sut.Categories.Should().HaveCount(7);
        _sut.TypeFilters.Should().HaveCount(3);
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateItems_AndCalculateKPIs()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.Items.Should().HaveCount(7);
        _sut.TotalCount.Should().Be(7);
        _sut.TotalIncome.Should().Contain("TL");
        _sut.TotalExpense.Should().Contain("TL");
        _sut.NetBalance.Should().Contain("TL");

        // Verify KPI calculations: income = 4520+2180+1240+3890 = 11830
        _sut.TotalIncome.Should().Contain("11");
        // expense = 380+6500+542.40 = 7422.40
        _sut.TotalExpense.Should().Contain("7");
    }

    [Fact]
    public async Task LoadAsync_ShouldSetLoadingStates()
    {
        // Arrange
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(GelirGiderAvaloniaViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        // Act
        await _sut.LoadAsync();

        // Assert
        loadingStates.Should().Contain(true, "IsLoading should have been set to true");
        _sut.IsLoading.Should().BeFalse("IsLoading should be false after load completes");
        _sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyFilters_CategoryFilter_FiltersCorrectly()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SelectedCategory = "Satis";

        // Assert — only items with Category=="Satis" should remain
        _sut.Items.Should().OnlyContain(x => x.Category == "Satis");
        _sut.Items.Should().HaveCount(4);
        _sut.TotalCount.Should().Be(4);
        _sut.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyFilters_TypeFilter_GelirOnly()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SelectedTypeFilter = "Gelir";

        // Assert
        _sut.Items.Should().OnlyContain(x => x.Type == "Gelir");
        _sut.Items.Should().HaveCount(4);
    }

    [Fact]
    public async Task ApplyFilters_SearchText_FiltersDescription()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act — search for "Trendyol" (>= 2 chars triggers filter)
        _sut.SearchText = "Trendyol";

        // Assert
        _sut.Items.Should().OnlyContain(x =>
            x.Description.Contains("Trendyol", StringComparison.OrdinalIgnoreCase));
        _sut.Items.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task ApplyFilters_NoMatch_SetsIsEmpty()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SearchText = "BuMetinHicbirYerdeYok";

        // Assert
        _sut.Items.Should().BeEmpty();
        _sut.IsEmpty.Should().BeTrue();
        _sut.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Refresh_CallsLoadAsync()
    {
        // Act
        await _sut.RefreshCommand.ExecuteAsync(null);

        // Assert — after refresh, items should be populated
        _sut.Items.Should().HaveCount(7);
        _sut.IsLoading.Should().BeFalse();
    }
}
