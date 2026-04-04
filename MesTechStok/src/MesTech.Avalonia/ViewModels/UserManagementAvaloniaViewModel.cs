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

    private readonly List<UserItemDto> _allUsers = [];

    public ObservableCollection<UserItemDto> Users { get; } = [];

    public UserManagementAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allUsers
            : _allUsers.Where(u =>
                u.Username.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                u.Email.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                u.Role.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

        Users.Clear();
        foreach (var u in filtered)
            Users.Add(u);

        TotalCount = Users.Count;
        IsEmpty = TotalCount == 0;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new GetUsersQuery(), ct);

            _allUsers.Clear();
            foreach (var user in result)
            {
                _allUsers.Add(new UserItemDto
                {
                    Username = user.Username,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    Role = user.Role,
                    Status = user.IsActive ? "Aktif" : "Pasif",
                    LastLogin = user.LastLoginDate?.ToString("dd.MM.yyyy HH:mm") ?? "-"
                });
            }

            ApplyFilter();
        }, "Kullanici verileri yuklenirken hata");
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
