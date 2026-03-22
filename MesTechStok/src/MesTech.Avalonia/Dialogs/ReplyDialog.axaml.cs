using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Dialogs;

public partial class ReplyDialog : Window
{
    public bool Result { get; private set; }
    public string? ReplyText => ReplyBox.Text;

    public ReplyDialog() : this(string.Empty) { }

    public ReplyDialog(string originalMessage)
    {
        InitializeComponent();
        OriginalMessageText.Text = originalMessage;
    }

    private void OnSend(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ReplyBox.Text)) return;
        Result = true;
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }
}
