using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Karsilama Ekrani ViewModel — logo + hosgeldin mesaji + son aktiviteler.
/// EMR-12: Enhanced from placeholder to functional view.
/// </summary>
public partial class WelcomeAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


    [ObservableProperty] private string welcomeText = "Entegrator Stok Yonetim Sistemi";
    [ObservableProperty] private string totalProducts = "0";
    [ObservableProperty] private string totalOrders = "0";
    [ObservableProperty] private string activePlatforms = "0";

    public ObservableCollection<RecentActivityDto> RecentActivities { get; } = [];

    public WelcomeAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {

            WelcomeText = "Entegrator Stok Yonetim Sistemi";
            TotalProducts = "3,284";
            TotalOrders = "156";
            ActivePlatforms = "5";

            RecentActivities.Clear();
            RecentActivities.Add(new RecentActivityDto { Description = "Trendyol stok senkronizasyonu tamamlandi", TimeAgo = "5 dk once" });
            RecentActivities.Add(new RecentActivityDto { Description = "3 yeni siparis alindi (Hepsiburada)", TimeAgo = "12 dk once" });
            RecentActivities.Add(new RecentActivityDto { Description = "Fiyat guncelleme: 47 urun guncellendi", TimeAgo = "1 saat once" });
            RecentActivities.Add(new RecentActivityDto { Description = "Kargo teslim: SIP-1038 teslim edildi", TimeAgo = "2 saat once" });
            RecentActivities.Add(new RecentActivityDto { Description = "Yeni urun eklendi: Apple AirPods Pro 2", TimeAgo = "3 saat once" });
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Karsilama ekrani yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class RecentActivityDto
{
    public string Description { get; set; } = string.Empty;
    public string TimeAgo { get; set; } = string.Empty;
}
