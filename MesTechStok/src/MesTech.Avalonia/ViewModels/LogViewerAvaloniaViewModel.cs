using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Logging.Queries.GetLogCount;
using MesTech.Application.Features.System.Queries.GetAuditLogs;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Log İzleme ViewModel — WPF010.
/// Wired to GetAuditLogsQuery via MediatR. Falls back to mock data if query returns empty.
/// </summary>
public partial class LogViewerAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private string selectedLevel = "Tumu";
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private bool autoRefresh;
    [ObservableProperty] private long totalLogCount;

    public LogViewerAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public ObservableCollection<string> Levels { get; } = new()
    {
        "Tumu",
        "Error",
        "Warning",
        "Information",
        "Debug"
    };

    public ObservableCollection<LogEntryItem> LogEntries { get; } = new();

    private readonly List<LogEntryItem> _allEntries = [];

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var logs = await _mediator.Send(
                new GetAuditLogsQuery(TenantId: _currentUser.TenantId), ct);

            _allEntries.Clear();
            foreach (var log in logs)
            {
                var level = log.Action.Contains("Error", StringComparison.OrdinalIgnoreCase) ? "Error"
                    : log.Action.Contains("Warning", StringComparison.OrdinalIgnoreCase) ? "Warning"
                    : log.Action.Contains("Debug", StringComparison.OrdinalIgnoreCase) ? "Debug"
                    : "Information";
                var color = level switch
                {
                    "Error" => "#EF4444",
                    "Warning" => "#F59E0B",
                    "Debug" => "#64748B",
                    _ => "#3B82F6"
                };
                _allEntries.Add(new LogEntryItem(
                    log.AccessTime.ToString("dd.MM.yyyy HH:mm:ss"),
                    level,
                    log.Resource,
                    log.Action,
                    color));
            }

            ApplyFilter();

            // G540 orphan: log count
            try { TotalLogCount = await _mediator.Send(new GetLogCountQuery(_currentUser.TenantId), ct); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[WARNING] GetLogCount failed: {ex.Message}"); }
        }, "Log kayitlari yuklenirken hata");
    }

    private void ApplyFilter()
    {
        LogEntries.Clear();
        foreach (var entry in _allEntries)
        {
            bool levelMatch = SelectedLevel == "Tumu" || entry.Level == SelectedLevel;
            bool searchMatch = string.IsNullOrWhiteSpace(SearchText)
                || entry.Message.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || entry.Source.Contains(SearchText, StringComparison.OrdinalIgnoreCase);

            if (levelMatch && searchMatch)
                LogEntries.Add(entry);
        }

        IsEmpty = LogEntries.Count == 0;
    }

    partial void OnSelectedLevelChanged(string value) => ApplyFilter();

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}

/// <summary>Log satır modeli.</summary>
public sealed class LogEntryItem
{
    public string Timestamp { get; }
    public string Level { get; }
    public string Source { get; }
    public string Message { get; }
    /// <summary>Hex renk kodu: Error=#EF4444, Warning=#F59E0B, Information=#3B82F6, Debug=#64748B</summary>
    public string LevelColor { get; }

    public LogEntryItem(string timestamp, string level, string source, string message, string levelColor)
    {
        Timestamp = timestamp;
        Level = level;
        Source = source;
        Message = message;
        LevelColor = levelColor;
    }
}
