using FluentAssertions;
using MesTech.Application.Features.Settings.Queries.GetStoreSettings;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class StoreManagementAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly StoreManagementAvaloniaViewModel _sut;

    public StoreManagementAvaloniaViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetStoreSettingsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoreSettingsDto());
        var tenantProviderMock = new Mock<ITenantProvider>();
        _sut = new StoreManagementAvaloniaViewModel(_mediatorMock.Object, tenantProviderMock.Object);
    }

    [Fact]
    public async Task LoadAsync_WithEmptyData_ShouldCompleteWithoutError()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.Stores.Should().BeEmpty();
        _sut.TotalCount.Should().Be(0);
        _sut.IsEmpty.Should().BeTrue();
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
            if (e.PropertyName == nameof(StoreManagementAvaloniaViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        // Act
        await _sut.LoadAsync();

        // Assert
        loadingStates.Should().Contain(true);
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ShouldInitializeDefaultState()
    {
        // Assert
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.IsEmpty.Should().BeFalse();
        _sut.Stores.Should().BeEmpty();
        _sut.TotalCount.Should().Be(0);
    }
}
