using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Kullanici Yonetimi ViewModel — kullanici listesi + rol yonetimi.
/// EMR-12: Enhanced from placeholder to functional view.
/// </summary>
public partial class UserManagementAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<UserItemDto> Users { get; } = [];

    public UserManagementAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(200); // Will be replaced with MediatR query

            Users.Clear();
            Users.Add(new UserItemDto { Username = "admin", FullName = "Sistem Yoneticisi", Email = "admin@mestech.com", Role = "Admin", Status = "Aktif", LastLogin = "19.03.2026 14:30" });
            Users.Add(new UserItemDto { Username = "operator1", FullName = "Ali Veli", Email = "ali@mestech.com", Role = "Operator", Status = "Aktif", LastLogin = "19.03.2026 09:15" });
            Users.Add(new UserItemDto { Username = "depocu1", FullName = "Mehmet Demir", Email = "mehmet@mestech.com", Role = "Depo", Status = "Aktif", LastLogin = "18.03.2026 17:00" });
            Users.Add(new UserItemDto { Username = "muhasebe1", FullName = "Fatma Ozturk", Email = "fatma@mestech.com", Role = "Muhasebe", Status = "Pasif", LastLogin = "15.03.2026 10:00" });

            TotalCount = Users.Count;
            IsEmpty = TotalCount == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Kullanicilar yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class UserItemDto
{
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string LastLogin { get; set; } = string.Empty;
}
