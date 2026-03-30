using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.DTOs.Crm;
using MesTech.Application.Features.Crm.Commands.CreateCampaign;
using MesTech.Application.Features.Crm.Queries.GetActiveCampaigns;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// G105: Kampanya yönetim ekranı — aktif kampanyalar tablosu + oluşturma.
/// MediatR wired: GetActiveCampaignsQuery + CreateCampaignCommand.
/// </summary>
public partial class CampaignAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;
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

    public CampaignAvaloniaViewModel(IMediator mediator, ITenantProvider tenantProvider, ILogger<CampaignAvaloniaViewModel> logger)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        try
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var result = await _mediator.Send(new GetActiveCampaignsQuery(tenantId), CancellationToken);

            Campaigns.Clear();
            foreach (var c in result.Items)
                Campaigns.Add(c);

            ActiveCampaignCount = Campaigns.Count(c => c.IsActive);
            TotalProductsInCampaign = Campaigns.Sum(c => c.ProductCount);
            IsEmpty = Campaigns.Count == 0;
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

        var tenantId = _tenantProvider.GetCurrentTenantId();
        var campaignId = await _mediator.Send(new CreateCampaignCommand(
            TenantId: tenantId,
            Name: NewCampaignName,
            StartDate: StartDate.DateTime,
            EndDate: EndDate.DateTime,
            DiscountPercent: DiscountPercent
        ), CancellationToken);

        Campaigns.Add(new CampaignDto
        {
            Id = campaignId,
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
