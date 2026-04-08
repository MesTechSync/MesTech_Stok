using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MesTech.Application.Features.Cargo.Queries.GetCargoProviders;
using MesTech.Avalonia.Controls;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// ViewModel for Cargo Providers screen — I-05 Siparis/Kargo Celiklestirme.
/// Displays all cargo provider cards with connection status and stats.
/// Wired to GetCargoProvidersQuery via MediatR (mock data eliminated).
/// </summary>
public partial class CargoProvidersAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public CargoProvidersAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [ObservableProperty] private int totalProviders;
    [ObservableProperty] private int connectedProviders;

    public ObservableCollection<CargoProviderCardViewModel> Providers { get; } = [];

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var dtos = await _mediator.Send(new GetCargoProvidersQuery(_currentUser.TenantId), ct);

            Providers.Clear();
            foreach (var dto in dtos)
            {
                var provider = Enum.TryParse<CargoProvider>(dto.Code, ignoreCase: true, out var p)
                    ? p
                    : CargoProvider.YurticiKargo;

                Providers.Add(new CargoProviderCardViewModel
                {
                    Provider = provider,
                    IsConnected = dto.IsActive,
                    LastShipmentText = dto.ContractInfo ?? "—",
                    TodayStats = dto.IsActive ? "Aktif" : "Pasif",
                    AvgDeliveryDays = dto.AvgDeliveryDays
                });
            }

            TotalProviders = Providers.Count;
            ConnectedProviders = Providers.Count(p => p.IsConnected);
            IsEmpty = Providers.Count == 0;
        }, "Kargo firmalari yuklenirken hata");
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();
}
