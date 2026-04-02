using FluentAssertions;
using MediatR;
using MesTech.Avalonia.Services;
using MesTech.Avalonia.ViewModels;
using GetCargoTrackingListQuery = MesTech.Application.Features.Cargo.Queries.GetCargoTrackingList.GetCargoTrackingListQuery;
using AppCargoTrackingItemDto = MesTech.Application.Features.Cargo.Queries.GetCargoTrackingList.CargoTrackingItemDto;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class CargoAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IDialogService> _dialogMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly CargoAvaloniaViewModel _sut;

    public CargoAvaloniaViewModelTests()
    {
        _currentUserMock.Setup(c => c.TenantId).Returns(Guid.NewGuid());
        SetupMediatorWithCargos();
        _sut = new CargoAvaloniaViewModel(_dialogMock.Object, _mediatorMock.Object, _currentUserMock.Object);
    }

    private void SetupMediatorWithCargos(IReadOnlyList<AppCargoTrackingItemDto>? items = null)
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCargoTrackingListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(items ?? CreateTestCargos());
    }

    private static IReadOnlyList<AppCargoTrackingItemDto> CreateTestCargos() =>
    [
        new() { OrderNumber = "SIP-001", TrackingNumber = "YK1001", CargoProvider = "Yurtici Kargo", Status = "Dagitimda", ShippedAt = DateTime.Now.AddDays(-2) },
        new() { OrderNumber = "SIP-002", TrackingNumber = "AK2001", CargoProvider = "Aras Kargo", Status = "Teslim Edildi", ShippedAt = DateTime.Now.AddDays(-5) },
        new() { OrderNumber = "SIP-003", TrackingNumber = "SK3001", CargoProvider = "Surat Kargo", Status = "Hazirlaniyor", ShippedAt = DateTime.Now.AddDays(-1) },
        new() { OrderNumber = "SIP-004", TrackingNumber = "YK1002", CargoProvider = "Yurtici Kargo", Status = "Dagitimda", ShippedAt = DateTime.Now.AddDays(-3) },
        new() { OrderNumber = "Ahmet-SIP-005", TrackingNumber = "MK4001", CargoProvider = "MNG Kargo", Status = "Teslim Edildi", ShippedAt = DateTime.Now.AddDays(-7) },
        new() { OrderNumber = "SIP-006", TrackingNumber = "PK5001", CargoProvider = "PTT Kargo", Status = "Dagitimda", ShippedAt = DateTime.Now },
        new() { OrderNumber = "SIP-007", TrackingNumber = "AK2002", CargoProvider = "Aras Kargo", Status = "Hazirlaniyor", ShippedAt = DateTime.Now.AddDays(-1) },
        new() { OrderNumber = "SIP-008", TrackingNumber = "YK1003", CargoProvider = "Yurtici Kargo", Status = "Teslim Edildi", ShippedAt = DateTime.Now.AddDays(-10) },
        new() { OrderNumber = "SIP-009", TrackingNumber = "SK3002", CargoProvider = "Surat Kargo", Status = "Dagitimda", ShippedAt = DateTime.Now.AddDays(-4) },
        new() { OrderNumber = "SIP-010", TrackingNumber = "MK4002", CargoProvider = "MNG Kargo", Status = "Hazirlaniyor", ShippedAt = DateTime.Now },
    ];

    [Fact]
    public async Task LoadAsync_ShouldPopulateCargosAndCount()
    {
        await _sut.LoadAsync();

        _sut.Cargos.Should().HaveCount(10);
        _sut.TotalCount.Should().Be(10);
        _sut.IsEmpty.Should().BeFalse();
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldTransitionLoadingState()
    {
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CargoAvaloniaViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        await _sut.LoadAsync();

        loadingStates.Should().Contain(true, "IsLoading should be true during load");
        _sut.IsLoading.Should().BeFalse("IsLoading should be false after load completes");
    }

    [Fact]
    public async Task SelectedCompany_ShouldFilterCargos()
    {
        await _sut.LoadAsync();

        _sut.SelectedCompany = "Yurtici Kargo";

        _sut.Cargos.Should().OnlyContain(c => c.Company == "Yurtici Kargo");
        _sut.TotalCount.Should().BeLessThan(10);
        _sut.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SearchText_ShouldFilterByTrackingNoOrReceiver()
    {
        await _sut.LoadAsync();

        _sut.SearchText = "Ahmet";

        _sut.Cargos.Should().HaveCountGreaterThan(0);
        _sut.Cargos.Should().OnlyContain(c =>
            c.TrackingNo.Contains("Ahmet", StringComparison.OrdinalIgnoreCase) ||
            c.Receiver.Contains("Ahmet", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CompanyFilter_ThenResetToTumu_ShouldShowAll()
    {
        await _sut.LoadAsync();

        _sut.SelectedCompany = "Aras Kargo";
        var filteredCount = _sut.TotalCount;
        _sut.SelectedCompany = "Tumu";

        filteredCount.Should().BeLessThan(10);
        _sut.TotalCount.Should().Be(10);
        _sut.Cargos.Should().HaveCount(10);
    }
}
