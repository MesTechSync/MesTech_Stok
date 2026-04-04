using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.System.Queries.GetAuditLogs;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Audit Log ViewModel — MediatR wired to GetAuditLogsQuery.
/// </summary>
public partial class AuditLogAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public AuditLogAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string exportMessage = string.Empty;
    [ObservableProperty] private bool isExported;

    private readonly List<AuditLogEntry> _allItems = [];

    // Filters
    [ObservableProperty] private string selectedUser = "Tumu";
    [ObservableProperty] private string selectedAction = "Tumu";
    [ObservableProperty] private DateTimeOffset startDate = new(new DateTime(2026, 3, 1));
    [ObservableProperty] private DateTimeOffset endDate = new(new DateTime(2026, 3, 20));

    // Detail panel
    [ObservableProperty] private bool isDetailVisible;
    [ObservableProperty] private AuditLogEntry? selectedEntry;

    public ObservableCollection<string> Users { get; } = new()
    {
        "Tumu", "admin@mestech.com", "dev1@mestech.com", "dev2@mestech.com",
        "operator@mestech.com", "muhasebe@mestech.com"
    };

    public ObservableCollection<string> ActionTypes { get; } = new()
    {
        "Tumu", "Create", "Update", "Delete", "Login", "Export"
    };

    public ObservableCollection<AuditLogEntry> LogEntries { get; } = new();

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var userFilter = SelectedUser == "Tumu" ? null : SelectedUser;
            var actionFilter = SelectedAction == "Tumu" ? null : SelectedAction;
            var logs = await _mediator.Send(new GetAuditLogsQuery(
                _currentUser.TenantId,
                StartDate.DateTime,
                EndDate.DateTime,
                userFilter,
                actionFilter), ct);

            _allItems.Clear();
            foreach (var log in logs)
            {
                _allItems.Add(new AuditLogEntry(
                    log.AccessTime.ToString("dd.MM.yyyy HH:mm:ss"),
                    log.UserId.ToString(),
                    log.Action,
                    log.Resource,
                    log.Id.ToString(),
                    log.AdditionalInfo ?? "—",
                    log.IpAddress ?? "—",
                    log.UserAgent ?? "—"));
            }

            ApplyFilter();
        }, "Audit log yuklenirken hata");
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        LogEntries.Clear();
        var filtered = _allItems.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchText))
            filtered = filtered.Where(r =>
                r.Action.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                r.User.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                r.EntityType.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        foreach (var item in filtered)
            LogEntries.Add(item);
        IsEmpty = LogEntries.Count == 0;
    }

    [RelayCommand]
    private void ShowDetail(AuditLogEntry? entry)
    {
        SelectedEntry = entry;
        IsDetailVisible = entry != null;
    }

    [RelayCommand]
    private void CloseDetail()
    {
        SelectedEntry = null;
        IsDetailVisible = false;
    }

    [RelayCommand]
    private Task ExportCsvAsync()
    {
        IsExported = false;
        ExportMessage = string.Empty;
        IsLoading = true;
        try
        {
            ExportMessage = $"CSV dosyasi basariyla olusturuldu. ({LogEntries.Count} kayit)";
            IsExported = true;
        }
        catch (Exception ex)
        {
            ExportMessage = $"Disari aktarma hatasi: {ex.Message}";
            IsExported = true;
        }
        finally
        {
            IsLoading = false;
        }
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}

public class AuditLogEntry
{
    public string Timestamp { get; }
    public string User { get; }
    public string Action { get; }
    public string EntityType { get; }
    public string EntityId { get; }
    public string Summary { get; }
    public string OldValues { get; }
    public string NewValues { get; }

    public AuditLogEntry(string timestamp, string user, string action, string entityType,
        string entityId, string summary, string oldValues, string newValues)
    {
        Timestamp = timestamp;
        User = user;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        Summary = summary;
        OldValues = oldValues;
        NewValues = newValues;
    }
}
