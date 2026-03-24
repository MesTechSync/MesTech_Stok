using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace MesTech.Avalonia.Dialogs.Fulfillment;

public partial class CreateInboundDialog : Window
{
    private int _currentStep = 1;
    public bool Result { get; private set; }

    public string SelectedProvider => (ProviderCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Amazon FBA";
    public string ShipmentNote => ShipmentNoteBox.Text ?? string.Empty;
    public ObservableCollection<InboundProductItem> Products { get; } = [];

    public CreateInboundDialog()
    {
        InitializeComponent();
        ProductGrid.ItemsSource = Products;
    }

    private void UpdateStepVisibility()
    {
        Step1Panel.IsVisible = _currentStep == 1;
        Step2Panel.IsVisible = _currentStep == 2;
        Step3Panel.IsVisible = _currentStep == 3;
        BackButton.IsVisible = _currentStep > 1;
        NextButton.Content = _currentStep == 3 ? "Olustur" : "Ileri";

        // Update step indicators
        UpdateIndicator(Step1Indicator, _currentStep >= 1);
        UpdateIndicator(Step2Indicator, _currentStep >= 2);
        UpdateIndicator(Step3Indicator, _currentStep >= 3);

        // Populate summary on step 3
        if (_currentStep == 3)
        {
            SummaryProvider.Text = SelectedProvider;
            SummaryProductCount.Text = Products.Count.ToString();
            SummaryTotalQty.Text = Products.Sum(p => p.Quantity).ToString();
            SummaryNote.Text = string.IsNullOrWhiteSpace(ShipmentNote) ? "-" : ShipmentNote;
        }
    }

    private void UpdateIndicator(Border indicator, bool active)
    {
        if (active)
        {
            indicator.Background = this.FindResource("MesPrimaryBlue") as IBrush ?? Brushes.DodgerBlue;
        }
        else
        {
            indicator.Background = this.FindResource("MesBorderMedium") as IBrush ?? Brushes.Gray;
        }
    }

    private void OnAddProduct(object? sender, RoutedEventArgs e)
    {
        var sku = SkuInput.Text?.Trim();
        if (string.IsNullOrWhiteSpace(sku)) return;

        var qty = (int)(QtyInput.Value ?? 1);
        var existing = Products.FirstOrDefault(p => p.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            existing.Quantity += qty;
            // Force refresh
            var idx = Products.IndexOf(existing);
            Products.RemoveAt(idx);
            Products.Insert(idx, existing);
        }
        else
        {
            Products.Add(new InboundProductItem { Sku = sku, Quantity = qty });
        }

        ProductCountText.Text = $"{Products.Count} urun eklendi";
        SkuInput.Text = string.Empty;
        QtyInput.Value = 1;
        SkuInput.Focus();
    }

    private void OnNext(object? sender, RoutedEventArgs e)
    {
        if (_currentStep == 2 && Products.Count == 0)
            return; // Cannot proceed without products

        if (_currentStep == 3)
        {
            // Confirm and close
            Result = true;
            Close();
            return;
        }

        _currentStep++;
        UpdateStepVisibility();
    }

    private void OnBack(object? sender, RoutedEventArgs e)
    {
        if (_currentStep > 1)
        {
            _currentStep--;
            UpdateStepVisibility();
        }
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }
}

public class InboundProductItem
{
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
