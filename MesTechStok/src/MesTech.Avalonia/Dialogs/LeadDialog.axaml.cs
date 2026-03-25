using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Dialogs;

public partial class LeadDialog : Window
{
    public bool Result { get; private set; }
    public string? LeadName => NameBox.Text;
    public string? Email => EmailBox.Text;
    public string? Phone => PhoneBox.Text;
    public string? Source => (SourceCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();
    public string? Note => NoteBox.Text;

    public LeadDialog() : this("Yeni Musteri Adayi") { }

    public LeadDialog(string title = "Yeni Musteri Adayi",
                      string? name = null,
                      string? email = null,
                      string? phone = null)
    {
        InitializeComponent();
        TitleText.Text = title;

        if (name != null) NameBox.Text = name;
        if (email != null) EmailBox.Text = email;
        if (phone != null) PhoneBox.Text = phone;
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text)) return;
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
