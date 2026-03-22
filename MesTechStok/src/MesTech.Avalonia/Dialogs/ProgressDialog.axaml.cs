using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Dialogs;

public partial class ProgressDialog : Window
{
    private readonly ObservableCollection<string> _errors = new();

    public ProgressDialog() : this("Islem Devam Ediyor...") { }

    public ProgressDialog(string title = "Islem Devam Ediyor...")
    {
        InitializeComponent();
        TitleText.Text = title;
        ErrorList.ItemsSource = _errors;
    }

    public void UpdateProgress(double value, string status)
    {
        ProgressBarCtrl.Value = value;
        StatusText.Text = status;
    }

    public void AddError(string error)
    {
        _errors.Add(error);
        ErrorList.IsVisible = true;
    }

    public void Complete(string message = "Islem tamamlandi.")
    {
        ProgressBarCtrl.Value = 100;
        StatusText.Text = message;
        CloseBtn.IsEnabled = true;
    }

    private void OnClose(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
