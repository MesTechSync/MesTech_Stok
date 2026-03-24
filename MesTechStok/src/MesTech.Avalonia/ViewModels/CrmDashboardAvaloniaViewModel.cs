using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Crm.Queries.GetCrmDashboard;

namespace MesTech.Avalonia.ViewModels;

public partial class CrmDashboardAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


    // KPI metrics — mapped from CrmDashboardDto
    [ObservableProperty] private int totalCustomers;
    [ObservableProperty] private int newThisMonth;
    [ObservableProperty] private int vipCustomers;
    [ObservableProperty] private string pipelineValue = "0 TL";
    [ObservableProperty] private int openDeals;
    [ObservableProperty] private int unreadMessages;
    [ObservableProperty] private int pendingReplies;
    [ObservableProperty] private int totalLeads;
    [ObservableProperty] private string lastUpdated = "--:--";

    public ObservableCollection<CrmPipelineSummaryVm> PipelineSummary { get; } = [];
    public ObservableCollection<CrmRecentActivityVm> RecentActivities { get; } = [];

    public CrmDashboardAvaloniaViewModel(IMediator mediator)
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
            var dto = await _mediator.Send(new GetCrmDashboardQuery(Guid.Empty));

            TotalCustomers = dto.TotalCustomers;
            NewThisMonth = dto.ActiveCustomers;
            VipCustomers = dto.VipCustomers;
            PipelineValue = $"{dto.PipelineValue:#,0} TL";
            OpenDeals = dto.OpenDeals;
            UnreadMessages = dto.UnreadMessages;
            PendingReplies = dto.TotalMessages - dto.UnreadMessages;
            TotalLeads = dto.TotalLeads;
            LastUpdated = DateTime.Now.ToString("HH:mm:ss");

            PipelineSummary.Clear();
            foreach (var stage in dto.StageSummaries)
            {
                PipelineSummary.Add(new CrmPipelineSummaryVm
                {
                    Stage = stage.StageName,
                    Count = stage.DealCount,
                    Amount = $"{stage.TotalValue:#,0} TL",
                    Color = stage.StageColor ?? "#3B82F6"
                });
            }

            RecentActivities.Clear();
            foreach (var act in dto.RecentActivities)
            {
                RecentActivities.Add(new CrmRecentActivityVm
                {
                    Description = $"{act.ContactName}: {act.Subject}",
                    Time = FormatTimeAgo(act.OccurredAt),
                    Type = act.Type
                });
            }

            IsEmpty = dto.TotalCustomers == 0 && dto.OpenDeals == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"CRM Dashboard yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    private static string FormatTimeAgo(DateTime date)
    {
        var diff = DateTime.Now - date;
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} dk once";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} saat once";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} gun once";
        return date.ToString("dd.MM.yyyy");
    }
}

public class CrmPipelineSummaryVm
{
    public string Stage { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Amount { get; set; } = "0 TL";
    public string Color { get; set; } = "#3B82F6";
}

public class CrmRecentActivityVm
{
    public string Description { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
