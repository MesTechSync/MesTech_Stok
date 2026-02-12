using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MesTechStok.Desktop.Components
{
    /// <summary>
    /// KPI Widget Component - BRAVO TİMİ
    /// Dashboard için modern KPI gösterge widget'ı
    /// </summary>
    public partial class KPIWidget : UserControl
    {
        #region Dependency Properties

        // Title Property
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(KPIWidget),
                new PropertyMetadata("KPI Başlığı"));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        // Value Property
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(string), typeof(KPIWidget),
                new PropertyMetadata("0", OnValueChanged));

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        // Icon Property
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(string), typeof(KPIWidget),
                new PropertyMetadata("📊"));

        public string Icon
        {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        // Description Property
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(string), typeof(KPIWidget),
                new PropertyMetadata(string.Empty));

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        // Background Color Property
        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register("BackgroundColor", typeof(Brush), typeof(KPIWidget),
                new PropertyMetadata(Brushes.White));

        public Brush BackgroundColor
        {
            get => (Brush)GetValue(BackgroundColorProperty);
            set => SetValue(BackgroundColorProperty, value);
        }

        // Show Trend Property
        public static readonly DependencyProperty ShowTrendProperty =
            DependencyProperty.Register("ShowTrend", typeof(bool), typeof(KPIWidget),
                new PropertyMetadata(false));

        public bool ShowTrend
        {
            get => (bool)GetValue(ShowTrendProperty);
            set => SetValue(ShowTrendProperty, value);
        }

        // Trend Text Property
        public static readonly DependencyProperty TrendTextProperty =
            DependencyProperty.Register("TrendText", typeof(string), typeof(KPIWidget),
                new PropertyMetadata("+12%"));

        public string TrendText
        {
            get => (string)GetValue(TrendTextProperty);
            set => SetValue(TrendTextProperty, value);
        }

        // Is Positive Trend Property
        public static readonly DependencyProperty IsPositiveTrendProperty =
            DependencyProperty.Register("IsPositiveTrend", typeof(bool), typeof(KPIWidget),
                new PropertyMetadata(true, OnTrendChanged));

        public bool IsPositiveTrend
        {
            get => (bool)GetValue(IsPositiveTrendProperty);
            set => SetValue(IsPositiveTrendProperty, value);
        }

        #endregion

        #region Constructor

        public KPIWidget()
        {
            InitializeComponent();
            DataContext = this;
            UpdateTrendDisplay();
        }

        #endregion

        #region Private Methods

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is KPIWidget widget)
            {
                // Value değiştiğinde animasyon tetikle
                widget.AnimateValueChange();
            }
        }

        private static void OnTrendChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is KPIWidget widget)
            {
                widget.UpdateTrendDisplay();
            }
        }

        private void AnimateValueChange()
        {
            // Value animation trigger
            var storyboard = FindResource("LoadValueAnimation") as System.Windows.Media.Animation.Storyboard;
            storyboard?.Begin();
        }

        private void UpdateTrendDisplay()
        {
            if (TrendIcon != null)
            {
                TrendIcon.Text = IsPositiveTrend ? "📈" : "📉";
            }

            if (TrendValueText != null)
            {
                TrendValueText.Foreground = IsPositiveTrend ?
                    (Brush)FindResource("AccentGreenBrush") :
                    (Brush)FindResource("ErrorRedBrush");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Widget değerini animate ederek güncelle
        /// </summary>
        public void UpdateValue(string newValue, bool isPositive = true)
        {
            IsPositiveTrend = isPositive;
            Value = newValue;
        }

        /// <summary>
        /// Widget'ı success state'ine geçir
        /// </summary>
        public void ShowSuccess()
        {
            BackgroundColor = new SolidColorBrush(Colors.LightGreen) { Opacity = 0.1 };
        }

        /// <summary>
        /// Widget'ı warning state'ine geçir
        /// </summary>
        public void ShowWarning()
        {
            BackgroundColor = new SolidColorBrush(Colors.Orange) { Opacity = 0.1 };
        }

        /// <summary>
        /// Widget'ı error state'ine geçir
        /// </summary>
        public void ShowError()
        {
            BackgroundColor = new SolidColorBrush(Colors.Red) { Opacity = 0.1 };
        }

        /// <summary>
        /// Widget'ı normal state'e döndür
        /// </summary>
        public void ResetState()
        {
            BackgroundColor = Brushes.White;
        }

        #endregion
    }
}