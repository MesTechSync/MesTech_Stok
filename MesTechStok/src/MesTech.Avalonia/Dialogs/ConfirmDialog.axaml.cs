using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Dialogs;

public partial class ConfirmDialog : Window
{
    public bool Result { get; private set; }

    public ConfirmDialog() : this(string.Empty, string.Empty) { }

    public ConfirmDialog(string title, string message, string confirmText = "Evet", string cancelText = "Iptal", bool isDanger = false)
    {
        InitializeComponent();
        TitleText.Text = title;
        MessageText.Text = message;
        ConfirmBtn.Content = confirmText;
        CancelBtn.Content = cancelText;

        if (isDanger)
        {
            ConfirmBtn.Classes.Remove("mestech-raised");
            ConfirmBtn.Classes.Add("mestech-danger");
        }
    }

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
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
