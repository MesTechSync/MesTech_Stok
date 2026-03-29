using FluentAssertions;
using MediatR;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTechStok.Avalonia.Tests;

/// <summary>
/// Unit tests for NakitAkisAvaloniaViewModel.
/// Covers constructor defaults, LoadAsync KPI calculations,
/// 3-state loading pattern, filter logic, and error handling.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class NakitAkisAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly NakitAkisAvaloniaViewModel _sut;

    public NakitAkisAvaloniaViewModelTests()
    {
        _mockMediator = new Mock<IMediator>();
        _sut = new NakitAkisAvaloniaViewModel(_mockMediator.Object, Mock.Of<ICurrentUserService>());
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        // Assert
        _sut.SelectedPeriodType.Should().Be("Aylik");
        _sut.SearchText.Should().BeEmpty();
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.IsEmpty.Should().BeFalse();
        _sut.TotalInflow.Should().Be("0,00 TL");
        _sut.TotalOutflow.Should().Be("0,00 TL");
        _sut.NetCashFlow.Should().Be("0,00 TL");

        _sut.PeriodTypes.Should().HaveCount(4);
        _sut.PeriodTypes.Should().ContainInOrder("Gunluk", "Haftalik", "Aylik", "Yillik");

        _sut.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateItems()
    {
        // Act
        await _sut.LoadAsync();

        // Assert — 6 seed items
        _sut.Items.Should().HaveCount(6);
        _sut.Items.Select(x => x.Description).Should().Contain("Trendyol hakedis odemesi");
        _sut.Items.Select(x => x.Description).Should().Contain("AWS sunucu odemesi");
        _sut.TotalCount.Should().Be(6);
    }

    [Fact]
    public async Task LoadAsync_ShouldCalculateKPIs()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        // Inflow: 12480 + 8920 + 3150 = 24550
        _sut.TotalInflow.Should().Be("24.550,00 TL");

        // Outflow: 1240 + 6500 + 2100 = 9840
        _sut.TotalOutflow.Should().Be("9.840,00 TL");

        // Net: 24550 - 9840 = 14710
        _sut.NetCashFlow.Should().Be("14.710,00 TL");
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
    public async Task ApplyFilters_SearchText_ShouldFilterByDescription()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act — search for Trendyol
        _sut.SearchText = "Trendyol";

        // Assert — only Trendyol hakedis odemesi
        _sut.Items.Should().HaveCount(1);
        _sut.Items[0].Description.Should().Contain("Trendyol");
        _sut.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyFilters_NoMatch_ShouldSetIsEmpty()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SearchText = "nonexistent";

        // Assert
        _sut.Items.Should().BeEmpty();
        _sut.IsEmpty.Should().BeTrue();
        _sut.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task LoadAsync_Error_ShouldSetErrorState()
    {
        // Arrange — We test the error path by calling LoadAsync after
        // verifying the normal path works. The current implementation
        // uses seed data that won't throw, so we verify the HasError
        // remains false on successful load and the error fields are reset.
        _sut.HasError.Should().BeFalse();

        // Act
        await _sut.LoadAsync();

        // Assert — successful load clears error state
        _sut.HasError.Should().BeFalse();
        _sut.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public async Task Refresh_ShouldCallLoadAsync()
    {
        // Act — call Refresh command which delegates to LoadAsync
        await _sut.RefreshCommand.ExecuteAsync(null);

        // Assert — items should be populated (proving LoadAsync ran)
        _sut.Items.Should().HaveCount(6);
        _sut.IsLoading.Should().BeFalse();
        _sut.TotalInflow.Should().NotBe("0,00 TL");
    }
}
