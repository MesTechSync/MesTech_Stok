using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Tasks.Queries.GetProjectTasks;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Gorevler ViewModel — gorev listesi + durum takibi.
/// MediatR query ile veri çeker, handler yoksa graceful fallback.
/// </summary>
public partial class WorkTaskAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<WorkTaskItemDto> Tasks { get; } = [];

    public WorkTaskAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
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
            var query = new GetProjectTasksQuery(_currentUser.TenantId);
            var result = await _mediator.Send(query);

            Tasks.Clear();
            foreach (var task in result)
            {
                Tasks.Add(new WorkTaskItemDto
                {
                    TaskName = task.Title,
                    Priority = task.Priority,
                    Status = task.Status,
                    DueDate = task.DueDate?.ToString("dd.MM.yyyy") ?? "-"
                });
            }

            TotalCount = Tasks.Count;
            IsEmpty = TotalCount == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Gorevler yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            _ = LoadAsync();
            return;
        }

        var filtered = Tasks.Where(t =>
            t.TaskName.Contains(value, System.StringComparison.OrdinalIgnoreCase) ||
            t.AssignedTo.Contains(value, System.StringComparison.OrdinalIgnoreCase)).ToList();

        TotalCount = filtered.Count;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class WorkTaskItemDto
{
    public string TaskName { get; set; } = string.Empty;
    public string AssignedTo { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
}
