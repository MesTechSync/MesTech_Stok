using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Crm.Queries.GetBitrix24Deals;
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
    [ObservableProperty] private int dealCount;
    [ObservableProperty] private string searchText = string.Empty;

    private readonly List<PipelineStageItem> _allStages = [];

    public ObservableCollection<PipelineStageItem> Stages { get; } = [];

    public Bitrix24AvaloniaViewModel(IMediator mediator, ILogger<Bitrix24AvaloniaViewModel> logger, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _logger = logger;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new GetBitrix24PipelineQuery(_currentUser.TenantId, StageFilter), ct);
            _allStages.Clear();
            TotalDeals = result.TotalDeals;
            TotalValue = result.TotalValue;
            foreach (var stage in result.Stages)
                _allStages.Add(new PipelineStageItem(stage.StageId, stage.StageName, stage.DealCount, stage.TotalValue));

            if (_allStages.Count == 0)
            {
                // Seed default stages for empty pipeline
                _allStages.Add(new("new", "Yeni", 0, 0));
                _allStages.Add(new("contact", "Temas", 0, 0));
                _allStages.Add(new("proposal", "Teklif", 0, 0));
                _allStages.Add(new("negotiation", "Muzakere", 0, 0));
                _allStages.Add(new("won", "Kazanildi", 0, 0));
                _allStages.Add(new("lost", "Kaybedildi", 0, 0));
            }
            ApplyFilter();
            IsEmpty = Stages.Count == 0;

            // Deals count (G540 orphan wire)
            try
            {
                var deals = await _mediator.Send(new GetBitrix24DealsQuery(_currentUser.TenantId), ct);
                DealCount = deals.TotalCount;
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[WARNING] GetBitrix24Deals failed: {ex.Message}"); DealCount = 0; }
        }, "Pipeline yuklenirken hata");
    }

    // ── Search Filter ────────────────────────────────────────────────────────
    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        Stages.Clear();
        var filtered = _allStages.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchText))
            filtered = filtered.Where(s =>
                s.StageName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                s.StageId.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        foreach (var item in filtered) Stages.Add(item);
        IsEmpty = Stages.Count == 0;
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
