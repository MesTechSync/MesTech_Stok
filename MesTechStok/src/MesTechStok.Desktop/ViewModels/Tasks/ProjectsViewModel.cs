using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Tasks.Queries.GetProjects;
using MesTech.Domain.Interfaces;

namespace MesTechStok.Desktop.ViewModels.Tasks;

public partial class ProjectsViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;

    [ObservableProperty] private bool isLoading;

    public ObservableCollection<ProjectCardVm> Projects { get; } = [];

    public ProjectsViewModel(IMediator mediator, ITenantProvider tenantProvider)
        => (_mediator, _tenantProvider) = (mediator, tenantProvider);

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var projects = await _mediator.Send(
                new GetProjectsQuery(_tenantProvider.GetCurrentTenantId()));

            Projects.Clear();
            foreach (var p in projects)
            {
                Projects.Add(new ProjectCardVm
                {
                    Id = p.Id,
                    Name = p.Name,
                    Status = p.Status,
                    Color = "#2855AC",
                    CompletionPercent = p.TaskCount > 0
                        ? Math.Round((double)p.CompletedTaskCount / p.TaskCount * 100, 0)
                        : 0,
                    TaskSummary = $"{p.CompletedTaskCount} / {p.TaskCount} görev tamamlandı"
                });
            }
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void CreateProject()
        => System.Windows.MessageBox.Show("Yeni Proje formu yakında.", "MesTech Görevler");

    [RelayCommand]
    private void OpenKanban(Guid projectId)
    {
        // KanbanBoardView'a navigate
        System.Windows.MessageBox.Show($"Kanban Board — Proje: {projectId}", "MesTech Görevler");
    }
}

public partial class ProjectCardVm : ObservableObject
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Color { get; set; }
    [ObservableProperty] private double completionPercent;
    public string TaskSummary { get; set; } = string.Empty;
}
