using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTechStok.Desktop.ViewModels.Crm;

public partial class DealsViewModel : ObservableObject
{
    public ObservableCollection<KanbanStageVm> Stages { get; } = [];

    [RelayCommand]
    private void CreateDeal() { /* H27'de implement */ }

    [RelayCommand]
    private void SwitchToList() { /* Liste görünümüne geç */ }

    // Örnek veri — H27'de MediatR ile değiştirilir
    public DealsViewModel()
    {
        Stages.Add(new KanbanStageVm { Name = "İlk İletişim", Color = "#3B82F6", DealCount = 3 });
        Stages.Add(new KanbanStageVm { Name = "Teklif Verildi", Color = "#F59E0B", DealCount = 2 });
        Stages.Add(new KanbanStageVm { Name = "Müzakere", Color = "#8B5CF6", DealCount = 1 });
        Stages.Add(new KanbanStageVm { Name = "Kazanıldı ✓", Color = "#10B981", DealCount = 0 });
        Stages.Add(new KanbanStageVm { Name = "Kaybedildi ✗", Color = "#EF4444", DealCount = 0 });
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
