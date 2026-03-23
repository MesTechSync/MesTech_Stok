using FluentAssertions;
using MesTech.Avalonia.ViewModels;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class CargoAvaloniaViewModelTests
{
    private readonly CargoAvaloniaViewModel _sut;

    public CargoAvaloniaViewModelTests()
    {
        _sut = new CargoAvaloniaViewModel();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateCargosAndCount()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.Cargos.Should().HaveCount(10);
        _sut.TotalCount.Should().Be(10);
        _sut.IsEmpty.Should().BeFalse();
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldTransitionLoadingState()
    {
        // Arrange
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CargoAvaloniaViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        // Act
        await _sut.LoadAsync();

        // Assert
        loadingStates.Should().Contain(true, "IsLoading should be true during load");
        _sut.IsLoading.Should().BeFalse("IsLoading should be false after load completes");
    }

    [Fact]
    public async Task SelectedCompany_ShouldFilterCargos()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SelectedCompany = "Yurtici Kargo";

        // Assert
        _sut.Cargos.Should().OnlyContain(c => c.Company == "Yurtici Kargo");
        _sut.TotalCount.Should().BeLessThan(10);
        _sut.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SearchText_ShouldFilterByTrackingNoOrReceiver()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act — search by receiver name
        _sut.SearchText = "Ahmet";

        // Assert
        _sut.Cargos.Should().HaveCountGreaterThan(0);
        _sut.Cargos.Should().OnlyContain(c =>
            c.TrackingNo.Contains("Ahmet", StringComparison.OrdinalIgnoreCase) ||
            c.Receiver.Contains("Ahmet", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CompanyFilter_ThenResetToTumu_ShouldShowAll()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act — filter then reset
        _sut.SelectedCompany = "Aras Kargo";
        var filteredCount = _sut.TotalCount;
        _sut.SelectedCompany = "Tumu";

        // Assert
        filteredCount.Should().BeLessThan(10);
        _sut.TotalCount.Should().Be(10);
        _sut.Cargos.Should().HaveCount(10);
    }
}
