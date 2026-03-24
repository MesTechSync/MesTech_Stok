using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.System.Users;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Kullanici Yonetimi ViewModel — veritabanindan gercek kullanici listesi.
/// </summary>
public partial class UserManagementAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<UserItemDto> Users { get; } = [];

    public UserManagementAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _mediator.Send(new GetUsersQuery());

            Users.Clear();
            foreach (var user in result)
            {
                Users.Add(new UserItemDto
                {
                    Username = user.Username,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    Role = user.Role,
                    Status = user.IsActive ? "Aktif" : "Pasif",
                    LastLogin = user.LastLoginDate?.ToString("dd.MM.yyyy HH:mm") ?? "-"
                });
            }

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
