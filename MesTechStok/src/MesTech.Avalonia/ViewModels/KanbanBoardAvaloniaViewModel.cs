using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

public partial class KanbanBoardAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


    public ObservableCollection<TaskColumnVm> Columns { get; } = [];

    public KanbanBoardAvaloniaViewModel(IMediator mediator)
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
            await Task.Delay(50);
            Columns.Clear();

            var todo = new TaskColumnVm { Name = "Yapilacak", Color = "#3B82F6" };
            todo.Tasks.Add(new TaskCardVm { Id = Guid.NewGuid(), Title = "API dokumanlarini guncelle", Assignee = "Mehmet C.", Priority = "Orta", DueDate = DateTime.Now.AddDays(3) });
            todo.Tasks.Add(new TaskCardVm { Id = Guid.NewGuid(), Title = "Musteri raporu hazirla", Assignee = "Ayse K.", Priority = "Yuksek", DueDate = DateTime.Now.AddDays(1) });
            todo.Tasks.Add(new TaskCardVm { Id = Guid.NewGuid(), Title = "Test senaryolari yaz", Assignee = "Ali K.", Priority = "Dusuk", DueDate = DateTime.Now.AddDays(5) });
            todo.TaskCount = todo.Tasks.Count;

            var inProgress = new TaskColumnVm { Name = "Devam Eden", Color = "#F59E0B" };
            inProgress.Tasks.Add(new TaskCardVm { Id = Guid.NewGuid(), Title = "Trendyol entegrasyon testi", Assignee = "Fatih I.", Priority = "Yuksek", DueDate = DateTime.Now.AddDays(2) });
            inProgress.Tasks.Add(new TaskCardVm { Id = Guid.NewGuid(), Title = "UI/UX iyilestirme", Assignee = "Zeynep A.", Priority = "Orta", DueDate = DateTime.Now.AddDays(4) });
            inProgress.TaskCount = inProgress.Tasks.Count;

            var done = new TaskColumnVm { Name = "Tamamlandi", Color = "#10B981" };
            done.Tasks.Add(new TaskCardVm { Id = Guid.NewGuid(), Title = "Veritabani yedekleme", Assignee = "Fatih I.", Priority = "Yuksek", DueDate = DateTime.Now.AddDays(-1) });
            done.TaskCount = done.Tasks.Count;

            Columns.Add(todo);
            Columns.Add(inProgress);
            Columns.Add(done);

            IsEmpty = Columns.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Kanban panosu yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public partial class TaskColumnVm : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#3B82F6";
    [ObservableProperty] private int taskCount;
    public ObservableCollection<TaskCardVm> Tasks { get; } = [];
}

public class TaskCardVm
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Assignee { get; set; }
    public string Priority { get; set; } = "Orta";
    public DateTime? DueDate { get; set; }

    public string DueDateDisplay => DueDate?.ToString("dd.MM") ?? "";

    public string PriorityColor => Priority switch
    {
        "Yuksek" => "#EF4444",
        "Orta" => "#F59E0B",
        "Dusuk" => "#10B981",
        _ => "#64748B"
    };
}
