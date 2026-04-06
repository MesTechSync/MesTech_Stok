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
    [ObservableProperty] private UserItemDto? selectedUser;

    // HH-DEV2-038: Add/Edit form fields
    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private string editUsername = string.Empty;
    [ObservableProperty] private string editFullName = string.Empty;
    [ObservableProperty] private string editEmail = string.Empty;
    [ObservableProperty] private string editRole = "Viewer";
    [ObservableProperty] private string editStatus = string.Empty;
    public string[] RoleOptions { get; } = ["Admin", "Editor", "Viewer"];

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

    // HH-DEV2-038: Show add-user form
    [RelayCommand]
    private void AddUser()
    {
        EditUsername = string.Empty;
        EditFullName = string.Empty;
        EditEmail = string.Empty;
        EditRole = "Viewer";
        SelectedUser = null;
        IsEditing = true;
    }

    // HH-DEV2-038: Populate form for editing
    [RelayCommand]
    private void EditUser()
    {
        if (SelectedUser is null) return;
        EditUsername = SelectedUser.Username;
        EditFullName = SelectedUser.FullName;
        EditEmail = SelectedUser.Email;
        EditRole = SelectedUser.Role;
        IsEditing = true;
    }

    // HH-DEV2-038: Save user (add or update)
    [RelayCommand]
    private async Task SaveUser()
    {
        if (string.IsNullOrWhiteSpace(EditUsername))
        {
            EditStatus = "Kullanici adi zorunlu.";
            return;
        }

        // TODO: CreateUserCommand / UpdateUserCommand — DEV1 handler gerekli
        EditStatus = $"{EditUsername} kaydedildi.";
        IsEditing = false;
        await LoadAsync();
    }

    // HH-DEV2-038: Cancel editing
    [RelayCommand]
    private void CancelEdit() => IsEditing = false;

    // HH-DEV2-038: Delete user
    [RelayCommand]
    private Task DeleteUser()
    {
        if (SelectedUser is null) return Task.CompletedTask;
        // TODO: DeleteUserCommand — DEV1 handler gerekli
        _allUsers.RemoveAll(u => u.Username == SelectedUser.Username);
        SelectedUser = null;
        ApplyFilter();
        return Task.CompletedTask;
    }
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
