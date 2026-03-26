using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Cargo.Queries.GetCargoTrackingList;
using MesTech.Avalonia.Services;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Cargo tracking ViewModel — wired to GetCargoTrackingListQuery via MediatR.
/// G033: Task.Delay mock replaced with real mediator.Send call.
/// </summary>
public partial class CargoAvaloniaViewModel : ViewModelBase
{
    private readonly IDialogService _dialog;
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedCompany = "Tumu";
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private CargoItemDto? selectedCargo;
    [ObservableProperty] private bool isCreating;

    public CargoAvaloniaViewModel(IDialogService dialog, IMediator mediator, ICurrentUserService currentUser)
    {
        _dialog = dialog;
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public ObservableCollection<CargoItemDto> Cargos { get; } = [];

    public ObservableCollection<string> Companies { get; } =
    [
        "Tumu", "Yurtici Kargo", "Aras Kargo", "Surat Kargo", "MNG Kargo", "PTT Kargo"
    ];

    private List<CargoItemDto> _allCargos = [];

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            var results = await _mediator.Send(
                new GetCargoTrackingListQuery(_currentUser.TenantId, 100),
                CancellationToken);

            _allCargos = results.Select(r => new CargoItemDto
            {
                TrackingNo = r.TrackingNumber ?? r.OrderNumber,
                Company = r.CargoProvider ?? "—",
                Date = r.ShippedAt?.ToString("dd.MM.yyyy") ?? "—",
                Status = r.Status,
                Receiver = r.OrderNumber
            }).ToList();

            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Kargo verileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allCargos.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(c =>
                c.TrackingNo.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                c.Receiver.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedCompany != "Tumu")
        {
            filtered = filtered.Where(c => c.Company == SelectedCompany);
        }

        Cargos.Clear();
        foreach (var item in filtered)
            Cargos.Add(item);

        TotalCount = Cargos.Count;
        IsEmpty = Cargos.Count == 0;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private async Task CreateShipment()
    {
        await _dialog.ShowInfoAsync("Kargo olusturma ekrani hazirlaniyor...", "Kargo Olustur");
    }

    [RelayCommand]
    private async Task PrintLabel()
    {
        if (SelectedCargo == null)
        {
            await _dialog.ShowInfoAsync("Lutfen bir kargo secin.", "Etiket Yazdir");
            return;
        }
        await _dialog.ShowInfoAsync($"Etiket hazirlaniyor: {SelectedCargo.TrackingNo}", "Etiket Yazdir");
    }

    [RelayCommand]
    private async Task TrackShipment()
    {
        if (SelectedCargo == null)
        {
            await _dialog.ShowInfoAsync("Lutfen bir kargo secin.", "Kargo Takip");
            return;
        }
        await _dialog.ShowInfoAsync($"Kargo takip: {SelectedCargo.TrackingNo} — {SelectedCargo.Status}", "Kargo Takip");
    }

    partial void OnSearchTextChanged(string value)
    {
        if (_allCargos.Count > 0)
            ApplyFilters();
    }

    partial void OnSelectedCompanyChanged(string value)
    {
        if (_allCargos.Count > 0)
            ApplyFilters();
    }
}

public class CargoItemDto
{
    public string TrackingNo { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Receiver { get; set; } = string.Empty;
}
