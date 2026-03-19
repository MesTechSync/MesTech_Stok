using Avalonia.Controls;
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
    public InfoDialog(string title, string message, InfoDialogType type = InfoDialogType.Info)
    {
        InitializeComponent();
        TitleText.Text = title;
        MessageText.Text = message;

        switch (type)
        {
            case InfoDialogType.Success:
                IconText.Text = "\u2713";
                IconBorder.Background = new SolidColorBrush(Color.Parse("#4CAF50"));
                break;
            case InfoDialogType.Warning:
                IconText.Text = "!";
                IconBorder.Background = new SolidColorBrush(Color.Parse("#FF9800"));
                break;
            case InfoDialogType.Info:
            default:
                IconText.Text = "i";
                IconBorder.Background = new SolidColorBrush(Color.Parse("#2196F3"));
                break;
        }
    }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
