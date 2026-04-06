using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Commands.SyncPlatform;
using MesTech.Application.Features.Platform.Queries.GetPlatformSyncStatus;
using MesTech.Application.DTOs.Platform;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Platform Sync ViewModel — wired to GetPlatformSyncStatusQuery via MediatR.
/// DataGrid with Platform, Son Sync, Durum, Urun Sayisi, Siparis Sayisi.
/// </summary>
public partial class PlatformSyncAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;

    public PlatformSyncAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public ObservableCollection<PlatformSyncItemDto> Platforms { get; } = [];

    private List<PlatformSyncItemDto> _allPlatforms = [];

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var statuses = await _mediator.Send(new GetPlatformSyncStatusQuery(TenantId: _currentUser.TenantId), ct);

            _allPlatforms = statuses.Select(s => new PlatformSyncItemDto
            {
                Platform = s.PlatformName,
                LastSync = s.LastSyncAt?.ToString("dd.MM.yyyy HH:mm") ?? "—",
                Status = s.HealthStatus,
                ProductCount = s.StoreCount,
                OrderCount = 0 // DEP: DTO field eksik — wire order count when available in DTO
            }).ToList();

            ApplyFilters();
        }, "Platform verileri yuklenirken hata");
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

        try
        {
            var result = await _mediator.Send(new SyncPlatformCommand(
                platform.Platform, MesTech.Domain.Enums.SyncDirection.Bidirectional));
            platform.Status = result.IsSuccess ? "Basarili" : $"Hata: {result.ErrorMessage}";
        }
        catch (Exception ex)
        {
            platform.Status = $"Hata: {ex.Message}";
        }
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
