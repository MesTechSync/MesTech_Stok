using System;
using System.ComponentModel;

namespace MesTechStok.Desktop.Models;

/// <summary>
/// Genel sistem performans bilgilerini içeren model sınıfı
/// </summary>
public class SystemPerformance : INotifyPropertyChanged
{
    private double _totalCpuUsage;
    private double _totalMemoryUsage;
    private long _availableMemory;
    private long _totalMemory;
    private int _activeProcessCount;
    private double _mesTechCpuUsage;
    private long _mesTechMemoryUsage;

    /// <summary>
    /// Toplam CPU kullanım yüzdesi
    /// </summary>
    public double TotalCpuUsage
    {
        get => _totalCpuUsage;
        set
        {
            _totalCpuUsage = value;
            OnPropertyChanged(nameof(TotalCpuUsage));
        }
    }

    /// <summary>
    /// Toplam RAM kullanım yüzdesi
    /// </summary>
    public double TotalMemoryUsage
    {
        get => _totalMemoryUsage;
        set
        {
            _totalMemoryUsage = value;
            OnPropertyChanged(nameof(TotalMemoryUsage));
        }
    }

    /// <summary>
    /// Kullanılabilir RAM (bytes)
    /// </summary>
    public long AvailableMemory
    {
        get => _availableMemory;
        set
        {
            _availableMemory = value;
            OnPropertyChanged(nameof(AvailableMemory));
            OnPropertyChanged(nameof(AvailableMemoryGB));
        }
    }

    /// <summary>
    /// Toplam RAM (bytes)
    /// </summary>
    public long TotalMemory
    {
        get => _totalMemory;
        set
        {
            _totalMemory = value;
            OnPropertyChanged(nameof(TotalMemory));
            OnPropertyChanged(nameof(TotalMemoryGB));
            OnPropertyChanged(nameof(UsedMemoryGB));
        }
    }

    /// <summary>
    /// Kullanılabilir RAM GB cinsinden
    /// </summary>
    public double AvailableMemoryGB => AvailableMemory / (1024.0 * 1024.0 * 1024.0);

    /// <summary>
    /// Toplam RAM GB cinsinden
    /// </summary>
    public double TotalMemoryGB => TotalMemory / (1024.0 * 1024.0 * 1024.0);

    /// <summary>
    /// Kullanılan RAM GB cinsinden
    /// </summary>
    public double UsedMemoryGB => (TotalMemory - AvailableMemory) / (1024.0 * 1024.0 * 1024.0);

    /// <summary>
    /// Aktif süreç sayısı
    /// </summary>
    public int ActiveProcessCount
    {
        get => _activeProcessCount;
        set
        {
            _activeProcessCount = value;
            OnPropertyChanged(nameof(ActiveProcessCount));
        }
    }

    /// <summary>
    /// MesTech uygulamalarının toplam CPU kullanımı
    /// </summary>
    public double MesTechCpuUsage
    {
        get => _mesTechCpuUsage;
        set
        {
            _mesTechCpuUsage = value;
            OnPropertyChanged(nameof(MesTechCpuUsage));
        }
    }

    /// <summary>
    /// MesTech uygulamalarının toplam RAM kullanımı (bytes)
    /// </summary>
    public long MesTechMemoryUsage
    {
        get => _mesTechMemoryUsage;
        set
        {
            _mesTechMemoryUsage = value;
            OnPropertyChanged(nameof(MesTechMemoryUsage));
            OnPropertyChanged(nameof(MesTechMemoryUsageMB));
        }
    }

    /// <summary>
    /// MesTech uygulamalarının RAM kullanımı MB cinsinden
    /// </summary>
    public double MesTechMemoryUsageMB => MesTechMemoryUsage / (1024.0 * 1024.0);

    /// <summary>
    /// Son güncelleme zamanı
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.Now;

    /// <summary>
    /// Sistem başlangıç zamanı
    /// </summary>
    public DateTime SystemStartTime { get; set; }

    /// <summary>
    /// Sistemin çalışma süresi
    /// </summary>
    public TimeSpan SystemUptime => DateTime.Now - SystemStartTime;

    // Legacy properties for compatibility
    public double CpuUsage => TotalCpuUsage;
    public double MemoryUsage => TotalMemoryUsage;
    public double DiskUsage { get; set; }
    public long AvailableMemoryMB => AvailableMemory / (1024 * 1024);
    public long TotalMemoryMB => TotalMemory / (1024 * 1024);
    public DateTime Timestamp => LastUpdated;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}