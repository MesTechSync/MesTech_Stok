using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Tasks.Queries.GetProjects;
using MesTech.Avalonia.Services;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class ProjectsAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly IDialogService _dialog;


    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string? selectedStatus;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<ProjectItemVm> Projects { get; } = [];
    public string[] StatusOptions { get; } = ["Tumu", "Planlandi", "Devam Ediyor", "Tamamlandi", "Beklemede"];

    public ProjectsAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser, IDialogService dialog)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _dialog = dialog;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new GetProjectsQuery(_currentUser.TenantId), ct);

            Projects.Clear();
            foreach (var dto in result)
            {
                var progress = dto.TaskCount > 0
                    ? (int)Math.Round((double)dto.CompletedTaskCount / dto.TaskCount * 100)
                    : 0;

                Projects.Add(new ProjectItemVm
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    Status = dto.Status,
                    StartDate = dto.DueDate?.AddMonths(-3) ?? DateTime.Now,
                    EndDate = dto.DueDate,
                    Progress = progress
                });
            }
            TotalCount = Projects.Count;
            IsEmpty = Projects.Count == 0;
        }, "Projeler yuklenirken hata");
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task Add()
    {
        await _dialog.ShowInfoAsync("Bu özellik yakinda aktif olacak.", "MesTech");
    }

    partial void OnSelectedStatusChanged(string? value)
        => _ = LoadAsync();

    partial void OnSearchTextChanged(string value)
    {
        if (value.Length == 0 || value.Length >= 2)
            _ = LoadAsync();
    }
}

public class ProjectItemVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Progress { get; set; }
    public string? Manager { get; set; }

    public string DateRange => $"{StartDate:dd.MM.yyyy} — {EndDate?.ToString("dd.MM.yyyy") ?? "—"}";
    public string ProgressDisplay => $"%{Progress}";
}
