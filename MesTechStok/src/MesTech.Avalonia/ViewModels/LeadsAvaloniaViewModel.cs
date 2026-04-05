using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Avalonia.Services;
using MesTech.Application.Features.Crm.Queries.GetLeads;
using MesTech.Application.Features.Reporting.Commands.ExportReport;
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

    private List<LeadItemVm> _allLeads = [];

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string? selectedStatus;
    [ObservableProperty] private int totalCount;

    // Sort
    [ObservableProperty] private string sortColumn = "date";
    [ObservableProperty] private bool sortAscending = false;

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
        await SafeExecuteAsync(async ct =>
        {
            var status = SelectedStatus == "Tumu" ? null : SelectedStatus;
            var result = await _mediator.Send(new GetLeadsQuery(
                _currentUser.TenantId,
                Status: status != null ? Enum.Parse<MesTech.Domain.Enums.LeadStatus>(status.Replace(" ", "")) : null), ct);

            _allLeads = result.Items.Select(lead => new LeadItemVm
            {
                Id = lead.Id,
                FullName = lead.FullName,
                Company = lead.Company,
                Email = lead.Email,
                Phone = lead.Phone,
                Status = lead.Status,
                Source = lead.Source,
                CreatedAt = lead.CreatedAt
            }).ToList();

            TotalCount = result.TotalCount;
            ApplyFilters();
        }, "Lead verileri yuklenirken hata");
    }

    private void ApplyFilters()
    {
        var filtered = _allLeads.AsEnumerable();

        // Search filter (already existing behaviour — re-applied client-side for instant UX)
        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var term = SearchText.Trim().ToLowerInvariant();
            filtered = filtered.Where(l =>
                l.FullName.Contains(term, StringComparison.InvariantCultureIgnoreCase) ||
                (l.Company?.Contains(term, StringComparison.InvariantCultureIgnoreCase) ?? false) ||
                (l.Email?.Contains(term, StringComparison.InvariantCultureIgnoreCase) ?? false));
        }

        // Sort
        filtered = SortColumn switch
        {
            "date"   => SortAscending ? filtered.OrderBy(l => l.CreatedAt)  : filtered.OrderByDescending(l => l.CreatedAt),
            "source" => SortAscending ? filtered.OrderBy(l => l.Source)     : filtered.OrderByDescending(l => l.Source),
            "status" => SortAscending ? filtered.OrderBy(l => l.Status)     : filtered.OrderByDescending(l => l.Status),
            _        => filtered.OrderByDescending(l => l.CreatedAt)
        };

        Leads.Clear();
        foreach (var item in filtered)
            Leads.Add(item);

        IsEmpty = Leads.Count == 0;
    }

    [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column)
            SortAscending = !SortAscending;
        else
        {
            SortColumn = column;
            SortAscending = true;
        }
        ApplyFilters();
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    /// <summary>LoadDataCommand — used by F5 keybinding and Retry button in error state.</summary>
    [RelayCommand]
    private Task LoadData() => LoadAsync();

    [RelayCommand]
    private async Task CreateLead()
    {
        // IDialogService replaces System.Windows.MessageBox — the ONLY change from WPF ViewModel
        await _dialog.ShowInfoAsync("Yeni Lead formu yakinda hazir.", "MesTech CRM");
    }

    // HH-FIX-014b: Excel export
    [RelayCommand]
    private async Task ExportExcel()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new ExportReportCommand(Guid.Empty, "leads", "xlsx"), ct);
            if (result.FileData.Length > 0)
            {
                var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MesTech_Exports");
                System.IO.Directory.CreateDirectory(dir);
                await System.IO.File.WriteAllBytesAsync(System.IO.Path.Combine(dir, result.FileName), result.FileData);
            }
        }, "Lead verileri disa aktarilirken hata");
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
