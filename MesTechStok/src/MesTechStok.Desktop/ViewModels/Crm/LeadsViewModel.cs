using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTechStok.Desktop.ViewModels.Crm;

public partial class LeadsViewModel : ObservableObject
{
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string? selectedStatus;
    [ObservableProperty] private string? selectedSource;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<LeadItemVm> Leads { get; } = [];
    public string[] StatusOptions { get; } = ["Tümü", "Yeni", "İletişime Geçildi", "Nitelikli", "Dönüştürüldü", "Kaybedildi"];
    public string[] SourceOptions { get; } = ["Tümü", "Manuel", "Web", "WhatsApp", "Telegram", "Pazaryeri", "Referans"];

    [RelayCommand]
    private void CreateLead()
    {
        // Dalga 8 H27'de MediatR bağlantısı
    }

    [RelayCommand]
    private void OpenDetail(Guid leadId)
    {
        // Dalga 8 H27'de LeadDetailView açılır
    }
}

public class LeadItemVm
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
