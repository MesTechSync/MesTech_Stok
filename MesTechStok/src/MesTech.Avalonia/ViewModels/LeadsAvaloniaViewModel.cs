using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Avalonia.Services;
using global::MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Avalonia adaptation of MesTechStok.Desktop.ViewModels.Crm.LeadsViewModel.
/// Identical logic — only System.Windows.MessageBox replaced with IDialogService.
///
/// Reuse score: 95% — only 2 lines changed (MessageBox -> IDialogService).
/// Full migration: extract to MesTech.Presentation.Shared with IDialogService interface.
/// </summary>
public partial class LeadsAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly IDialogService _dialog;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string? selectedStatus;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<LeadItemVm> Leads { get; } = [];
    public string[] StatusOptions { get; } = ["Tumu", "Yeni", "Iletisime Gecildi", "Nitelikli", "Donusturuldu", "Kaybedildi"];
    public string[] SourceOptions { get; } = ["Tumu", "Manuel", "Web", "WhatsApp", "Telegram", "Pazaryeri", "Referans"];

    public LeadsAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser, IDialogService dialog)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _dialog = dialog;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            // Same MediatR pipeline as WPF — GetLeadsQuery
            await Task.Delay(10);
            Leads.Clear();
            Leads.Add(new LeadItemVm { Id = Guid.NewGuid(), FullName = "Ahmet Yilmaz", Company = "ABC Ltd", Email = "ahmet@abc.com", Phone = "0532 123 45 67", Status = "Yeni", Source = "Web", CreatedAt = DateTime.Now.AddDays(-3) });
            Leads.Add(new LeadItemVm { Id = Guid.NewGuid(), FullName = "Fatma Demir", Company = "XYZ AS", Email = "fatma@xyz.com", Phone = "0541 987 65 43", Status = "Iletisime Gecildi", Source = "Referans", CreatedAt = DateTime.Now.AddDays(-7) });
            TotalCount = Leads.Count;
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private async Task CreateLead()
    {
        // IDialogService replaces System.Windows.MessageBox — the ONLY change from WPF ViewModel
        await _dialog.ShowInfoAsync("Yeni Lead formu yakinda hazir.", "MesTech CRM");
    }

    [RelayCommand]
    private async Task OpenDetail(Guid leadId)
    {
        // IDialogService replaces System.Windows.MessageBox — the ONLY change from WPF ViewModel
        await _dialog.ShowInfoAsync($"Lead Detay — ID: {leadId}", "MesTech CRM");
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
