using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Dialogs;

public partial class PasswordDialog : Window
{
    public bool Result { get; private set; }
    public string? OldPassword => OldPasswordBox.Text;
    public string? NewPassword => NewPasswordBox.Text;

    public PasswordDialog() : this("Sifre Degistir") { }

    public PasswordDialog(string title = "Sifre Degistir")
    {
        InitializeComponent();
        TitleText.Text = title;
    }

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NewPasswordBox.Text))
        {
            ErrorText.Text = "Yeni sifre bos olamaz.";
            ErrorText.IsVisible = true;
            return;
        }

        if (NewPasswordBox.Text != ConfirmPasswordBox.Text)
        {
            ErrorText.Text = "Yeni sifreler eslesmedi.";
            ErrorText.IsVisible = true;
            return;
        }

        Result = true;
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Result = false;
            Close();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }
}
