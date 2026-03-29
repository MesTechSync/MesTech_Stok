using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetFixedExpenses;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

/// <summary>
/// Unit tests for SabitGiderlerAvaloniaViewModel.
/// Covers constructor defaults, LoadAsync KPI calculations,
/// 3-state loading pattern, and filter logic.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class SabitGiderlerAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly SabitGiderlerAvaloniaViewModel _sut;

    public SabitGiderlerAvaloniaViewModelTests()
    {
        _mockMediator = new Mock<IMediator>();
        _sut = new SabitGiderlerAvaloniaViewModel(_mockMediator.Object, Mock.Of<ICurrentUserService>());
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        // Assert — Categories and Periods collections populated
        _sut.Categories.Should().NotBeEmpty();
        _sut.Categories.Should().HaveCount(7);
        _sut.Categories.Should().Contain("Tumu");
        _sut.Categories.Should().Contain("Kira");
        _sut.Categories.Should().Contain("Teknoloji");

        _sut.Periods.Should().NotBeEmpty();
        _sut.Periods.Should().HaveCount(4);
        _sut.Periods.Should().Contain("Tumu");
        _sut.Periods.Should().Contain("Aylik");
        _sut.Periods.Should().Contain("Yillik");

        _sut.SelectedCategory.Should().Be("Tumu");
        _sut.SelectedPeriod.Should().Be("Tumu");
        _sut.SearchText.Should().BeEmpty();
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateItems()
    {
        // Act
        await _sut.LoadAsync();

        // Assert — 6 seed items loaded
        _sut.Items.Should().HaveCount(6);
        _sut.Items.Select(x => x.Name).Should().Contain("Ofis Kirasi");
        _sut.Items.Select(x => x.Name).Should().Contain("AWS Sunucu");
        _sut.Items.Select(x => x.Name).Should().Contain("Domain + SSL");
    }

    [Fact]
    public async Task LoadAsync_ShouldCalculateKPIs()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        // Monthly items: 15000 + 2100 + 5000 + 1800 = 23900
        _sut.MonthlyTotal.Should().Be("23.900,00 TL");

        // Yearly: 23900 * 12 + (4500 + 750) = 286800 + 5250 = 292050
        _sut.YearlyTotal.Should().Be("292.050,00 TL");

        _sut.ActiveCount.Should().Be(6);
    }

    [Fact]
    public async Task LoadAsync_ShouldSetLoadingStates()
    {
        // Arrange — track loading state transitions
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(_sut.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        // Act
        await _sut.LoadAsync();

        // Assert — IsLoading should go true then false
        loadingStates.Should().ContainInOrder(true, false);
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyFilters_CategoryFilter_ShouldFilterByCategory()
    {
        // Arrange — load data first
        await _sut.LoadAsync();

        // Act — select Kira category
        _sut.SelectedCategory = "Kira";

        // Assert — only rent items
        _sut.Items.Should().HaveCount(1);
        _sut.Items[0].Name.Should().Be("Ofis Kirasi");
        _sut.Items[0].Category.Should().Be("Kira");
    }

    [Fact]
    public async Task ApplyFilters_PeriodFilter_ShouldFilterByPeriod()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act — select Yillik period
        _sut.SelectedPeriod = "Yillik";

        // Assert — only yearly items: Isyeri Sigortasi, Domain + SSL
        _sut.Items.Should().HaveCount(2);
        _sut.Items.Should().OnlyContain(x => x.Period == "Yillik");
    }

    [Fact]
    public async Task ApplyFilters_SearchText_ShouldFilterByName()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act — search for "AWS"
        _sut.SearchText = "AWS";

        // Assert — only AWS Sunucu item
        _sut.Items.Should().HaveCount(1);
        _sut.Items[0].Name.Should().Be("AWS Sunucu");
    }

    [Fact]
    public async Task ApplyFilters_NoMatch_ShouldSetIsEmpty()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act — search for non-existent item
        _sut.SearchText = "nonexistent";

        // Assert
        _sut.Items.Should().BeEmpty();
        _sut.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_WhenMediatorThrows_SetsHasError()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetFixedExpensesQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB down"));

        var sut = new SabitGiderlerAvaloniaViewModel(mediator.Object, Mock.Of<ICurrentUserService>());
        await sut.LoadAsync();

        sut.HasError.Should().BeTrue();
        sut.ErrorMessage.Should().NotBeNullOrEmpty();
        sut.IsLoading.Should().BeFalse(); // KÇ-13
    }

    [Fact]
    public async Task LoadAsync_WhenEmptyList_SetsIsEmpty()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetFixedExpensesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Application.DTOs.Accounting.FixedExpenseDto>)new List<Application.DTOs.Accounting.FixedExpenseDto>());

        var sut = new SabitGiderlerAvaloniaViewModel(mediator.Object, Mock.Of<ICurrentUserService>());
        await sut.LoadAsync();

        sut.IsEmpty.Should().BeTrue();
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }
}
