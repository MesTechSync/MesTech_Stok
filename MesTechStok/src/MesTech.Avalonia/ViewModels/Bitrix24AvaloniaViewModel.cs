using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Crm.Queries.GetBitrix24Pipeline;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// G096: Bitrix24 CRM Kanban — pipeline stage view.
/// DEV1 G097 handlers: GetBitrix24PipelineQuery + UpdateDealStageCommand + GetLeadScoreQuery
/// </summary>
public partial class Bitrix24AvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<Bitrix24AvaloniaViewModel> _logger;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private int totalDeals;
    [ObservableProperty] private decimal totalValue;
    [ObservableProperty] private string? stageFilter;

    public ObservableCollection<PipelineStageItem> Stages { get; } = [];

    public Bitrix24AvaloniaViewModel(IMediator mediator, ILogger<Bitrix24AvaloniaViewModel> logger, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _logger = logger;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _mediator.Send(new GetBitrix24PipelineQuery(_currentUser.TenantId, StageFilter));
            Stages.Clear();
            TotalDeals = result.TotalDeals;
            TotalValue = result.TotalValue;
            foreach (var stage in result.Stages)
                Stages.Add(new PipelineStageItem(stage.StageId, stage.StageName, stage.DealCount, stage.TotalValue));

            if (Stages.Count == 0)
            {
                // Seed default stages for empty pipeline
                Stages.Add(new("new", "Yeni", 0, 0));
                Stages.Add(new("contact", "Temas", 0, 0));
                Stages.Add(new("proposal", "Teklif", 0, 0));
                Stages.Add(new("negotiation", "Muzakere", 0, 0));
                Stages.Add(new("won", "Kazanildi", 0, 0));
                Stages.Add(new("lost", "Kaybedildi", 0, 0));
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Pipeline yuklenemedi: {ex.Message}";
            _logger.LogError(ex, "Bitrix24 pipeline load failed");
            // Fallback stages
            Stages.Clear();
            Stages.Add(new("new", "Yeni", 0, 0));
            Stages.Add(new("contact", "Temas", 0, 0));
            Stages.Add(new("proposal", "Teklif", 0, 0));
            Stages.Add(new("won", "Kazanildi", 0, 0));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private async Task FilterByStageAsync(string? stage)
    {
        StageFilter = stage;
        await LoadAsync();
    }
}

public record PipelineStageItem(string StageId, string StageName, int DealCount, decimal TotalValue);
