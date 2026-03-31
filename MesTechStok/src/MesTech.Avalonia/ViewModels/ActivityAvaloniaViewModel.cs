using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Crm.Queries.GetCrmActivities;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class ActivityAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private string? selectedFilter;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<ActivityItemVm> Activities { get; } = [];
    public string[] FilterOptions { get; } = ["Tumu", "Arama", "Toplanti", "E-posta", "Not", "Gorev"];

    public ActivityAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _mediator.Send(new GetCrmActivitiesQuery(_currentUser.TenantId));
            Activities.Clear();
            foreach (var a in result.Activities)
            {
                Activities.Add(new ActivityItemVm
                {
                    Id = a.Id,
                    Type = a.Type.ToString(),
                    Subject = a.Subject,
                    Description = a.Description,
                    ContactName = a.ContactName,
                    ActivityDate = a.OccurredAt,
                    CreatedBy = "—"
                });
            }
            TotalCount = result.TotalCount;
            IsEmpty = Activities.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Aktiviteler yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class ActivityItemVm
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ContactName { get; set; }
    public DateTime ActivityDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    public string TimeAgo
    {
        get
        {
            var diff = DateTime.Now - ActivityDate;
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} dk once";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} saat once";
            return $"{(int)diff.TotalDays} gun once";
        }
    }

    public string TypeIcon => Type switch
    {
        "Arama" => "T",    // phone icon placeholder
        "E-posta" => "@",
        "Toplanti" => "M",
        "Not" => "N",
        "Gorev" => "G",
        _ => "?"
    };
}
