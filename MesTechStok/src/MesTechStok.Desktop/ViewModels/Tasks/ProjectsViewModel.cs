using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Domain.Interfaces;
// TODO H28: using MesTech.Application.Features.Tasks.Queries.GetProjects;

namespace MesTechStok.Desktop.ViewModels.Tasks;

public partial class ProjectsViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private bool isLoading;

    public ObservableCollection<ProjectCardVm> Projects { get; } = [];

    public ProjectsViewModel(IMediator mediator, ICurrentUserService currentUser)
        => (_mediator, _currentUser) = (mediator, currentUser);

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            // TODO: GetProjectsQuery H28 DEV3 tamamlayınca bağlanır
            // Placeholder:
            Projects.Clear();
            Projects.Add(new ProjectCardVm { Id = Guid.NewGuid(), Name = "Trendyol Entegrasyon", Status = "Active", Color = "#FF6600", CompletionPercent = 65, TaskSummary = "13 / 20 görev tamamlandı" });
            Projects.Add(new ProjectCardVm { Id = Guid.NewGuid(), Name = "Hepsiburada Fatura", Status = "Planning", Color = "#3B82F6", CompletionPercent = 20, TaskSummary = "4 / 20 görev tamamlandı" });
            await Task.CompletedTask;
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
