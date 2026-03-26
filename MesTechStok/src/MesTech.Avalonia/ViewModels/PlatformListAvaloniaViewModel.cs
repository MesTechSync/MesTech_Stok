using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Platform.Queries.GetPlatformList;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Platform Listesi ViewModel — 13 platform karti ile pazaryeri yonetimi.
/// </summary>
public partial class PlatformListAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private int totalCount;

    public ObservableCollection<PlatformCardDto> Platforms { get; } = [];

    public PlatformListAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
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
            var result = await _mediator.Send(new GetPlatformListQuery(_currentUser.TenantId));

            Platforms.Clear();
            foreach (var dto in result)
            {
                Platforms.Add(new PlatformCardDto
                {
                    Name = dto.Name,
                    Color = dto.LogoColor,
                    StoreCount = dto.StoreCount,
                    IsActive = dto.ActiveStoreCount > 0 || dto.AdapterAvailable
                });
            }

            TotalCount = Platforms.Count;
            IsEmpty = TotalCount == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Platform listesi yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class PlatformCardDto
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#0078D4";
    public int StoreCount { get; set; }
    public bool IsActive { get; set; }
    public string StatusText => IsActive ? "Aktif" : "Pasif";
}
