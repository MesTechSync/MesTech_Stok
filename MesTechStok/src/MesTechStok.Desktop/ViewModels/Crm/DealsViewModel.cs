using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Domain.Interfaces;
using MesTech.Application.Features.Crm.Queries.GetDeals;
using MesTech.Application.Features.Crm.Queries.GetPipelineKanban;

namespace MesTechStok.Desktop.ViewModels.Crm;

public partial class DealsViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    [ObservableProperty] private bool isLoading;

    public ObservableCollection<KanbanStageVm> Stages { get; } = [];

    /// <summary>Flat list for list-view mode — populated by LoadAsListAsync</summary>
    public ObservableCollection<DealCardVm> AllDeals { get; } = [];

    public DealsViewModel(IMediator mediator, ICurrentUserService currentUser, ITenantProvider tenantProvider)
    {
        _mediator       = mediator;
        _currentUser    = currentUser;
        _tenantProvider = tenantProvider;
        InitDefaultStages();
    }

    private void InitDefaultStages()
    {
        Stages.Clear();
        Stages.Add(new KanbanStageVm { Name = "İlk İletişim",   Color = "#3B82F6" });
        Stages.Add(new KanbanStageVm { Name = "Teklif Verildi", Color = "#F59E0B" });
        Stages.Add(new KanbanStageVm { Name = "Müzakere",       Color = "#8B5CF6" });
        Stages.Add(new KanbanStageVm { Name = "Kazanıldı ✓",    Color = "#10B981" });
        Stages.Add(new KanbanStageVm { Name = "Kaybedildi ✗",   Color = "#EF4444" });
    }

    /// <summary>Kanban görünümü için pipeline verilerini yükler (MediatR — GetPipelineKanbanQuery).</summary>
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var result   = await _mediator.Send(new GetPipelineKanbanQuery(tenantId, Guid.Empty));

            Stages.Clear();
            foreach (var stage in result.Stages)
            {
                var stageVm = new KanbanStageVm
                {
                    Name  = stage.Name,
                    Color = stage.Color ?? "#3B82F6",
                };

                foreach (var deal in stage.Deals)
                {
                    stageVm.Deals.Add(new DealCardVm
                    {
                        Id          = deal.Id,
                        Title       = deal.Title,
                        ContactName = deal.ContactName,
                        Amount      = deal.Amount,
                        StageName   = deal.StageName,
                    });
                }

                stageVm.DealCount = stageVm.Deals.Count;
                Stages.Add(stageVm);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DealsViewModel] LoadAsync error: {ex.Message}");
            // Fallback: restore default stage structure so the UI is never empty
            InitDefaultStages();
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Liste görünümü için düz deal listesini yükler (MediatR — GetDealsQuery).</summary>
    private async Task LoadAsListAsync()
    {
        IsLoading = true;
        try
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var result   = await _mediator.Send(new GetDealsQuery(
                TenantId: tenantId,
                Status:   null,
                Page:     1,
                PageSize: 50));

            AllDeals.Clear();
            foreach (var deal in result.Items)
            {
                AllDeals.Add(new DealCardVm
                {
                    Id          = deal.Id,
                    Title       = deal.Title,
                    ContactName = deal.ContactName,
                    Amount      = deal.Amount,
                    StageName   = deal.StageName,
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DealsViewModel] LoadAsListAsync error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private void CreateDeal()
        => System.Windows.MessageBox.Show("Yeni Fırsat formu yakında.", "MesTech CRM");

    [RelayCommand]
    private async Task SwitchToList() => await LoadAsListAsync();
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
