using FluentAssertions;
using MesTech.Avalonia.ViewModels;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class ErpSettingsAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly ErpSettingsAvaloniaViewModel _sut;

    public ErpSettingsAvaloniaViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut = new ErpSettingsAvaloniaViewModel(_mediatorMock.Object);
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        // Assert
        _sut.SelectedErpProvider.Should().Be("Yok");
        _sut.IsProviderSelected.Should().BeFalse();
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.IsConnected.Should().BeFalse();
        _sut.LastTestResult.Should().Be("Test edilmedi");
        _sut.ConnectionStatusColor.Should().Be("#94A3B8");
        _sut.AutoSyncStock.Should().BeTrue();
        _sut.AutoSyncInvoice.Should().BeTrue();
        _sut.StockSyncPeriodMinutes.Should().Be(30);
        _sut.PriceSyncPeriodMinutes.Should().Be(60);
        _sut.ErpProviders.Should().HaveCount(6);
    }

    [Fact]
    public async Task LoadAsync_ShouldSetStates()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.SyncHistory.Should().HaveCount(4);
        _sut.SyncHistory[0].SyncType.Should().Be("Stok");
        _sut.SyncHistory[0].RecordCount.Should().Be(245);
        _sut.SyncHistory[1].SyncType.Should().Be("Fatura");
        _sut.SyncHistory[2].Status.Should().Contain("Hata");
    }

    [Fact]
    public void SelectedErpProvider_Logo_ShowsLogoFields()
    {
        // Act
        _sut.SelectedErpProvider = "Logo";

        // Assert
        _sut.IsLogoSelected.Should().BeTrue();
        _sut.IsProviderSelected.Should().BeTrue();
        _sut.IsParasutSelected.Should().BeFalse();
        _sut.IsNetsisSelected.Should().BeFalse();
        _sut.IsNebimSelected.Should().BeFalse();
    }

    [Fact]
    public void SelectedErpProvider_Parasut_ShowsParasutFields()
    {
        // Act
        _sut.SelectedErpProvider = "Parasut";

        // Assert
        _sut.IsParasutSelected.Should().BeTrue();
        _sut.IsProviderSelected.Should().BeTrue();
        _sut.IsLogoSelected.Should().BeFalse();
        _sut.ParasutSandbox.Should().BeTrue();
    }

    [Fact]
    public void SelectedErpProvider_Yok_HidesAllFields()
    {
        // Arrange — first select a provider
        _sut.SelectedErpProvider = "Logo";
        _sut.IsProviderSelected.Should().BeTrue();

        // Act
        _sut.SelectedErpProvider = "Yok";

        // Assert
        _sut.IsProviderSelected.Should().BeFalse();
        _sut.IsLogoSelected.Should().BeFalse();
        _sut.IsParasutSelected.Should().BeFalse();
        _sut.IsNetsisSelected.Should().BeFalse();
        _sut.IsNebimSelected.Should().BeFalse();
    }

    [Fact]
    public async Task TestConnection_Success_SetsConnected()
    {
        // Act
        await _sut.TestConnectionCommand.ExecuteAsync(null);

        // Assert
        _sut.IsConnected.Should().BeTrue();
        _sut.LastTestResult.Should().Be("Baglanti basarili");
        _sut.ConnectionStatusColor.Should().Be("#22C55E");
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task SaveSettings_SetsLoadingState()
    {
        // Arrange — track loading state changes
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ErpSettingsAvaloniaViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        // Act
        await _sut.SaveSettingsCommand.ExecuteAsync(null);

        // Assert
        loadingStates.Should().Contain(true, "IsLoading should have been true during save");
        _sut.IsLoading.Should().BeFalse("IsLoading should be false after save completes");
    }

    [Theory]
    [InlineData("Logo", true, false, false, false)]
    [InlineData("Netsis", false, true, false, false)]
    [InlineData("Nebim", false, false, true, false)]
    [InlineData("Parasut", false, false, false, true)]
    public void SelectedErpProvider_SetsCorrectVisibility(
        string provider, bool logo, bool netsis, bool nebim, bool parasut)
    {
        // Act
        _sut.SelectedErpProvider = provider;

        // Assert
        _sut.IsLogoSelected.Should().Be(logo);
        _sut.IsNetsisSelected.Should().Be(netsis);
        _sut.IsNebimSelected.Should().Be(nebim);
        _sut.IsParasutSelected.Should().Be(parasut);
        _sut.IsProviderSelected.Should().BeTrue();
    }
}
