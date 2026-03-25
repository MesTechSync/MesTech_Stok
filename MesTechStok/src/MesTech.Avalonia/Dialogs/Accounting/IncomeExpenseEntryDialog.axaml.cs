using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Dialogs.Accounting;

public partial class IncomeExpenseEntryDialog : Window
{
    public bool Result { get; private set; }
    public bool IsIncome => RadioIncome.IsChecked == true;
    public string EntryType => IsIncome ? "Gelir" : "Gider";
    public string? Category => (CategoryCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();
    public string? Amount => AmountBox.Text;
    public DateTimeOffset? SelectedDate => EntryDatePicker.SelectedDate;
    public string? Platform => (PlatformCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();
    public string? Description => DescriptionBox.Text;
    public bool IsRecurring => IsRecurringCheck.IsChecked == true;
    public string? RecurrenceInterval => IsRecurring
        ? (RecurrenceCombo.SelectedItem as ComboBoxItem)?.Content?.ToString()
        : null;

    public IncomeExpenseEntryDialog() : this("Gelir / Gider Kaydi") { }

    public IncomeExpenseEntryDialog(string title = "Gelir / Gider Kaydi")
    {
        InitializeComponent();
        TitleText.Text = title;
        EntryDatePicker.SelectedDate = DateTimeOffset.Now;
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
