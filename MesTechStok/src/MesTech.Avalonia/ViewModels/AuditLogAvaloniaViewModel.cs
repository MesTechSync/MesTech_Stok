using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Audit Log ViewModel — MediatR hazır, handler oluşturulunca gerçek veriye geçecek.
/// </summary>
public partial class AuditLogAvaloniaViewModel : ViewModelBase
{
    private readonly ISender _mediator;

    public AuditLogAvaloniaViewModel(ISender mediator)
    {
        _mediator = mediator;
    }

    [ObservableProperty] private string exportMessage = string.Empty;
    [ObservableProperty] private bool isExported;

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
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            // DEV1-DEPENDENCY: GetAuditLogsQuery handler oluşturulunca gerçek veriye geçecek
            // await _mediator.Send(new GetAuditLogsQuery(tenantId, startDate, endDate, selectedUser, selectedAction));
            LogEntries.Clear();
            // Handler hazır olana kadar boş liste — mock data kaldırıldı

            IsEmpty = LogEntries.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Denetim kayitlari yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
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
    private async Task ExportCsvAsync()
    {
        IsExported = false;
        ExportMessage = string.Empty;
        IsLoading = true;
        try
        {
            await Task.Delay(600); // Simulate export
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
