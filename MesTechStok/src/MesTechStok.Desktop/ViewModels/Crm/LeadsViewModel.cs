using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Domain.Interfaces;
// TODO H28: using MesTech.Application.Features.Crm.Queries.GetLeads;
// TODO H28: using MesTech.Application.Features.Crm.Commands.CreateLead;
// TODO H28: using MesTech.Domain.Enums; // LeadStatus — aktif olunca ekle

namespace MesTechStok.Desktop.ViewModels.Crm;

public partial class LeadsViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string? selectedStatus;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private bool isLoading;

    public ObservableCollection<LeadItemVm> Leads { get; } = [];
    public string[] StatusOptions { get; } = ["Tümü", "Yeni", "İletişime Geçildi", "Nitelikli", "Dönüştürüldü", "Kaybedildi"];
    public string[] SourceOptions { get; } = ["Tümü", "Manuel", "Web", "WhatsApp", "Telegram", "Pazaryeri", "Referans"];

    public LeadsViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    // View yüklenince çağrılır (code-behind: Loaded event veya navigation)
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            // TODO H28: GetLeadsQuery pipeline DEV3 tamamlayınca aktif olacak
            // LeadStatus? statusFilter = SelectedStatus switch
            // {
            //     "Yeni"             => LeadStatus.New,
            //     "İletişime Geçildi"=> LeadStatus.Contacted,
            //     "Nitelikli"        => LeadStatus.Qualified,
            //     "Dönüştürüldü"     => LeadStatus.Converted,
            //     "Kaybedildi"       => LeadStatus.Lost,
            //     _                  => null
            // };
            // var result = await _mediator.Send(new GetLeadsQuery(
            //     tenantId, statusFilter, null, 1, 100));
            // Leads.Clear();
            // foreach (var dto in result.Items) { ... }
            // TotalCount = result.TotalCount;

            // Placeholder verisi — H28'de GetLeadsQuery ile değiştirilir
            await Task.Delay(10);
            Leads.Clear();
            Leads.Add(new LeadItemVm { Id = Guid.NewGuid(), FullName = "Ahmet Yılmaz", Company = "ABC Ltd", Email = "ahmet@abc.com", Phone = "0532 123 45 67", Status = "Yeni", Source = "Web", CreatedAt = DateTime.Now.AddDays(-3) });
            Leads.Add(new LeadItemVm { Id = Guid.NewGuid(), FullName = "Fatma Demir", Company = "XYZ AŞ", Email = "fatma@xyz.com", Phone = "0541 987 65 43", Status = "İletişime Geçildi", Source = "Referans", CreatedAt = DateTime.Now.AddDays(-7) });
            TotalCount = Leads.Count;
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private void CreateLead()
    {
        // CreateLeadDialog aç — H28'de modal dialog implement edilecek
        System.Windows.MessageBox.Show("Yeni Lead formu yakında hazır.", "MesTech CRM");
    }

    [RelayCommand]
    private void OpenDetail(Guid leadId)
    {
        // LeadDetailView navigate — H28'de
        System.Windows.MessageBox.Show($"Lead Detay — ID: {leadId}", "MesTech CRM");
    }

    partial void OnSelectedStatusChanged(string? value)
        => _ = LoadAsync();

    partial void OnSearchTextChanged(string value)
    {
        if (value.Length == 0 || value.Length >= 2)
            _ = LoadAsync();
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
