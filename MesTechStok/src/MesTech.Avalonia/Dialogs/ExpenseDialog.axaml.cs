using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Dialogs;

public partial class ExpenseDialog : Window
{
    public bool Result { get; private set; }
    public string? Category => (CategoryCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();
    public string? Amount => AmountBox.Text;
    public DateTimeOffset? SelectedDate => DatePicker.SelectedDate;
    public string? Description => DescriptionBox.Text;
    public string? DocumentNo => DocumentNoBox.Text;

    public ExpenseDialog() : this("Gider Kaydi") { }

    public ExpenseDialog(string title = "Gider Kaydi")
    {
        InitializeComponent();
        TitleText.Text = title;
        DatePicker.SelectedDate = DateTimeOffset.Now;
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(AmountBox.Text)) return;
        Result = true;
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }
}
