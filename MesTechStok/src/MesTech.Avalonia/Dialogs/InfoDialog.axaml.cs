using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace MesTech.Avalonia.Dialogs;

public enum InfoDialogType
{
    Info,
    Success,
    Warning
}

public partial class InfoDialog : Window
{
    private static Color TokenColor(string key) =>
        global::Avalonia.Application.Current?.Resources.TryGetResource(key, null, out var val) == true && val is Color c ? c : Colors.Gray;

    public InfoDialog() : this(string.Empty, string.Empty) { }

    public InfoDialog(string title, string message, InfoDialogType type = InfoDialogType.Info)
    {
        InitializeComponent();
        TitleText.Text = title;
        MessageText.Text = message;

        switch (type)
        {
            case InfoDialogType.Success:
                IconText.Text = "\u2713";
                IconBorder.Background = new SolidColorBrush(TokenColor("MesConnectedGreen"));
                break;
            case InfoDialogType.Warning:
                IconText.Text = "!";
                IconBorder.Background = new SolidColorBrush(TokenColor("MesWarningOrange"));
                break;
            case InfoDialogType.Info:
            default:
                IconText.Text = "i";
                IconBorder.Background = new SolidColorBrush(TokenColor("MesBlueMaterial"));
                break;
        }
    }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }
}
