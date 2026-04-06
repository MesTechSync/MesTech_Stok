using FluentAssertions;
using MesTech.Application.DTOs.Platform;
using MesTech.Application.Features.Platform.Queries.GetPlatformSyncStatus;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class SyncStatusAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly SyncStatusAvaloniaViewModel _sut;

    public SyncStatusAvaloniaViewModelTests()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPlatformSyncStatusQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlatformSyncStatusDto>());
        _sut = new SyncStatusAvaloniaViewModel(_mediatorMock.Object, Mock.Of<ICurrentUserService>());
    }

    [Fact]
    public async Task LoadAsync_WithEmptyData_ShouldCompleteWithoutError()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.Items.Should().BeEmpty();
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
            if (e.PropertyName == nameof(SyncStatusAvaloniaViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        // Act
        await _sut.LoadAsync();

        // Assert
        loadingStates.Should().Contain(true);
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void DurumRenk_ShouldReturnCorrectColorCodes()
    {
        // Arrange
        var basarili = new SyncStatusItemDto { Durum = "Basarili" };
        var hatali = new SyncStatusItemDto { Durum = "Hatali" };
        var bekliyor = new SyncStatusItemDto { Durum = "Bekliyor" };
        var bilinmeyen = new SyncStatusItemDto { Durum = "Bilinmeyen" };

        // Assert
        basarili.DurumRenk.Should().Be("#16A34A");
        hatali.DurumRenk.Should().Be("#DC2626");
        bekliyor.DurumRenk.Should().Be("#D97706");
        bilinmeyen.DurumRenk.Should().Be("#64748B");
    }
}
