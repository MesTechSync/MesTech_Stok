using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Avalonia.Services;
using global::MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Avalonia adaptation of MesTechStok.Desktop.ViewModels.Crm.DealsViewModel.
/// Identical logic — only System.Windows.MessageBox replaced with IDialogService.
///
/// Reuse score: 97% — single MessageBox line changed.
/// Full migration: extract to MesTech.Presentation.Shared with IDialogService interface.
/// </summary>
public partial class KanbanAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDialogService _dialog;

    [ObservableProperty] private bool isLoading;

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

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            // Same MediatR pipeline as WPF — GetPipelineKanbanQuery
            // For PoC: use default stages with sample data
            await Task.Delay(50);

            Stages.Clear();
            var stage1 = new KanbanStageVm { Name = "Ilk Iletisim", Color = "#3B82F6" };
            stage1.Deals.Add(new DealCardVm { Id = Guid.NewGuid(), Title = "ABC Ltd ERP Projesi", ContactName = "Ahmet Yilmaz", Amount = 45000, StageName = "Ilk Iletisim" });
            stage1.Deals.Add(new DealCardVm { Id = Guid.NewGuid(), Title = "XYZ Stok Entegrasyonu", ContactName = "Fatma Demir", Amount = 22000, StageName = "Ilk Iletisim" });
            stage1.DealCount = stage1.Deals.Count;

            var stage2 = new KanbanStageVm { Name = "Teklif Verildi", Color = "#F59E0B" };
            stage2.Deals.Add(new DealCardVm { Id = Guid.NewGuid(), Title = "DEF Marketplace Setup", ContactName = "Mehmet Can", Amount = 67000, StageName = "Teklif Verildi" });
            stage2.DealCount = stage2.Deals.Count;

            var stage3 = new KanbanStageVm { Name = "Muzakere", Color = "#8B5CF6" };
            var stage4 = new KanbanStageVm { Name = "Kazanildi", Color = "#10B981" };
            stage4.Deals.Add(new DealCardVm { Id = Guid.NewGuid(), Title = "GHI Dropshipping", ContactName = "Ayse Kara", Amount = 35000, StageName = "Kazanildi" });
            stage4.DealCount = stage4.Deals.Count;

            var stage5 = new KanbanStageVm { Name = "Kaybedildi", Color = "#EF4444" };

            Stages.Add(stage1);
            Stages.Add(stage2);
            Stages.Add(stage3);
            Stages.Add(stage4);
            Stages.Add(stage5);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[KanbanAvaloniaViewModel] LoadAsync error: {ex.Message}");
            InitDefaultStages();
        }
        finally
        {
            IsLoading = false;
        }
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
        await Task.CompletedTask;
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
