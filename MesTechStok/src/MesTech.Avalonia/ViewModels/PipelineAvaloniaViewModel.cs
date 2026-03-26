using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Crm.Queries.GetPipelineKanban;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class PipelineAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;


    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string totalValue = "0 TL";

    public ObservableCollection<PipelineStageVm> Stages { get; } = [];

    public PipelineAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
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
            var board = await _mediator.Send(new GetPipelineKanbanQuery(_currentUser.TenantId, Guid.Empty));

            Stages.Clear();
            decimal grandTotal = board.Stages.SelectMany(s => s.Deals).Sum(d => d.Amount);

            foreach (var stageDto in board.Stages.OrderBy(s => s.Position))
            {
                var pct = grandTotal > 0
                    ? (int)Math.Round(stageDto.TotalAmount / grandTotal * 100)
                    : 0;

                Stages.Add(new PipelineStageVm
                {
                    Name = stageDto.Name,
                    DealCount = stageDto.Deals.Count,
                    Amount = $"{stageDto.TotalAmount:N0} TL",
                    Percentage = pct,
                    Color = stageDto.Color ?? "#3B82F6"
                });
            }

            TotalCount = board.Stages.SelectMany(s => s.Deals).Count();
            TotalValue = $"{grandTotal:N0} TL";
            IsEmpty = Stages.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Pipeline yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class PipelineStageVm
{
    public string Name { get; set; } = string.Empty;
    public int DealCount { get; set; }
    public string Amount { get; set; } = "0 TL";
    public int Percentage { get; set; }
    public string Color { get; set; } = "#3B82F6";
}
