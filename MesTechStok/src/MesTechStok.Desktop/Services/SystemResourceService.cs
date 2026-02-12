using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MesTechStok.Desktop.Models;
using System.Runtime.InteropServices;

namespace MesTechStok.Desktop.Services;

public interface ISystemResourceService
{
    Task<SystemPerformance> GetSystemPerformanceAsync();
    Task<string> GetSystemInfoAsync();
    Task<bool> IsSystemHealthyAsync();
}

/// <summary>
/// Desktop için entegre sistem kaynak izleme servisi
/// </summary>
public class SystemResourceService : ISystemResourceService
{
    private readonly ILogger<SystemResourceService> _logger;
    private readonly PerformanceCounter _cpuCounter;
    private readonly PerformanceCounter _ramCounter;
    private readonly Timer _monitoringTimer;
    private readonly object _lockObject = new();
    private bool _isRunning = false;

    // MesTech application identifiers
    private readonly string[] _mesTechProcessNames =
    {
        "MesTechStok.Desktop",
        "MesTechStok.MainPanel",
        "MesTechStok.Screensaver",
        "MesTechStok.SystemResources"
    };

    public SystemPerformance SystemPerformance { get; } = new();

    // Events
    public event EventHandler<SystemPerformance>? PerformanceUpdated;

    public SystemResourceService(ILogger<SystemResourceService> logger)
    {
        _logger = logger;

        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ALPHA TEAM: Failed to initialize performance counters");
            throw;
        }

        // Initialize monitoring timer
        _monitoringTimer = new Timer(MonitorSystem, null, Timeout.Infinite, Timeout.Infinite);

        // Get system start time
        SystemPerformance.SystemStartTime = DateTime.Now - TimeSpan.FromMilliseconds(Environment.TickCount);
    }

    #region Windows Job Object CPU Throttling (advanced)
    // P/Invoke for Job Objects
    private const int JobObjectCpuRateControlInformation = 15; // JOBOBJECTINFOCLASS
    private const uint JOB_OBJECT_CPU_RATE_CONTROL_ENABLE = 0x1;
    private const uint JOB_OBJECT_CPU_RATE_CONTROL_HARD_CAP = 0x4; // Hard cap mode

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_CPU_RATE_CONTROL_INFORMATION
    {
        public uint ControlFlags; // Flags
        public uint CpuRate;      // 1-10000 (1% = 100)
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateJobObjectW(IntPtr lpJobAttributes, string? name);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetInformationJobObject(IntPtr hJob, int infoClass, ref JOBOBJECT_CPU_RATE_CONTROL_INFORMATION lpJobObjectInfo, uint cbJobObjectInfoLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint access, bool inherit, int pid);

    private const uint PROCESS_ALL_ACCESS = 0x001F0FFF;

    /// <summary>
    /// Belirli bir süreci hedef CPU yüzdesi ile sınırlamaya çalışır (hard cap). Admin yetkisi gerekebilir.
    /// </summary>
    private bool TryThrottleProcessCpu(int pid, int cpuPercentCap)
    {
        try
        {
            if (cpuPercentCap < 1) cpuPercentCap = 1;
            if (cpuPercentCap > 99) cpuPercentCap = 99;

            // 1% = 100 birim
            uint cpuRate = (uint)(cpuPercentCap * 100);
            var job = CreateJobObjectW(IntPtr.Zero, null);
            if (job == IntPtr.Zero)
                return false;

            var info = new JOBOBJECT_CPU_RATE_CONTROL_INFORMATION
            {
                ControlFlags = JOB_OBJECT_CPU_RATE_CONTROL_ENABLE | JOB_OBJECT_CPU_RATE_CONTROL_HARD_CAP,
                CpuRate = cpuRate
            };

            if (!SetInformationJobObject(job, JobObjectCpuRateControlInformation, ref info, (uint)Marshal.SizeOf<JOBOBJECT_CPU_RATE_CONTROL_INFORMATION>()))
                return false;

            var hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, pid);
            if (hProcess == IntPtr.Zero)
                return false;

            return AssignProcessToJobObject(job, hProcess);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"CPU throttle failed for PID={pid}");
            return false;
        }
    }

    /// <summary>
    /// MesTech dışı süreçlere CPU üst limiti uygular (örn. %70). Yoğunlukla UI-odaklı olmayan arkaplan süreçlerinde etkilidir.
    /// </summary>
    public async Task ApplyThrottlingForNonMesTechAsync(int cpuCapPercent = 70)
    {
        await Task.Run(() =>
        {
            try
            {
                var processes = Process.GetProcesses();
                foreach (var p in processes)
                {
                    try
                    {
                        if (IsMesTechProcess(p.ProcessName))
                            continue;

                        // Kritik Windows süreçlerini geç
                        var name = p.ProcessName.ToLowerInvariant();
                        if (name is "system" or "registry" or "idle" or "wininit" or "services" or "lsass" or "svchost" or "explorer")
                            continue;

                        TryThrottleProcessCpu(p.Id, cpuCapPercent);
                    }
                    catch { /* ignore one-by-one errors */ }
                    finally { p.Dispose(); }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Global throttling failed");
            }
        });
    }

    private bool IsMesTechProcess(string processName)
    {
        return _mesTechProcessNames.Any(n => processName.StartsWith(n, StringComparison.OrdinalIgnoreCase));
    }
    #endregion

    public void Start()
    {
        if (!_isRunning)
        {
            _isRunning = true;
            _monitoringTimer.Change(0, 2000); // Her 2 saniyede bir güncelle
            _logger.LogInformation("SystemResourceService started in Desktop");
        }
    }

    public void Stop()
    {
        if (_isRunning)
        {
            _isRunning = false;
            _monitoringTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _logger.LogInformation("SystemResourceService stopped");
        }
    }

    /// <summary>
    /// Sistem izleme ana metodu
    /// </summary>
    private void MonitorSystem(object? state)
    {
        if (!_isRunning) return;

        try
        {
            lock (_lockObject)
            {
                UpdateSystemPerformance();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "System monitoring error");
        }
    }

    /// <summary>
    /// Sistem performans bilgilerini günceller
    /// </summary>
    private void UpdateSystemPerformance()
    {
        try
        {
            // CPU Usage
            SystemPerformance.TotalCpuUsage = Math.Max(0, Math.Min(100, _cpuCounter.NextValue()));

            // Memory Usage
            var availableMemoryMB = _ramCounter.NextValue();
            var totalMemoryBytes = GetTotalPhysicalMemory();

            SystemPerformance.TotalMemory = totalMemoryBytes;
            SystemPerformance.AvailableMemory = (long)(availableMemoryMB * 1024 * 1024);
            SystemPerformance.TotalMemoryUsage = ((double)(totalMemoryBytes - SystemPerformance.AvailableMemory) / totalMemoryBytes) * 100;

            // Process Count
            SystemPerformance.ActiveProcessCount = Process.GetProcesses().Length;

            // MesTech specific metrics
            CalculateMesTechMetrics();

            SystemPerformance.LastUpdated = DateTime.Now;
            PerformanceUpdated?.Invoke(this, SystemPerformance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Performance update error");
        }
    }

    /// <summary>
    /// MesTech uygulamalarının performans metriklerini hesaplar
    /// </summary>
    private void CalculateMesTechMetrics()
    {
        try
        {
            var allProcesses = Process.GetProcesses();
            var mesTechProcesses = allProcesses.Where(p =>
                _mesTechProcessNames.Any(name =>
                    p.ProcessName.StartsWith(name, StringComparison.OrdinalIgnoreCase))).ToArray();

            double totalMesTechCpu = 0;
            long totalMesTechMemory = 0;

            foreach (var process in mesTechProcesses)
            {
                try
                {
                    totalMesTechCpu += GetProcessCpuUsage(process);
                    totalMesTechMemory += process.WorkingSet64;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "MesTech process metric error");
                }
            }

            SystemPerformance.MesTechCpuUsage = Math.Min(100, totalMesTechCpu);
            SystemPerformance.MesTechMemoryUsage = totalMesTechMemory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MesTech metrics error");
        }
    }

    /// <summary>
    /// Yaklaşık CPU kullanımı hesaplaması
    /// </summary>
    private double GetProcessCpuUsage(Process process)
    {
        try
        {
            var workingSet = process.WorkingSet64 / (1024.0 * 1024.0);
            return Math.Min(10, workingSet / 100);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Toplam fiziksel bellek miktarını alır
    /// </summary>
    private long GetTotalPhysicalMemory()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            foreach (var obj in searcher.Get())
            {
                return Convert.ToInt64(obj["TotalPhysicalMemory"]);
            }
        }
        catch
        {
            return 8L * 1024 * 1024 * 1024; // 8 GB default
        }
        return 0;
    }

    public async Task<SystemPerformance> GetSystemPerformanceAsync()
    {
        try
        {
            // Get CPU usage
            var cpuUsage = await Task.Run(() => _cpuCounter.NextValue());

            // Get memory info
            var availableMemory = _ramCounter.NextValue();
            var totalMemory = GetTotalMemoryMB();
            var usedMemory = totalMemory - availableMemory;
            var memoryUsage = (usedMemory / totalMemory) * 100;

            // Get disk usage
            var diskUsage = GetDiskUsage();

            var performance = new SystemPerformance
            {
                TotalCpuUsage = Math.Round(cpuUsage, 1),
                TotalMemoryUsage = Math.Round(memoryUsage, 1),
                DiskUsage = Math.Round(diskUsage, 1),
                AvailableMemory = (long)(availableMemory * 1024 * 1024),
                TotalMemory = (long)(totalMemory * 1024 * 1024),
                LastUpdated = DateTime.Now
            };
            return performance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ALPHA TEAM: Failed to get system performance");
            return new SystemPerformance
            {
                TotalCpuUsage = 0,
                TotalMemoryUsage = 0,
                DiskUsage = 0,
                AvailableMemory = 0,
                TotalMemory = 0,
                LastUpdated = DateTime.Now
            };
        }
    }

    public async Task<string> GetSystemInfoAsync()
    {
        try
        {
            var computerName = Environment.MachineName;
            var userName = Environment.UserName;
            var osVersion = Environment.OSVersion.ToString();
            var processorCount = Environment.ProcessorCount;

            var performance = await GetSystemPerformanceAsync();

            return $"Computer: {computerName}\n" +
                   $"User: {userName}\n" +
                   $"OS: {osVersion}\n" +
                   $"CPU Cores: {processorCount}\n" +
                   $"CPU Usage: {performance.CpuUsage}%\n" +
                   $"Memory: {performance.AvailableMemoryMB:F0}/{performance.TotalMemoryMB:F0} MB\n" +
                   $"Memory Usage: {performance.MemoryUsage:F1}%\n" +
                   $"Disk Usage: {performance.DiskUsage:F1}%";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ALPHA TEAM: Failed to get system info");
            return "System information unavailable";
        }
    }

    public async Task<bool> IsSystemHealthyAsync()
    {
        try
        {
            var performance = await GetSystemPerformanceAsync();

            // System is healthy if:
            // - CPU usage < 80%
            // - Memory usage < 85%
            // - Disk usage < 90%
            return performance.CpuUsage < 80 &&
                   performance.MemoryUsage < 85 &&
                   performance.DiskUsage < 90;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ALPHA TEAM: Failed to check system health");
            return false;
        }
    }

    private double GetTotalMemoryMB()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            var collection = searcher.Get();

            foreach (ManagementObject mo in collection)
            {
                var totalMemoryBytes = Convert.ToDouble(mo["TotalPhysicalMemory"]);
                return totalMemoryBytes / (1024 * 1024); // Convert to MB
            }

            return 0;
        }
        catch
        {
            return 8192; // Default 8GB if unable to detect
        }
    }

    private double GetDiskUsage()
    {
        try
        {
            var drive = new DriveInfo("C:");
            if (drive.IsReady)
            {
                var totalSize = drive.TotalSize;
                var freeSpace = drive.TotalFreeSpace;
                var usedSpace = totalSize - freeSpace;
                return (double)usedSpace / totalSize * 100;
            }
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    public void Dispose()
    {
        Stop();
        _monitoringTimer?.Dispose();
        _cpuCounter?.Dispose();
        _ramCounter?.Dispose();
    }
}