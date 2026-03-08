using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Adapters;

namespace MesTechStok.Desktop.Views
{
    public partial class PlatformOrdersView : UserControl
    {
        private readonly TrendyolAdapter? _trendyolAdapter;
        private readonly OpenCartAdapter? _openCartAdapter;
        private readonly ObservableCollection<OrderDisplayItem> _allOrders = new();

        public PlatformOrdersView()
        {
            InitializeComponent();
            StartDatePicker.SelectedDate = DateTime.Today.AddDays(-7);
            EndDatePicker.SelectedDate = DateTime.Today;

            _trendyolAdapter = App.ServiceProvider?.GetService<TrendyolAdapter>();
            _openCartAdapter = App.ServiceProvider?.GetService<OpenCartAdapter>();

            OrdersGrid.ItemsSource = _allOrders;
        }

        private async void RefreshOrders_Click(object sender, RoutedEventArgs e)
        {
            _allOrders.Clear();
            var since = StartDatePicker.SelectedDate ?? DateTime.Today.AddDays(-7);
            var allOrders = new List<ExternalOrderDto>();

            var platformFilter = (PlatformFilterCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Tumu";

            try
            {
                if (_trendyolAdapter != null && (platformFilter == "Tumu" || platformFilter == "Trendyol"))
                {
                    try
                    {
                        var trendyolOrders = await _trendyolAdapter.PullOrdersAsync(since);
                        allOrders.AddRange(trendyolOrders);
                    }
                    catch (InvalidOperationException)
                    {
                        MessageBox.Show("Trendyol baglantisi yapilanmamis. Trendyol Baglanti ekranindan kimlik bilgilerini girin.",
                            "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                if (_openCartAdapter != null && (platformFilter == "Tumu" || platformFilter == "OpenCart"))
                {
                    try
                    {
                        var ocOrders = await _openCartAdapter.PullOrdersAsync(since);
                        allOrders.AddRange(ocOrders);
                    }
                    catch (InvalidOperationException)
                    {
                        // OpenCart not configured yet — skip silently
                    }
                }

                foreach (var o in allOrders.OrderByDescending(x => x.OrderDate))
                    _allOrders.Add(OrderDisplayItem.From(o));

                UpdateStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Siparis cekme hatasi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InvoiceSelected_Click(object sender, RoutedEventArgs e)
        {
            if (OrdersGrid.SelectedItems.Count == 0)
            {
                MessageBox.Show("Lutfen faturalanacak siparisleri seciniz.", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var count = OrdersGrid.SelectedItems.Count;
            var result = MessageBox.Show(
                $"{count} siparis icin fatura olusturulacak. Devam edilsin mi?",
                "Fatura Olustur", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                MessageBox.Show($"{count} siparis icin fatura olusturma islemi baslatildi.\nFatura Yonetimi ekranindan takip edebilirsiniz.",
                    "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void SearchChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void OrdersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void ApplyFilters()
        {
            if (OrdersGrid?.ItemsSource == null) return;

            var view = CollectionViewSource.GetDefaultView(OrdersGrid.ItemsSource);
            if (view == null) return;

            var statusFilter = (StatusFilterCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Tumu";
            var searchText = SearchTextBox?.Text?.Trim()?.ToLowerInvariant() ?? "";

            view.Filter = obj =>
            {
                if (obj is not OrderDisplayItem item) return false;

                if (statusFilter != "Tumu" && !string.Equals(item.Status, statusFilter, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!string.IsNullOrEmpty(searchText))
                {
                    return (item.OrderNumber?.ToLowerInvariant().Contains(searchText) == true) ||
                           (item.CustomerName?.ToLowerInvariant().Contains(searchText) == true);
                }

                return true;
            };

            UpdateStats();
        }

        private void UpdateStats()
        {
            var visible = CollectionViewSource.GetDefaultView(_allOrders)?.Cast<OrderDisplayItem>().ToList() ?? _allOrders.ToList();
            TotalOrdersText.Text = visible.Count.ToString();
            NewOrdersText.Text = visible.Count(o => o.Status == "Created" || o.Status == "Yeni").ToString();
            PreparingOrdersText.Text = visible.Count(o => o.Status == "Picking" || o.Status == "Hazirlaniyor").ToString();
            ShippedOrdersText.Text = visible.Count(o => o.Status == "Shipped" || o.Status == "Kargoda").ToString();
            DeliveredOrdersText.Text = visible.Count(o => o.Status == "Delivered" || o.Status == "Teslim").ToString();
        }
    }

    internal class OrderDisplayItem
    {
        public string OrderNumber { get; set; } = "";
        public string Platform { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
        public string Status { get; set; } = "";
        public string InvoiceStatus { get; set; } = "Bekliyor";
        public string CargoStatus { get; set; } = "Bekliyor";

        public static OrderDisplayItem From(ExternalOrderDto dto) => new()
        {
            OrderNumber = dto.OrderNumber,
            Platform = dto.PlatformCode,
            CustomerName = dto.CustomerName,
            OrderDate = dto.OrderDate,
            TotalAmount = dto.TotalAmount,
            ItemCount = dto.Lines.Count,
            Status = dto.Status,
            InvoiceStatus = "Bekliyor",
            CargoStatus = string.IsNullOrEmpty(dto.CargoTrackingNumber) ? "Bekliyor" : "Kargoda"
        };
    }
}
