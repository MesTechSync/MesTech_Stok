using System;
using System.Windows;
using MesTechStok.Desktop.Models;

namespace MesTechStok.Desktop.Views
{
    public partial class AddStockLotDialog : Window
    {
        public int Quantity { get; private set; }
        public decimal UnitCost { get; private set; }
        public string LotNumber { get; private set; } = string.Empty;
        public DateTime? ExpiryDate { get; private set; }
        public string? Notes { get; private set; }

        public AddStockLotDialog(ProductItem product)
        {
            InitializeComponent();
            TitleText.Text = $"Ürün: {product.Name}";
            CurrentStockText.Text = $"Mevcut Stok: {product.Stock} adet";
            QuantityTextBox.Text = "0";
            UnitCostTextBox.Text = product.Cost.ToString("F2");
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(QuantityTextBox.Text, out var qty) || qty <= 0)
                    throw new Exception("Miktar pozitif olmalı!");
                if (!decimal.TryParse(UnitCostTextBox.Text, out var cost) || cost < 0)
                    throw new Exception("Birim maliyet negatif olamaz!");
                var lot = (LotNumberTextBox.Text ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(lot))
                    throw new Exception("Lot numarası zorunludur!");

                Quantity = qty;
                UnitCost = Math.Round(cost, 4);
                LotNumber = lot;
                ExpiryDate = ExpiryDatePicker.SelectedDate;
                Notes = (NotesTextBox.Text ?? string.Empty).Trim();
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Giriş Hatası", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}


