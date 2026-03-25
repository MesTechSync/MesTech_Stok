using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Dialogs;

public partial class TextAreaDialog : Window
{
    public bool Result { get; private set; }
    public string? InputValue => TextAreaBox.Text;

    public TextAreaDialog() : this(string.Empty, string.Empty) { }

    public TextAreaDialog(string title, string label, string? defaultValue = null)
    {
        InitializeComponent();
        TitleText.Text = title;
        LabelText.Text = label;

        if (defaultValue != null)
            TextAreaBox.Text = defaultValue;
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
