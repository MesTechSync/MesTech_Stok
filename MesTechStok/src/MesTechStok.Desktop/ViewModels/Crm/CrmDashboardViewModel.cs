using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.DTOs.Crm;
using MesTech.Application.Features.Crm.Queries.GetCrmDashboard;
using MesTech.Domain.Interfaces;

namespace MesTechStok.Desktop.ViewModels.Crm;

public partial class CrmDashboardViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private int totalCustomers;
    [ObservableProperty] private int activeCustomers;
    [ObservableProperty] private int vipCustomers;
    [ObservableProperty] private int totalLeads;
    [ObservableProperty] private int openDeals;
    [ObservableProperty] private decimal pipelineValue;
    [ObservableProperty] private int unreadMessages;
    [ObservableProperty] private int totalMessages;
    [ObservableProperty] private int totalSuppliers;
    [ObservableProperty] private int stageCount;
    [ObservableProperty] private DateTime lastRefreshed;
    [ObservableProperty] private string? errorMessage;

    public ObservableCollection<StageSummaryVm> StageSummaries { get; } = [];
    public ObservableCollection<RecentActivityVm> RecentActivities { get; } = [];

    public CrmDashboardViewModel(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var result = await _mediator.Send(new GetCrmDashboardQuery(tenantId));

            TotalCustomers = result.TotalCustomers;
            ActiveCustomers = result.ActiveCustomers;
            VipCustomers = result.VipCustomers;
            TotalLeads = result.TotalLeads;
            OpenDeals = result.OpenDeals;
            PipelineValue = result.PipelineValue;
            UnreadMessages = result.UnreadMessages;
            TotalMessages = result.TotalMessages;
            TotalSuppliers = result.TotalSuppliers;

            StageSummaries.Clear();
            foreach (var stage in result.StageSummaries)
            {
                StageSummaries.Add(new StageSummaryVm
                {
                    StageName = stage.StageName,
                    StageColor = stage.StageColor ?? "#3B82F6",
                    DealCount = stage.DealCount,
                    TotalValue = stage.TotalValue
                });
            }
            StageCount = StageSummaries.Count;

            RecentActivities.Clear();
            foreach (var act in result.RecentActivities)
            {
                RecentActivities.Add(new RecentActivityVm
                {
                    Subject = act.Subject,
                    ContactName = act.ContactName ?? "—",
                    TypeIcon = MapTypeIcon(act.Type),
                    TimeAgo = FormatTimeAgo(act.OccurredAt)
                });
            }

            LastRefreshed = DateTime.Now;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CrmDashboardViewModel] LoadAsync error: {ex.Message}");
            ErrorMessage = ex.Message;
            LoadPlaceholderData();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    private void LoadPlaceholderData()
    {
        TotalCustomers = 0;
        ActiveCustomers = 0;
        VipCustomers = 0;
        TotalLeads = 0;
        OpenDeals = 0;
        PipelineValue = 0;
        UnreadMessages = 0;
        TotalMessages = 0;
        TotalSuppliers = 0;
        StageCount = 0;
        StageSummaries.Clear();
        RecentActivities.Clear();
        LastRefreshed = DateTime.Now;
    }

    private static string MapTypeIcon(string type) => type.ToLowerInvariant() switch
    {
        "deal" or "deal_created" => "F",
        "lead" or "lead_created" => "L",
        "message" or "message_received" => "M",
        "customer" or "customer_created" => "C",
        _ => "?"
    };

    private static string FormatTimeAgo(DateTime occurredAt)
    {
        var diff = DateTime.UtcNow - occurredAt;
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} dk";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} sa";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} gun";
        return occurredAt.ToString("dd.MM.yyyy");
    }
}

public class StageSummaryVm
{
    public string StageName { get; set; } = string.Empty;
    public string StageColor { get; set; } = "#3B82F6";
    public int DealCount { get; set; }
    public decimal TotalValue { get; set; }
}

public class RecentActivityVm
{
    public string Subject { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string TypeIcon { get; set; } = "?";
    public string TimeAgo { get; set; } = string.Empty;
}
