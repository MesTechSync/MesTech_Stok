using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MesTechStok.Desktop.Views.Notifications
{
    public partial class NotificationDashboardView : UserControl
    {
        private static readonly SolidColorBrush _whatsAppBrush;
        private static readonly SolidColorBrush _telegramBrush;
        private static readonly SolidColorBrush _emailBrush;
        private static readonly SolidColorBrush _pushBrush;
        private static readonly SolidColorBrush _smsBrush;

        // Status background brushes
        private static readonly SolidColorBrush _pendingBg;
        private static readonly SolidColorBrush _pendingFg;
        private static readonly SolidColorBrush _sentBg;
        private static readonly SolidColorBrush _sentFg;
        private static readonly SolidColorBrush _deliveredBg;
        private static readonly SolidColorBrush _deliveredFg;
        private static readonly SolidColorBrush _failedBg;
        private static readonly SolidColorBrush _failedFg;
        private static readonly SolidColorBrush _readBg;
        private static readonly SolidColorBrush _readFg;

        static NotificationDashboardView()
        {
            _whatsAppBrush = new SolidColorBrush(Color.FromRgb(0x25, 0xD3, 0x66));
            _whatsAppBrush.Freeze();
            _telegramBrush = new SolidColorBrush(Color.FromRgb(0x24, 0x9F, 0xDB));
            _telegramBrush.Freeze();
            _emailBrush = new SolidColorBrush(Color.FromRgb(0x6C, 0x75, 0x7D));
            _emailBrush.Freeze();
            _pushBrush = new SolidColorBrush(Color.FromRgb(0x8B, 0x5C, 0xF6));
            _pushBrush.Freeze();
            _smsBrush = new SolidColorBrush(Color.FromRgb(0xF9, 0x7F, 0x16));
            _smsBrush.Freeze();

            // Pending = yellow
            _pendingBg = new SolidColorBrush(Color.FromRgb(0xFE, 0xF3, 0xC7));
            _pendingBg.Freeze();
            _pendingFg = new SolidColorBrush(Color.FromRgb(0x92, 0x40, 0x0E));
            _pendingFg.Freeze();

            // Sent = blue
            _sentBg = new SolidColorBrush(Color.FromRgb(0xDB, 0xEA, 0xFE));
            _sentBg.Freeze();
            _sentFg = new SolidColorBrush(Color.FromRgb(0x1E, 0x40, 0xAF));
            _sentFg.Freeze();

            // Delivered = green
            _deliveredBg = new SolidColorBrush(Color.FromRgb(0xD1, 0xFA, 0xE5));
            _deliveredBg.Freeze();
            _deliveredFg = new SolidColorBrush(Color.FromRgb(0x06, 0x5F, 0x46));
            _deliveredFg.Freeze();

            // Failed = red
            _failedBg = new SolidColorBrush(Color.FromRgb(0xFE, 0xE2, 0xE2));
            _failedBg.Freeze();
            _failedFg = new SolidColorBrush(Color.FromRgb(0x99, 0x1B, 0x1B));
            _failedFg.Freeze();

            // Read = gray
            _readBg = new SolidColorBrush(Color.FromRgb(0xF3, 0xF4, 0xF6));
            _readBg.Freeze();
            _readFg = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80));
            _readFg.Freeze();
        }

        private readonly ObservableCollection<NotificationItem> _notifications = new();
        private List<NotificationItem> _allNotifications = new();

        public NotificationDashboardView()
        {
            InitializeComponent();
            NotificationGrid.ItemsSource = _notifications;
            LoadMockData();
        }

        private void LoadMockData()
        {
            _allNotifications = new List<NotificationItem>
            {
                new() { Id = Guid.NewGuid(), Channel = "WhatsApp", Recipient = "+90 532 111 2233", TemplateName = "siparis_onay", Content = "Siparis #TY-001 onaylandi. Kargoya verilecektir.", Status = "Delivered", SentAt = DateTime.Now.AddHours(-1), DeliveredAt = DateTime.Now.AddMinutes(-58), ErrorMessage = "" },
                new() { Id = Guid.NewGuid(), Channel = "Telegram", Recipient = "@ahmet_yilmaz", TemplateName = "stok_uyari", Content = "Urun X stok seviyesi kritik: 3 adet kaldi.", Status = "Sent", SentAt = DateTime.Now.AddHours(-2), ErrorMessage = "" },
                new() { Id = Guid.NewGuid(), Channel = "Email", Recipient = "fatma@firma.com", TemplateName = "fatura_bilgi", Content = "Fatura #INV-2026-0315 basariyla olusturuldu.", Status = "Delivered", SentAt = DateTime.Now.AddHours(-3), DeliveredAt = DateTime.Now.AddHours(-3).AddMinutes(2), ErrorMessage = "" },
                new() { Id = Guid.NewGuid(), Channel = "Push", Recipient = "device:abc123", TemplateName = "kampanya", Content = "Yeni kampanya: Tum urunlerde %20 indirim!", Status = "Pending", SentAt = DateTime.Now.AddMinutes(-30), ErrorMessage = "" },
                new() { Id = Guid.NewGuid(), Channel = "SMS", Recipient = "+90 544 555 6677", TemplateName = "kargo_takip", Content = "Kargonuz yola cikti. Takip: YK-778293", Status = "Delivered", SentAt = DateTime.Now.AddHours(-4), DeliveredAt = DateTime.Now.AddHours(-4).AddMinutes(1), ErrorMessage = "" },
                new() { Id = Guid.NewGuid(), Channel = "WhatsApp", Recipient = "+90 555 888 9900", TemplateName = "siparis_onay", Content = "Siparis #HB-002 onaylandi.", Status = "Failed", SentAt = DateTime.Now.AddHours(-5), ErrorMessage = "Numara WhatsApp'a kayitli degil" },
                new() { Id = Guid.NewGuid(), Channel = "Email", Recipient = "mehmet@demir.com", TemplateName = "odeme_hatirlatma", Content = "Odeme vadesine 3 gun kaldi. Tutar: 2.400 TL", Status = "Read", SentAt = DateTime.Now.AddDays(-1), DeliveredAt = DateTime.Now.AddDays(-1).AddMinutes(1), ReadAt = DateTime.Now.AddHours(-6), ErrorMessage = "" },
                new() { Id = Guid.NewGuid(), Channel = "Telegram", Recipient = "@stok_bot", TemplateName = "gunluk_ozet", Content = "Gunluk ozet: 45 siparis, 12 kargo, 3 iade.", Status = "Delivered", SentAt = DateTime.Now.AddDays(-1).AddHours(-2), DeliveredAt = DateTime.Now.AddDays(-1).AddHours(-2).AddSeconds(5), ErrorMessage = "" },
                new() { Id = Guid.NewGuid(), Channel = "SMS", Recipient = "+90 212 333 4455", TemplateName = "randevu_hatirlatma", Content = "Yarin saat 10:00'da randevunuz bulunmaktadir.", Status = "Sent", SentAt = DateTime.Now.AddDays(-1).AddHours(-4), ErrorMessage = "" },
                new() { Id = Guid.NewGuid(), Channel = "Push", Recipient = "device:xyz789", TemplateName = "yeni_urun", Content = "Yeni urun eklendi: Premium Kulaklik", Status = "Failed", SentAt = DateTime.Now.AddDays(-2), ErrorMessage = "Device token expired" },
                new() { Id = Guid.NewGuid(), Channel = "WhatsApp", Recipient = "+90 532 777 8899", TemplateName = "iade_bilgi", Content = "Iade talebiniz #RET-045 onaylandi.", Status = "Delivered", SentAt = DateTime.Now.AddDays(-2).AddHours(-3), DeliveredAt = DateTime.Now.AddDays(-2).AddHours(-3).AddMinutes(1), ErrorMessage = "" },
                new() { Id = Guid.NewGuid(), Channel = "Email", Recipient = "zeynep@arslan.net", TemplateName = "hosgeldin", Content = "MesTech'e hosgeldiniz! Hesabiniz basariyla olusturuldu.", Status = "Read", SentAt = DateTime.Now.AddDays(-3), DeliveredAt = DateTime.Now.AddDays(-3).AddMinutes(2), ReadAt = DateTime.Now.AddDays(-2), ErrorMessage = "" },
                new() { Id = Guid.NewGuid(), Channel = "Telegram", Recipient = "@finans_grubu", TemplateName = "odeme_bilgi", Content = "Trendyol hakedisiniz yatti: 45.200 TL", Status = "Delivered", SentAt = DateTime.Now.AddDays(-3).AddHours(-5), DeliveredAt = DateTime.Now.AddDays(-3).AddHours(-5).AddSeconds(3), ErrorMessage = "" },
                new() { Id = Guid.NewGuid(), Channel = "SMS", Recipient = "+90 312 444 5566", TemplateName = "dogrulama", Content = "Dogrulama kodunuz: 482917", Status = "Delivered", SentAt = DateTime.Now.AddDays(-4), DeliveredAt = DateTime.Now.AddDays(-4).AddSeconds(8), ErrorMessage = "" },
                new() { Id = Guid.NewGuid(), Channel = "WhatsApp", Recipient = "+90 216 999 0011", TemplateName = "teslimat_bilgi", Content = "Kargonuz teslim edildi. Iyi gunlerde kullanin!", Status = "Read", SentAt = DateTime.Now.AddDays(-4).AddHours(-6), DeliveredAt = DateTime.Now.AddDays(-4).AddHours(-6).AddMinutes(1), ReadAt = DateTime.Now.AddDays(-4), ErrorMessage = "" }
            };

            ApplyChannelAndStatusBrushes();
            ApplyFilters();
        }

        private void ApplyChannelAndStatusBrushes()
        {
            foreach (var item in _allNotifications)
            {
                item.ChannelColor = item.Channel switch
                {
                    "WhatsApp" => _whatsAppBrush,
                    "Telegram" => _telegramBrush,
                    "Email" => _emailBrush,
                    "Push" => _pushBrush,
                    "SMS" => _smsBrush,
                    _ => _emailBrush
                };

                (item.StatusBackground, item.StatusForeground) = item.Status switch
                {
                    "Pending" => (_pendingBg, _pendingFg),
                    "Sent" => (_sentBg, _sentFg),
                    "Delivered" => (_deliveredBg, _deliveredFg),
                    "Failed" => (_failedBg, _failedFg),
                    "Read" => (_readBg, _readFg),
                    _ => (_readBg, _readFg)
                };

                item.MarkReadVisibility = item.Status is "Delivered" or "Sent"
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void ApplyFilters()
        {
            var channelFilter = (ChannelFilter?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Tumu";
            var statusFilter = (StatusFilter?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Tumu";
            var startDate = StartDatePicker?.SelectedDate;
            var endDate = EndDatePicker?.SelectedDate;

            var filtered = _allNotifications.AsEnumerable();

            if (channelFilter != "Tumu")
                filtered = filtered.Where(n => n.Channel == channelFilter);

            if (statusFilter != "Tumu")
                filtered = filtered.Where(n => n.Status == statusFilter);

            if (startDate.HasValue)
                filtered = filtered.Where(n => n.SentAt >= startDate.Value);

            if (endDate.HasValue)
                filtered = filtered.Where(n => n.SentAt <= endDate.Value.AddDays(1));

            var result = filtered.OrderByDescending(n => n.SentAt).ToList();

            _notifications.Clear();
            foreach (var item in result)
                _notifications.Add(item);

            UpdateKpis();
        }

        private void UpdateKpis()
        {
            var all = _allNotifications;
            TotalCountText.Text = all.Count.ToString();
            UnreadCountText.Text = all.Count(n => n.Status is "Pending" or "Sent" or "Delivered").ToString();
            SuccessCountText.Text = all.Count(n => n.Status is "Delivered" or "Read").ToString();
            FailedCountText.Text = all.Count(n => n.Status == "Failed").ToString();
        }

        private void ChannelFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void StatusFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void DateFilter_Changed(object? sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void MarkRead_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Guid id)
            {
                var item = _allNotifications.FirstOrDefault(n => n.Id == id);
                if (item != null)
                {
                    item.Status = "Read";
                    item.ReadAt = DateTime.Now;
                    item.StatusBackground = _readBg;
                    item.StatusForeground = _readFg;
                    item.MarkReadVisibility = Visibility.Collapsed;
                    ApplyFilters();
                }
            }
        }

        private void Detail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Guid id)
            {
                var item = _allNotifications.FirstOrDefault(n => n.Id == id);
                if (item != null)
                {
                    var details = $"Kanal: {item.Channel}\n"
                                + $"Alici: {item.Recipient}\n"
                                + $"Sablon: {item.TemplateName}\n"
                                + $"Icerik: {item.Content}\n"
                                + $"Durum: {item.Status}\n"
                                + $"Gonderim: {item.SentAtText}\n"
                                + (item.DeliveredAt.HasValue ? $"Teslim: {item.DeliveredAt:dd.MM.yyyy HH:mm}\n" : "")
                                + (item.ReadAt.HasValue ? $"Okunma: {item.ReadAt:dd.MM.yyyy HH:mm}\n" : "")
                                + (!string.IsNullOrEmpty(item.ErrorMessage) ? $"Hata: {item.ErrorMessage}" : "");

                    MessageBox.Show(details, "Bildirim Detayi", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void NewNotification_Click(object sender, RoutedEventArgs e)
        {
            // Intentional: placeholder for new notification dialog
            // Real implementation will use SendNotificationCommand via CQRS
            MessageBox.Show(
                "Yeni bildirim gonderme formu Dalga 12 Wave 5'te aktif olacaktir.\n\n"
                + "CQRS Handler: SendNotificationCommand(TenantId, Channel, Recipient, TemplateName, Content)",
                "Yeni Bildirim",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadMockData();
        }
    }

    internal sealed class NotificationItem
    {
        public Guid Id { get; set; }
        public string Channel { get; set; } = "";
        public string Recipient { get; set; } = "";
        public string TemplateName { get; set; } = "";
        public string Content { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime SentAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public string ErrorMessage { get; set; } = "";

        // UI helpers
        public string SentAtText => SentAt.ToString("dd.MM.yyyy HH:mm");
        public string ContentPreview => Content.Length > 60 ? Content[..57] + "..." : Content;
        public SolidColorBrush ChannelColor { get; set; } = Brushes.Gray;
        public SolidColorBrush StatusBackground { get; set; } = Brushes.LightGray;
        public SolidColorBrush StatusForeground { get; set; } = Brushes.Black;
        public Visibility MarkReadVisibility { get; set; } = Visibility.Collapsed;
    }
}
