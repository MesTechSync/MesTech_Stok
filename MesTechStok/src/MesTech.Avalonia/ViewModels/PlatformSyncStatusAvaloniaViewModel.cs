using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Platform.Queries.GetPlatformSyncStatus;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Platform Senkronizasyon Durumu ViewModel — platform bazli sync durumu + saglik gostergesi.
/// </summary>
public partial class PlatformSyncStatusAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private int totalCount;

    public ObservableCollection<PlatformSyncStatusItemDto> Platforms { get; } = [];

    public PlatformSyncStatusAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _mediator.Send(new GetPlatformSyncStatusQuery(_currentUser.TenantId));

            Platforms.Clear();
            foreach (var dto in result)
            {
                Platforms.Add(new PlatformSyncStatusItemDto
                {
                    Platform = dto.PlatformName,
                    PlatformColor = dto.HealthColor,
                    StoreCount = dto.StoreCount,
                    LastSync = dto.LastSyncAt.HasValue ? dto.LastSyncAt.Value.ToString("dd.MM.yyyy HH:mm") : "-",
                    LastSuccess = dto.LastSuccessAt.HasValue ? dto.LastSuccessAt.Value.ToString("dd.MM.yyyy HH:mm") : "-",
                    ErrorsToday = dto.ErrorCountToday,
                    HealthStatus = dto.HealthStatus
                });
            }

            TotalCount = Platforms.Count;
            IsEmpty = TotalCount == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Platform senkronizasyon durumu yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private Task SyncPlatformAsync(PlatformSyncStatusItemDto? platform)
    {
        if (platform is null || platform.HealthStatus == "Pasif") return Task.CompletedTask;

        platform.HealthStatus = "Senkronize ediliyor...";
        var index = Platforms.IndexOf(platform);
        if (index >= 0) { Platforms.RemoveAt(index); Platforms.Insert(index, platform); }

        platform.HealthStatus = "Saglikli";
        platform.LastSync = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
        platform.LastSuccess = platform.LastSync;
        platform.ErrorsToday = 0;
        if (index >= 0) { Platforms.RemoveAt(index); Platforms.Insert(index, platform); }
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task Refresh() => LoadAsync();
}

public class PlatformSyncStatusItemDto
{
    public string Platform { get; set; } = string.Empty;
    public string PlatformColor { get; set; } = "#0078D4";
    public int StoreCount { get; set; }
    public string LastSync { get; set; } = string.Empty;
    public string LastSuccess { get; set; } = string.Empty;
    public int ErrorsToday { get; set; }
    public string HealthStatus { get; set; } = string.Empty;

    public string HealthColor => HealthStatus switch
    {
        "Saglikli" => "#4CAF50",
        "Uyari" => "#FF9800",
        "Hatali" => "#F44336",
        "Pasif" => "#9E9E9E",
        _ => "#64748B"
    };
}
