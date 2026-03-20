using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Backup management ViewModel — manual backup, history, restore test status.
/// İ-11 Görev 4D: Backup & restore UI with mock data.
/// </summary>
public partial class BackupAvaloniaViewModel : ObservableObject
{
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isBackingUp;
    [ObservableProperty] private string backupMessage = string.Empty;
    [ObservableProperty] private int backupProgress;

    // Last backup info
    [ObservableProperty] private string lastBackupDate = "20.03.2026 03:00";
    [ObservableProperty] private string lastBackupSize = "1.2 GB";
    [ObservableProperty] private string lastBackupStatus = "Basarili";
    [ObservableProperty] private string lastBackupType = "Full";

    // Restore test
    [ObservableProperty] private string lastRestoreTestDate = "18.03.2026 04:00";
    [ObservableProperty] private string lastRestoreTestResult = "Basarili — 127 tablo dogrulandi";

    // Backup history
    public ObservableCollection<BackupHistoryItem> BackupHistory { get; } = new();

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(300); // Simulate loading

            BackupHistory.Clear();
            BackupHistory.Add(new BackupHistoryItem("20.03.2026 03:00", "Full", "1.2 GB", "45 dk", "Basarili"));
            BackupHistory.Add(new BackupHistoryItem("19.03.2026 03:00", "Incremental", "180 MB", "8 dk", "Basarili"));
            BackupHistory.Add(new BackupHistoryItem("18.03.2026 03:00", "Incremental", "210 MB", "9 dk", "Basarili"));
            BackupHistory.Add(new BackupHistoryItem("17.03.2026 03:00", "Incremental", "95 MB", "5 dk", "Basarili"));
            BackupHistory.Add(new BackupHistoryItem("16.03.2026 03:00", "Incremental", "320 MB", "12 dk", "Basarili"));
            BackupHistory.Add(new BackupHistoryItem("15.03.2026 03:00", "Full", "1.1 GB", "42 dk", "Basarili"));
            BackupHistory.Add(new BackupHistoryItem("14.03.2026 03:00", "Incremental", "150 MB", "7 dk", "Basarisiz"));
            BackupHistory.Add(new BackupHistoryItem("13.03.2026 03:00", "Full", "1.1 GB", "44 dk", "Basarili"));
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Yedekleme gecmisi yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ManualBackupAsync()
    {
        IsBackingUp = true;
        BackupProgress = 0;
        BackupMessage = "Yedekleme baslatiliyor...";
        try
        {
            for (int i = 1; i <= 10; i++)
            {
                await Task.Delay(300);
                BackupProgress = i * 10;
                BackupMessage = i switch
                {
                    <= 3 => "Veritabani tabloları taranıyor...",
                    <= 6 => "Veriler sikistiriliyor...",
                    <= 8 => "Dosya yaziliyor...",
                    _ => "Dogrulama yapiliyor..."
                };
            }
            BackupMessage = "Yedekleme basariyla tamamlandi!";
            LastBackupDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            LastBackupStatus = "Basarili";
        }
        catch (Exception ex)
        {
            BackupMessage = $"Yedekleme hatasi: {ex.Message}";
        }
        finally
        {
            IsBackingUp = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}

public class BackupHistoryItem
{
    public string Date { get; }
    public string Type { get; }
    public string Size { get; }
    public string Duration { get; }
    public string Status { get; }

    public BackupHistoryItem(string date, string type, string size, string duration, string status)
    {
        Date = date;
        Type = type;
        Size = size;
        Duration = duration;
        Status = status;
    }
}
