using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MesTech.Application.DTOs.Crm;
using Microsoft.Extensions.Logging;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// G105: Kampanya yönetim ekranı — aktif kampanyalar tablosu + oluşturma.
/// GetCampaignsQuery DEV1'e bırakıldı — mock fallback ile UI hazır.
/// </summary>
public partial class CampaignAvaloniaViewModel : ViewModelBase
{
    private readonly ILogger<CampaignAvaloniaViewModel> _logger;

    [ObservableProperty] private int activeCampaignCount;
    [ObservableProperty] private int totalProductsInCampaign;

    // Form fields
    [ObservableProperty] private string newCampaignName = string.Empty;
    [ObservableProperty] private DateTimeOffset startDate = DateTimeOffset.Now;
    [ObservableProperty] private DateTimeOffset endDate = DateTimeOffset.Now.AddDays(7);
    [ObservableProperty] private decimal discountPercent = 10;
    [ObservableProperty] private string selectedPlatform = "Tumu";

    public ObservableCollection<CampaignDto> Campaigns { get; } = [];
    public ObservableCollection<string> Platforms { get; } = ["Tumu", "Trendyol", "Hepsiburada", "N11", "Ciceksepeti", "Amazon", "Pazarama"];

    public CampaignAvaloniaViewModel(ILogger<CampaignAvaloniaViewModel> logger)
    {
        _logger = logger;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        try
        {
            // TODO: await _mediator.Send(new GetCampaignsQuery(Guid.Empty)) — DEV1 handler bekleniyor
            await Task.Delay(100);
            Campaigns.Clear();
            Campaigns.Add(new CampaignDto { Id = Guid.NewGuid(), Name = "Yaz Indirimi 2026", StartDate = DateTime.Now.AddDays(-5), EndDate = DateTime.Now.AddDays(25), DiscountPercent = 15, IsActive = true, ProductCount = 42 });
            Campaigns.Add(new CampaignDto { Id = Guid.NewGuid(), Name = "Stok Eritme", StartDate = DateTime.Now.AddDays(-10), EndDate = DateTime.Now.AddDays(3), DiscountPercent = 30, IsActive = true, ProductCount = 18 });
            Campaigns.Add(new CampaignDto { Id = Guid.NewGuid(), Name = "Yeni Sezon Tanitimi", StartDate = DateTime.Now.AddDays(5), EndDate = DateTime.Now.AddDays(20), DiscountPercent = 10, IsActive = false, ProductCount = 65 });

            ActiveCampaignCount = Campaigns.Count(c => c.IsActive);
            TotalProductsInCampaign = Campaigns.Sum(c => c.ProductCount);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Kampanyalar yuklenemedi: {ex.Message}";
            _logger.LogError(ex, "Campaign load failed");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CreateCampaignAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCampaignName))
        {
            HasError = true;
            ErrorMessage = "Kampanya adi bos olamaz.";
            return;
        }

        // TODO: await _mediator.Send(new CreateCampaignCommand(...)) — DEV1
        Campaigns.Add(new CampaignDto
        {
            Id = Guid.NewGuid(),
            Name = NewCampaignName,
            StartDate = StartDate.DateTime,
            EndDate = EndDate.DateTime,
            DiscountPercent = DiscountPercent,
            IsActive = true,
            ProductCount = 0
        });

        ActiveCampaignCount = Campaigns.Count(c => c.IsActive);
        NewCampaignName = string.Empty;
        _logger.LogInformation("Campaign created: {Name}", NewCampaignName);
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}
