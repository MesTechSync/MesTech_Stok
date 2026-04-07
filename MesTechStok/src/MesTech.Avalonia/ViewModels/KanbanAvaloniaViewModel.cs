using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Avalonia.Services;
using MesTech.Application.Features.Crm.Queries.GetPipelineKanban;
using global::MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Avalonia adaptation of MesTechStok.Desktop.ViewModels.Crm.DealsViewModel.
/// Identical logic — only System.Windows.MessageBox replaced with IDialogService.
///
/// Reuse score: 97% — single MessageBox line changed.
/// Full migration: extract to MesTech.Presentation.Shared with IDialogService interface.
/// </summary>
public partial class KanbanAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDialogService _dialog;


    public ObservableCollection<KanbanStageVm> Stages { get; } = [];
    public ObservableCollection<DealCardVm> AllDeals { get; } = [];

    public KanbanAvaloniaViewModel(
        IMediator mediator,
        ICurrentUserService currentUser,
        ITenantProvider tenantProvider,
        IDialogService dialog)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
        _dialog = dialog;
        InitDefaultStages();
    }

    private void InitDefaultStages()
    {
        Stages.Clear();
        Stages.Add(new KanbanStageVm { Name = "Ilk Iletisim",   Color = "#3B82F6" });
        Stages.Add(new KanbanStageVm { Name = "Teklif Verildi", Color = "#F59E0B" });
        Stages.Add(new KanbanStageVm { Name = "Muzakere",       Color = "#8B5CF6" });
        Stages.Add(new KanbanStageVm { Name = "Kazanildi",      Color = "#10B981" });
        Stages.Add(new KanbanStageVm { Name = "Kaybedildi",     Color = "#EF4444" });
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var board = await _mediator.Send(new GetPipelineKanbanQuery(tenantId, Guid.Empty), ct);

            Stages.Clear();
            AllDeals.Clear();
            foreach (var stageDto in board.Stages.OrderBy(s => s.Position))
            {
                var stageVm = new KanbanStageVm
                {
                    Name = stageDto.Name,
                    Color = stageDto.Color ?? "#3B82F6"
                };
                foreach (var deal in stageDto.Deals)
                {
                    var card = new DealCardVm
                    {
                        Id = deal.Id,
                        Title = deal.Title,
                        ContactName = deal.ContactName,
                        Amount = deal.Amount,
                        StageName = stageDto.Name
                    };
                    stageVm.Deals.Add(card);
                    AllDeals.Add(card);
                }
                stageVm.DealCount = stageVm.Deals.Count;
                Stages.Add(stageVm);
            }
            IsEmpty = Stages.Count == 0;
        }, "Kanban verileri yuklenirken hata");
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private async Task CreateDeal()
    {
        // IDialogService replaces System.Windows.MessageBox — the ONLY change from WPF ViewModel
        await _dialog.ShowInfoAsync("Yeni Firsat formu yakinda.", "MesTech CRM");
    }

    [RelayCommand]
    private async Task SwitchToList()
    {
        await _dialog.ShowInfoAsync("Liste gorunumune gecmek icin sol menuden 'Siparisler' ekranini kullanin.", "MesTech");
    }
}

public partial class KanbanStageVm : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#3B82F6";
    [ObservableProperty] private int dealCount;
    public ObservableCollection<DealCardVm> Deals { get; } = [];
}

public class DealCardVm
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public decimal Amount { get; set; }
    public string StageName { get; set; } = string.Empty;
}
