using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.DTOs.Crm;
using MesTech.Application.Features.Crm.Commands.CreateCampaign;
using MesTech.Application.Features.Crm.Queries.GetActiveCampaigns;
using MesTech.Application.Features.Reporting.Commands.ExportReport;
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
    private List<CampaignDto> _allCampaigns = [];

    // Sort
    [ObservableProperty] private string sortColumn = "default";
    [ObservableProperty] private bool sortAscending = false;

    public ObservableCollection<string> Platforms { get; } = ["Tumu", "Trendyol", "Hepsiburada", "N11", "Ciceksepeti", "Amazon", "Pazarama"];

    public CampaignAvaloniaViewModel(IMediator mediator, ITenantProvider tenantProvider, ILogger<CampaignAvaloniaViewModel> logger)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var result = await _mediator.Send(new GetActiveCampaignsQuery(tenantId), ct);

            _allCampaigns = result.Items.ToList();
            ApplySort();

            ActiveCampaignCount = _allCampaigns.Count(c => c.IsActive);
            TotalProductsInCampaign = _allCampaigns.Sum(c => c.ProductCount);
        }, "Kampanyalar yuklenirken hata");
    }

    private void ApplySort()
    {
        var sorted = SortColumn switch
        {
            "Name"            => SortAscending ? _allCampaigns.OrderBy(x => x.Name).ToList()            : _allCampaigns.OrderByDescending(x => x.Name).ToList(),
            "DiscountPercent" => SortAscending ? _allCampaigns.OrderBy(x => x.DiscountPercent).ToList() : _allCampaigns.OrderByDescending(x => x.DiscountPercent).ToList(),
            "StartDate"       => SortAscending ? _allCampaigns.OrderBy(x => x.StartDate).ToList()       : _allCampaigns.OrderByDescending(x => x.StartDate).ToList(),
            "EndDate"         => SortAscending ? _allCampaigns.OrderBy(x => x.EndDate).ToList()         : _allCampaigns.OrderByDescending(x => x.EndDate).ToList(),
            _                 => SortAscending ? _allCampaigns.OrderBy(x => x.StartDate).ToList()       : _allCampaigns.OrderByDescending(x => x.StartDate).ToList(),
        };
        Campaigns.Clear();
        foreach (var c in sorted) Campaigns.Add(c);
        IsEmpty = Campaigns.Count == 0;
    }

    [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column) SortAscending = !SortAscending;
        else { SortColumn = column; SortAscending = true; }
        ApplySort();
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new ExportReportCommand(_tenantProvider.GetCurrentTenantId(), "campaigns", "xlsx"), ct);
            if (result.FileData.Length > 0)
            {
                var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MesTech_Exports");
                System.IO.Directory.CreateDirectory(dir);
                await System.IO.File.WriteAllBytesAsync(System.IO.Path.Combine(dir, result.FileName), result.FileData);
            }
        }, "Kampanyalar disa aktarilirken hata");
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
