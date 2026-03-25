using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Dialogs;

public enum InputDialogMode
{
    Text,
    Number,
    Password,
    Decimal,
    MultiLine
}

public partial class InputDialog : Window
{
    public bool Result { get; private set; }
    public string? InputValue => InputBox.Text;

    public InputDialog() : this(string.Empty, string.Empty) { }

    public InputDialog(string title, string label, InputDialogMode mode = InputDialogMode.Text, string? defaultValue = null)
    {
        InitializeComponent();
        TitleText.Text = title;
        LabelText.Text = label;

        if (defaultValue != null)
            InputBox.Text = defaultValue;

        switch (mode)
        {
            case InputDialogMode.Password:
                InputBox.PasswordChar = '\u25CF';
                break;
            case InputDialogMode.MultiLine:
                InputBox.AcceptsReturn = true;
                InputBox.Height = 80;
                break;
            case InputDialogMode.Number:
            case InputDialogMode.Decimal:
                InputBox.Watermark = "0";
                break;
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
