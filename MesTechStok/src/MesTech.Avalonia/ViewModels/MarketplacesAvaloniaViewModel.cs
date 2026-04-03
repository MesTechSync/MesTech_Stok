using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Platform.Queries.GetPlatformSyncStatus;
using MesTech.Application.DTOs.Platform;
using MesTech.Avalonia.Services;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Marketplaces ViewModel — wired to GetPlatformSyncStatusQuery via MediatR.
/// Displays platform adapters with sync status and configuration.
/// </summary>
public partial class MarketplacesAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly IDialogService _dialog;

    [ObservableProperty] private string summary = "Pazaryeri yonetimi ekrani — Dalga 11 sonrasi aktif edilecek.";
    [ObservableProperty] private int platformCount = 10;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private System.Collections.ObjectModel.ObservableCollection<string> platforms = new();
    [ObservableProperty] private string? selectedPlatform;

    public MarketplacesAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser, IDialogService dialog)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _dialog = dialog;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var statuses = await _mediator.Send(new GetPlatformSyncStatusQuery(TenantId: _currentUser.TenantId));

            Platforms.Clear();
            foreach (var s in statuses)
                Platforms.Add(s.PlatformName);

            PlatformCount = statuses.Count;
            TotalCount = statuses.Count;
            Summary = $"Pazaryeri yonetimi ekrani hazir. {statuses.Count} platform entegrasyonu aktif.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Platform verileri yuklenemedi: {ex.Message}";
            PlatformCount = 10;
            Summary = "Pazaryeri yonetimi ekrani hazir. 10 platform entegrasyonu, API ayarlari, senkronizasyon durumu ve hata loglari burada yer alacak.";
        }
        finally
        {
            IsLoading = false;
            IsEmpty = Platforms.Count == 0;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task Add()
    {
        Summary = "Platform ekleme sihirbazi hazirlaniyor — Magaza Ayarlari sayfasindan yeni platform ekleyebilirsiniz.";
        await Task.CompletedTask;
    }
}
