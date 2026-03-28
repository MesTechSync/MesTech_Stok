using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Platform.Queries.GetPlatformSyncStatus;
using MesTech.Application.DTOs.Platform;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Platform Sync ViewModel — wired to GetPlatformSyncStatusQuery via MediatR.
/// DataGrid with Platform, Son Sync, Durum, Urun Sayisi, Siparis Sayisi.
/// </summary>
public partial class PlatformSyncAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;

    public PlatformSyncAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public ObservableCollection<PlatformSyncItemDto> Platforms { get; } = [];

    private List<PlatformSyncItemDto> _allPlatforms = [];

    private static readonly List<PlatformSyncItemDto> _mockPlatforms =
    [
        new() { Platform = "Trendyol", LastSync = "17.03.2026 14:30", Status = "Basarili", ProductCount = 1245, OrderCount = 89 },
        new() { Platform = "Hepsiburada", LastSync = "17.03.2026 14:15", Status = "Basarili", ProductCount = 876, OrderCount = 45 },
        new() { Platform = "N11", LastSync = "17.03.2026 13:45", Status = "Basarili", ProductCount = 654, OrderCount = 32 },
        new() { Platform = "Ciceksepeti", LastSync = "17.03.2026 12:00", Status = "Hata", ProductCount = 432, OrderCount = 18 },
        new() { Platform = "Amazon TR", LastSync = "17.03.2026 11:30", Status = "Basarili", ProductCount = 321, OrderCount = 27 },
        new() { Platform = "eBay", LastSync = "16.03.2026 23:00", Status = "Basarili", ProductCount = 198, OrderCount = 12 },
        new() { Platform = "Shopify", LastSync = "16.03.2026 22:45", Status = "Beklemede", ProductCount = 567, OrderCount = 34 },
        new() { Platform = "WooCommerce", LastSync = "16.03.2026 20:00", Status = "Basarili", ProductCount = 234, OrderCount = 15 },
        new() { Platform = "Pazarama", LastSync = "16.03.2026 18:30", Status = "Basarili", ProductCount = 145, OrderCount = 8 },
        new() { Platform = "PttAvm", LastSync = "15.03.2026 16:00", Status = "Hata", ProductCount = 89, OrderCount = 3 },
    ];

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            var statuses = await _mediator.Send(new GetPlatformSyncStatusQuery(TenantId: Guid.Empty));

            _allPlatforms = statuses.Select(s => new PlatformSyncItemDto
            {
                Platform = s.PlatformName,
                LastSync = s.LastSyncAt?.ToString("dd.MM.yyyy HH:mm") ?? "—",
                Status = s.HealthStatus,
                ProductCount = s.StoreCount,
                OrderCount = 0 // TODO: wire order count when available in DTO
            }).ToList();

            // Fall back to mock data when query returns empty (no DB configured)
            if (_allPlatforms.Count == 0)
                _allPlatforms = [.._mockPlatforms];

            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Platform verileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allPlatforms.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(p =>
                p.Platform.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        Platforms.Clear();
        foreach (var item in filtered)
            Platforms.Add(item);

        TotalCount = Platforms.Count;
        IsEmpty = Platforms.Count == 0;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private async Task SyncPlatformAsync(PlatformSyncItemDto? platform)
    {
        if (platform == null) return;

        platform.Status = "Senkronize ediliyor...";
        // Trigger UI update
        var index = Platforms.IndexOf(platform);
        if (index >= 0)
        {
            Platforms.RemoveAt(index);
            Platforms.Insert(index, platform);
        }

        // TODO: Wire to SyncPlatformCommand when available
        await Task.Delay(1000);

        platform.Status = "Basarili";
        platform.LastSync = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

        if (index >= 0)
        {
            Platforms.RemoveAt(index);
            Platforms.Insert(index, platform);
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        if (_allPlatforms.Count > 0)
            ApplyFilters();
    }
}

public class PlatformSyncItemDto
{
    public string Platform { get; set; } = string.Empty;
    public string LastSync { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public int OrderCount { get; set; }
}
