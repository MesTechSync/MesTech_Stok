using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Dialogs;

public partial class TextAreaDialog : Window
{
    public bool Result { get; private set; }
    public string? InputValue => TextAreaBox.Text;

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
}
