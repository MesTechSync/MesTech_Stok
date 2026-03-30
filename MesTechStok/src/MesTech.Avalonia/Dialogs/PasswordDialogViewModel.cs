using CommunityToolkit.Mvvm.ComponentModel;

namespace MesTech.Avalonia.Dialogs;

/// <summary>ViewModel for PasswordDialog — password change form.</summary>
public partial class PasswordDialogViewModel : ObservableObject
{
    [ObservableProperty] private string currentPassword = string.Empty;
    [ObservableProperty] private string newPassword = string.Empty;
    [ObservableProperty] private string confirmPassword = string.Empty;
}
