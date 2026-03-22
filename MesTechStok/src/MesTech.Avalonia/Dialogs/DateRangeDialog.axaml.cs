using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Dialogs;

public partial class DateRangeDialog : Window
{
    public bool Result { get; private set; }
    public DateTimeOffset? StartDate => StartDatePicker.SelectedDate;
    public DateTimeOffset? EndDate => EndDatePicker.SelectedDate;

    public DateRangeDialog() : this("Tarih Araligi Secin") { }

    public DateRangeDialog(string title = "Tarih Araligi Secin", DateTimeOffset? defaultStart = null, DateTimeOffset? defaultEnd = null)
    {
        InitializeComponent();
        TitleText.Text = title;

        if (defaultStart.HasValue)
            StartDatePicker.SelectedDate = defaultStart.Value;
        if (defaultEnd.HasValue)
            EndDatePicker.SelectedDate = defaultEnd.Value;
    }

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        if (StartDate.HasValue && EndDate.HasValue && StartDate > EndDate)
        {
            ErrorText.Text = "Baslangic tarihi, bitis tarihinden sonra olamaz.";
            ErrorText.IsVisible = true;
            return;
        }

        Result = true;
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }
}
