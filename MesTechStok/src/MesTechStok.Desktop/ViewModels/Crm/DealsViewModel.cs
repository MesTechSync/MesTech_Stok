using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTechStok.Desktop.ViewModels.Crm;

public partial class DealsViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private bool isLoading;

    public ObservableCollection<KanbanStageVm> Stages { get; } = [];

    public DealsViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
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

    // GetDealsQuery pipeline H28 DEV1 + DEV3 tamamlayınca tam aktif olacak
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            // TODO H28: await _mediator.Send(new GetPipelineKanbanQuery(...));
            // Şimdilik: stage yapısı hazır, deal kartları H28'de dolacak
            await Task.Delay(10);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private void CreateDeal()
        => System.Windows.MessageBox.Show("Yeni Fırsat formu yakında.", "MesTech CRM");

    [RelayCommand]
    private void SwitchToList() { /* H28 liste görünümü */ }
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
