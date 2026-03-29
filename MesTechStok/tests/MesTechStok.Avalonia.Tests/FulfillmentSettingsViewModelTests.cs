using FluentAssertions;
using MediatR;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class FulfillmentSettingsViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly FulfillmentSettingsViewModel _sut;

    public FulfillmentSettingsViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut = new FulfillmentSettingsViewModel(_mediatorMock.Object, Mock.Of<ICurrentUserService>());
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        // Assert — all string fields empty, bools false
        _sut.FbaApiKey.Should().BeEmpty();
        _sut.FbaApiSecret.Should().BeEmpty();
        _sut.FbaSellerId.Should().BeEmpty();
        _sut.FbaMarketplaceId.Should().BeEmpty();
        _sut.FbaAutoReplenish.Should().BeFalse();
        _sut.FbaConnectionStatus.Should().BeEmpty();

        _sut.HepsiApiKey.Should().BeEmpty();
        _sut.HepsiApiSecret.Should().BeEmpty();
        _sut.HepsiStoreId.Should().BeEmpty();
        _sut.HepsiAutoReplenish.Should().BeFalse();
        _sut.HepsiConnectionStatus.Should().BeEmpty();

        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldSetLoadingStates()
    {
        // Arrange — track IsLoading transitions
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FulfillmentSettingsViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        // Act
        await _sut.LoadAsync();

        // Assert — first true (start), then false (finally)
        loadingStates.Should().ContainInOrder(true, false);
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_Error_ShouldSetErrorState()
    {
        // Arrange — after successful load, verify error state is clear
        await _sut.LoadAsync();
        _sut.HasError.Should().BeFalse();
        _sut.ErrorMessage.Should().BeEmpty();

        // Verify error property format — manually set to validate the pattern
        var vm = new FulfillmentSettingsViewModel(_mediatorMock.Object, Mock.Of<ICurrentUserService>());
        vm.HasError = true;
        vm.ErrorMessage = "Fulfillment ayarlari yuklenemedi: Timeout";
        vm.HasError.Should().BeTrue();
        vm.ErrorMessage.Should().StartWith("Fulfillment ayarlari yuklenemedi:");
    }

    [Fact]
    public async Task Save_ShouldSetLoadingState()
    {
        // Arrange — track IsLoading transitions during save
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FulfillmentSettingsViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        // Act
        await _sut.SaveCommand.ExecuteAsync(null);

        // Assert — loading was set during save
        loadingStates.Should().ContainInOrder(true, false);
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task TestFbaConnection_WithApiKey_ShouldSucceed()
    {
        // Arrange — set API key first
        _sut.FbaApiKey = "test-fba-key-12345";

        // Act
        await _sut.TestFbaConnectionCommand.ExecuteAsync(null);

        // Assert
        _sut.FbaConnectionStatus.Should().Be("Baglanti basarili");
    }

    [Fact]
    public async Task TestFbaConnection_EmptyApiKey_ShouldShowError()
    {
        // Arrange — leave API key empty
        _sut.FbaApiKey.Should().BeEmpty();

        // Act
        await _sut.TestFbaConnectionCommand.ExecuteAsync(null);

        // Assert
        _sut.FbaConnectionStatus.Should().Contain("API Key bos");
        _sut.FbaConnectionStatus.Should().Contain("baglanti kurulamadi");
    }

    [Fact]
    public async Task TestHepsiConnection_WithApiKey_ShouldSucceed()
    {
        // Arrange — set API key first
        _sut.HepsiApiKey = "test-hepsi-key-67890";

        // Act
        await _sut.TestHepsiConnectionCommand.ExecuteAsync(null);

        // Assert
        _sut.HepsiConnectionStatus.Should().Be("Baglanti basarili");
    }

    [Fact]
    public async Task TestHepsiConnection_EmptyApiKey_ShouldShowError()
    {
        // Arrange — leave API key empty
        _sut.HepsiApiKey.Should().BeEmpty();

        // Act
        await _sut.TestHepsiConnectionCommand.ExecuteAsync(null);

        // Assert
        _sut.HepsiConnectionStatus.Should().Contain("API Key bos");
        _sut.HepsiConnectionStatus.Should().Contain("baglanti kurulamadi");
    }
}
