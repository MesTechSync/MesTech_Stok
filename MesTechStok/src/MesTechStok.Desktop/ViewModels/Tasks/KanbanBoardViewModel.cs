using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Tasks.Queries.GetProjectTasks;

namespace MesTechStok.Desktop.ViewModels.Tasks;

public partial class KanbanBoardViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private Guid _projectId;

    [ObservableProperty] private string projectName = "Proje";
    [ObservableProperty] private bool isLoading;

    public ObservableCollection<TaskCardVm> BacklogTasks { get; } = [];
    public ObservableCollection<TaskCardVm> TodoTasks { get; } = [];
    public ObservableCollection<TaskCardVm> InProgressTasks { get; } = [];
    public ObservableCollection<TaskCardVm> InReviewTasks { get; } = [];
    public ObservableCollection<TaskCardVm> DoneTasks { get; } = [];

    public KanbanBoardViewModel(IMediator mediator) => _mediator = mediator;

    public async Task LoadAsync(Guid projectId, string projectName)
    {
        _projectId = projectId;
        ProjectName = projectName;
        IsLoading = true;
        try
        {
            var tasks = await _mediator.Send(new GetProjectTasksQuery(projectId));
            BacklogTasks.Clear(); TodoTasks.Clear(); InProgressTasks.Clear();
            InReviewTasks.Clear(); DoneTasks.Clear();

            foreach (var t in tasks)
            {
                var card = new TaskCardVm { Id = t.Id, Title = t.Title, DueDate = t.DueDate, Priority = t.Priority };
                var target = t.Status switch
                {
                    "Backlog"    => BacklogTasks,
                    "Todo"       => TodoTasks,
                    "InProgress" => InProgressTasks,
                    "InReview"   => InReviewTasks,
                    "Done"       => DoneTasks,
                    _            => BacklogTasks
                };
                target.Add(card);
            }
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void Back() { /* ProjectsView'a dön */ }

    [RelayCommand]
    private void CreateTask()
        => System.Windows.MessageBox.Show("Yeni Görev formu yakında.", "MesTech Görevler");
}

public class TaskCardVm
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
}
