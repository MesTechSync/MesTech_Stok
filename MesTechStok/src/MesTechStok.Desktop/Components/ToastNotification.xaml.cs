using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using MesTechStok.Desktop.Views;

namespace MesTechStok.Desktop.Components
{
    /// <summary>
    /// Modern Toast Notification - BRAVO TİMİ Enhanced
    /// Enterprise-grade notification system with animations and theming
    /// </summary>
    public partial class ToastNotification : UserControl
    {
        #region Dependency Properties

        // Toast Title
        public static readonly DependencyProperty ToastTitleProperty =
            DependencyProperty.Register("ToastTitle", typeof(string), typeof(ToastNotification),
                new PropertyMetadata("Bildirim"));

        public string ToastTitle
        {
            get => (string)GetValue(ToastTitleProperty);
            set => SetValue(ToastTitleProperty, value);
        }

        // Toast Message
        public static readonly DependencyProperty ToastMessageProperty =
            DependencyProperty.Register("ToastMessage", typeof(string), typeof(ToastNotification),
                new PropertyMetadata("Bildirim mesajı"));

        public string ToastMessage
        {
            get => (string)GetValue(ToastMessageProperty);
            set => SetValue(ToastMessageProperty, value);
        }

        // Toast Icon
        public static readonly DependencyProperty ToastIconProperty =
            DependencyProperty.Register("ToastIcon", typeof(string), typeof(ToastNotification),
                new PropertyMetadata("ℹ️"));

        public string ToastIcon
        {
            get => (string)GetValue(ToastIconProperty);
            set => SetValue(ToastIconProperty, value);
        }

        // Toast Source
        public static readonly DependencyProperty ToastSourceProperty =
            DependencyProperty.Register("ToastSource", typeof(string), typeof(ToastNotification),
                new PropertyMetadata("Sistem"));

        public string ToastSource
        {
            get => (string)GetValue(ToastSourceProperty);
            set => SetValue(ToastSourceProperty, value);
        }

        // Toast Background
        public static readonly DependencyProperty ToastBackgroundProperty =
            DependencyProperty.Register("ToastBackground", typeof(Brush), typeof(ToastNotification),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(33, 150, 243))));

        public Brush ToastBackground
        {
            get => (Brush)GetValue(ToastBackgroundProperty);
            set => SetValue(ToastBackgroundProperty, value);
        }

        // Toast Border Brush
        public static readonly DependencyProperty ToastBorderBrushProperty =
            DependencyProperty.Register("ToastBorderBrush", typeof(Brush), typeof(ToastNotification),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(25, 118, 210))));

        public Brush ToastBorderBrush
        {
            get => (Brush)GetValue(ToastBorderBrushProperty);
            set => SetValue(ToastBorderBrushProperty, value);
        }

        // Icon Background
        public static readonly DependencyProperty IconBackgroundProperty =
            DependencyProperty.Register("IconBackground", typeof(Brush), typeof(ToastNotification),
                new PropertyMetadata(new SolidColorBrush(Color.FromArgb(60, 255, 255, 255))));

        public Brush IconBackground
        {
            get => (Brush)GetValue(IconBackgroundProperty);
            set => SetValue(IconBackgroundProperty, value);
        }

        // Icon Glow Color
        public static readonly DependencyProperty IconGlowColorProperty =
            DependencyProperty.Register("IconGlowColor", typeof(Color), typeof(ToastNotification),
                new PropertyMetadata(Color.FromRgb(33, 150, 243)));

        public Color IconGlowColor
        {
            get => (Color)GetValue(IconGlowColorProperty);
            set => SetValue(IconGlowColorProperty, value);
        }

        // Show Action Buttons
        public static readonly DependencyProperty ShowActionButtonsProperty =
            DependencyProperty.Register("ShowActionButtons", typeof(bool), typeof(ToastNotification),
                new PropertyMetadata(false));

        public bool ShowActionButtons
        {
            get => (bool)GetValue(ShowActionButtonsProperty);
            set => SetValue(ShowActionButtonsProperty, value);
        }

        // Action Button 1 Text
        public static readonly DependencyProperty ActionButton1TextProperty =
            DependencyProperty.Register("ActionButton1Text", typeof(string), typeof(ToastNotification),
                new PropertyMetadata("Tamam"));

        public string ActionButton1Text
        {
            get => (string)GetValue(ActionButton1TextProperty);
            set => SetValue(ActionButton1TextProperty, value);
        }

        // Action Button 2 Text
        public static readonly DependencyProperty ActionButton2TextProperty =
            DependencyProperty.Register("ActionButton2Text", typeof(string), typeof(ToastNotification),
                new PropertyMetadata("İptal"));

        public string ActionButton2Text
        {
            get => (string)GetValue(ActionButton2TextProperty);
            set => SetValue(ActionButton2TextProperty, value);
        }

        // Show Progress
        public static readonly DependencyProperty ShowProgressProperty =
            DependencyProperty.Register("ShowProgress", typeof(bool), typeof(ToastNotification),
                new PropertyMetadata(true));

        public bool ShowProgress
        {
            get => (bool)GetValue(ShowProgressProperty);
            set => SetValue(ShowProgressProperty, value);
        }

        #endregion

        #region Private Fields

        private readonly DispatcherTimer _autoCloseTimer;
        private Storyboard? _slideInAnimation;
        private Storyboard? _slideOutAnimation;

        #endregion

        #region Events

        public event EventHandler? Closed;
        public Action? OnActionButton1Clicked { get; set; }
        public Action? OnActionButton2Clicked { get; set; }

        #endregion

        public ToastNotification()
        {
            InitializeComponent();
            DataContext = this;
            InitializeAnimations();
            _autoCloseTimer = new DispatcherTimer();
            _autoCloseTimer.Tick += AutoCloseTimer_Tick;
        }

        private void InitializeAnimations()
        {
            _slideInAnimation = FindResource("SlideInAnimation") as Storyboard;
            _slideOutAnimation = FindResource("SlideOutAnimation") as Storyboard;

            if (_slideOutAnimation != null)
            {
                _slideOutAnimation.Completed += (s, e) =>
                {
                    Closed?.Invoke(this, EventArgs.Empty);
                };
            }
        }

        public void ShowToast(string message, string title = "", string source = "", ToastType type = ToastType.Error, int autoCloseSeconds = 5)
        {
            // Set content using BRAVO TİMİ modern dependency properties
            ToastTitle = string.IsNullOrEmpty(title) ? GetDefaultTitle(type) : title;
            ToastMessage = message;
            ToastSource = string.IsNullOrEmpty(source) ?
                $"{DateTime.Now:HH:mm:ss}" :
                $"Kaynak: {source} • {DateTime.Now:HH:mm:ss}";

            // Set modern BRAVO appearance based on type
            SetModernAppearance(type);

            // Show modern animation
            ShowModernAnimation();

            // Auto close timer
            if (autoCloseSeconds > 0)
            {
                _autoCloseTimer.Interval = TimeSpan.FromSeconds(autoCloseSeconds);
                _autoCloseTimer.Start();
            }
        }

        private void SetModernAppearance(ToastType type)
        {
            switch (type)
            {
                case ToastType.Error:
                    ToastIcon = "❌";
                    ToastBackground = new LinearGradientBrush(
                        Color.FromRgb(244, 67, 54),
                        Color.FromRgb(211, 47, 47), 0);
                    ToastBorderBrush = new SolidColorBrush(Color.FromRgb(183, 28, 28));
                    IconBackground = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
                    IconGlowColor = Color.FromRgb(244, 67, 54);
                    break;
                case ToastType.Warning:
                    ToastIcon = "⚠️";
                    ToastBackground = new LinearGradientBrush(
                        Color.FromRgb(255, 152, 0),
                        Color.FromRgb(245, 124, 0), 0);
                    ToastBorderBrush = new SolidColorBrush(Color.FromRgb(230, 108, 0));
                    IconBackground = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
                    IconGlowColor = Color.FromRgb(255, 152, 0);
                    break;
                case ToastType.Success:
                    ToastIcon = "✅";
                    ToastBackground = new LinearGradientBrush(
                        Color.FromRgb(76, 175, 80),
                        Color.FromRgb(56, 142, 60), 0);
                    ToastBorderBrush = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                    IconBackground = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
                    IconGlowColor = Color.FromRgb(76, 175, 80);
                    break;
                case ToastType.Info:
                default:
                    ToastIcon = "ℹ️";
                    ToastBackground = new LinearGradientBrush(
                        Color.FromRgb(33, 150, 243),
                        Color.FromRgb(25, 118, 210), 0);
                    ToastBorderBrush = new SolidColorBrush(Color.FromRgb(21, 101, 192));
                    IconBackground = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
                    IconGlowColor = Color.FromRgb(33, 150, 243);
                    break;
            }
        }

        private string GetDefaultTitle(ToastType type)
        {
            return type switch
            {
                ToastType.Error => "Hata Oluştu",
                ToastType.Warning => "Uyarı",
                ToastType.Success => "Başarılı",
                ToastType.Info => "Bilgi",
                _ => "Bildirim"
            };
        }

        private void ShowModernAnimation()
        {
            // Use BRAVO TİMİ modern storyboard animations
            _slideInAnimation?.Begin();
        }

        private void HideModernAnimation()
        {
            // Use BRAVO TİMİ modern storyboard animations
            _slideOutAnimation?.Begin();
        }

        // Legacy animation methods for backwards compatibility
        private void ShowAnimation()
        {
            ShowModernAnimation();
        }

        private void HideAnimation()
        {
            HideModernAnimation();
        }

        #region Event Handlers

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ActionButton1_Click(object sender, RoutedEventArgs e)
        {
            OnActionButton1Clicked?.Invoke();
            Close();
        }

        private void ActionButton2_Click(object sender, RoutedEventArgs e)
        {
            OnActionButton2Clicked?.Invoke();
            Close();
        }

        private void AutoCloseTimer_Tick(object? sender, EventArgs e)
        {
            _autoCloseTimer.Stop();
            Close();
        }

        #endregion

        #region Public Methods

        public void Close()
        {
            _autoCloseTimer.Stop();
            HideModernAnimation();
        }

        /// <summary>
        /// Show toast with action buttons
        /// </summary>
        public void ShowWithActions(string message, string title, ToastType type,
            string button1Text, string button2Text,
            Action? action1 = null, Action? action2 = null)
        {
            ShowActionButtons = true;
            ActionButton1Text = button1Text;
            ActionButton2Text = button2Text;
            OnActionButton1Clicked = action1;
            OnActionButton2Clicked = action2;

            ShowToast(message, title, "", type, 0); // No auto-close when actions present
        }

        /// <summary>
        /// Set custom auto-hide delay
        /// </summary>
        public void SetAutoHideDelay(TimeSpan delay)
        {
            _autoCloseTimer.Interval = delay;
        }

        #endregion
    }

    public enum ToastType
    {
        Error,
        Warning,
        Success,
        Info
    }

    // Global Toast Manager
    public static class ToastManager
    {
        private static Panel? _toastContainer;

        public static void Initialize(Panel container)
        {
            _toastContainer = container;
        }

        public static void ShowToast(string message, string title = "", string source = "", ToastType type = ToastType.Error, int autoCloseSeconds = 5)
        {
            if (_toastContainer == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                var toast = new ToastNotification();

                // StackPanel için basit positioning - otomatik vertical stacking
                toast.HorizontalAlignment = HorizontalAlignment.Right;
                toast.VerticalAlignment = VerticalAlignment.Top;
                toast.Margin = new Thickness(0, 0, 0, 10); // Sadece alt margin

                toast.Closed += (s, e) =>
                {
                    _toastContainer.Children.Remove(toast);
                    // StackPanel otomatik olarak yeniden düzenler, manuel reorder gerekmez
                };

                // StackPanel'a ekle - en alta eklenecek
                _toastContainer.Children.Add(toast);
                toast.ShowToast(message, title, source, type, autoCloseSeconds);

                // Auto-log to LogView
                GlobalLogger.Instance.LogError($"Toast: {message}", source);
            });
        }

        public static void ShowError(string message, string source = "")
        {
            ShowToast(message, "Hata", source, ToastType.Error);
        }

        public static void ShowWarning(string message, string source = "")
        {
            ShowToast(message, "Uyarı", source, ToastType.Warning);
        }

        public static void ShowSuccess(string message, string source = "")
        {
            ShowToast(message, "Başarılı", source, ToastType.Success);
        }

        public static void ShowInfo(string message, string source = "")
        {
            ShowToast(message, "Bilgi", source, ToastType.Info);
        }

        private static void ReorderToastsFromBottom()
        {
            // StackPanel otomatik olarak vertical stacking yapar, manuel reorder gerekmez
            // Bu fonksiyon artık gerekli değil ama mevcut çağrılar için boş bırakıyoruz
        }
    }
}