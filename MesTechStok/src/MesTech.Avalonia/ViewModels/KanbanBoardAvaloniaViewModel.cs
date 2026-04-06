using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Crm.Queries.GetPipelineKanban;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class KanbanBoardAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;

    public ObservableCollection<TaskColumnVm> Columns { get; } = [];

    public KanbanBoardAvaloniaViewModel(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var board = await _mediator.Send(
                new GetPipelineKanbanQuery(tenantId, PipelineId: Guid.Empty), ct);

            Columns.Clear();
            foreach (var stage in board.Stages.OrderBy(s => s.Position))
            {
                var col = new TaskColumnVm
                {
                    Name = stage.Name,
                    Color = stage.Color ?? "#3B82F6"
                };
                foreach (var deal in stage.Deals)
                {
                    col.Tasks.Add(new TaskCardVm
                    {
                        Id = deal.Id,
                        Title = deal.Title,
                        Assignee = deal.ContactName,
                        DueDate = deal.ExpectedCloseDate,
                    });
                }
                col.TaskCount = col.Tasks.Count;
                Columns.Add(col);
            }

            IsEmpty = Columns.Count == 0;
        }, "Kanban panosu yuklenirken hata");
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
