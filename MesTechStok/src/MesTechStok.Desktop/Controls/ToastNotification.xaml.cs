using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MaterialDesignThemes.Wpf;

namespace MesTechStok.Desktop.Controls;

public partial class ToastNotification : UserControl
{
    private readonly System.Timers.Timer _autoCloseTimer;

    public ToastNotification()
    {
        InitializeComponent();
        _autoCloseTimer = new System.Timers.Timer(3000);
        _autoCloseTimer.Elapsed += (_, _) =>
        {
            _autoCloseTimer.Stop();
            Dispatcher.Invoke(Hide);
        };
    }

    public void Show(string title, string message, ToastType type)
    {
        TitleText.Text = title;
        MessageText.Text = message;
        ApplyStyle(type);

        var showAnim = (Storyboard)FindResource("ShowAnimation");
        showAnim.Begin(this);

        _autoCloseTimer.Stop();
        _autoCloseTimer.Start();
    }

    public void Hide()
    {
        _autoCloseTimer.Stop();
        var hideAnim = (Storyboard)FindResource("HideAnimation");
        hideAnim.Completed += (_, _) =>
        {
            if (Parent is Panel panel)
                panel.Children.Remove(this);
        };
        hideAnim.Begin(this);
    }

    private void ApplyStyle(ToastType type)
    {
        (ToastBorder.Background, ToastIcon.Kind) = type switch
        {
            ToastType.Success => ((Brush)new SolidColorBrush(Color.FromRgb(16, 185, 129)), PackIconKind.CheckCircle),
            ToastType.Warning => ((Brush)new SolidColorBrush(Color.FromRgb(245, 158, 11)), PackIconKind.AlertCircle),
            ToastType.Error => ((Brush)new SolidColorBrush(Color.FromRgb(220, 53, 69)), PackIconKind.CloseCircle),
            _ => ((Brush)new SolidColorBrush(Color.FromRgb(40, 85, 172)), PackIconKind.InformationCircle),
        };
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Hide();
}

public enum ToastType
{
    Info,
    Success,
    Warning,
    Error
}
