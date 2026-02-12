using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace MesTechStok.Desktop.Components
{
    /// <summary>
    /// LoadingSpinner Component - BRAVO TİMİ
    /// Modern loading indicator with customizable animations
    /// </summary>
    public partial class LoadingSpinner : UserControl
    {
        #region Dependency Properties

        // Spinner Size Property
        public static readonly DependencyProperty SpinnerSizeProperty =
            DependencyProperty.Register("SpinnerSize", typeof(double), typeof(LoadingSpinner),
                new PropertyMetadata(60.0));

        public double SpinnerSize
        {
            get => (double)GetValue(SpinnerSizeProperty);
            set => SetValue(SpinnerSizeProperty, value);
        }

        // Inner Glow Size Property (calculated from SpinnerSize)
        public double InnerGlowSize => SpinnerSize * 0.6;

        // Icon Size Property (calculated from SpinnerSize)
        public double IconSize => SpinnerSize * 0.3;

        // Center Icon Property
        public static readonly DependencyProperty CenterIconProperty =
            DependencyProperty.Register("CenterIcon", typeof(string), typeof(LoadingSpinner),
                new PropertyMetadata("⚡"));

        public string CenterIcon
        {
            get => (string)GetValue(CenterIconProperty);
            set => SetValue(CenterIconProperty, value);
        }

        // Loading Text Property
        public static readonly DependencyProperty LoadingTextProperty =
            DependencyProperty.Register("LoadingText", typeof(string), typeof(LoadingSpinner),
                new PropertyMetadata("Yükleniyor..."));

        public string LoadingText
        {
            get => (string)GetValue(LoadingTextProperty);
            set => SetValue(LoadingTextProperty, value);
        }

        // Progress Text Property
        public static readonly DependencyProperty ProgressTextProperty =
            DependencyProperty.Register("ProgressText", typeof(string), typeof(LoadingSpinner),
                new PropertyMetadata(string.Empty));

        public string ProgressText
        {
            get => (string)GetValue(ProgressTextProperty);
            set => SetValue(ProgressTextProperty, value);
        }

        // Show Background Property
        public static readonly DependencyProperty ShowBackgroundProperty =
            DependencyProperty.Register("ShowBackground", typeof(bool), typeof(LoadingSpinner),
                new PropertyMetadata(false));

        public bool ShowBackground
        {
            get => (bool)GetValue(ShowBackgroundProperty);
            set => SetValue(ShowBackgroundProperty, value);
        }

        // Show Loading Text Property
        public static readonly DependencyProperty ShowLoadingTextProperty =
            DependencyProperty.Register("ShowLoadingText", typeof(bool), typeof(LoadingSpinner),
                new PropertyMetadata(true));

        public bool ShowLoadingText
        {
            get => (bool)GetValue(ShowLoadingTextProperty);
            set => SetValue(ShowLoadingTextProperty, value);
        }

        // Show Progress Text Property
        public static readonly DependencyProperty ShowProgressTextProperty =
            DependencyProperty.Register("ShowProgressText", typeof(bool), typeof(LoadingSpinner),
                new PropertyMetadata(false));

        public bool ShowProgressText
        {
            get => (bool)GetValue(ShowProgressTextProperty);
            set => SetValue(ShowProgressTextProperty, value);
        }

        // Is Active Property
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register("IsActive", typeof(bool), typeof(LoadingSpinner),
                new PropertyMetadata(true, OnIsActiveChanged));

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        #endregion

        #region Private Fields

        private Storyboard? _spinAnimation;
        private Storyboard? _pulseAnimation;
        private Storyboard? _fadeInAnimation;
        private Storyboard? _fadeOutAnimation;

        #endregion

        #region Constructor

        public LoadingSpinner()
        {
            InitializeComponent();
            DataContext = this;
            InitializeAnimations();
        }

        #endregion

        #region Private Methods

        private void InitializeAnimations()
        {
            _spinAnimation = FindResource("SpinAnimation") as Storyboard;
            _pulseAnimation = FindResource("PulseAnimation") as Storyboard;
            _fadeInAnimation = FindResource("FadeInAnimation") as Storyboard;
            _fadeOutAnimation = FindResource("FadeOutAnimation") as Storyboard;

            if (_fadeOutAnimation != null)
            {
                _fadeOutAnimation.Completed += OnFadeOutCompleted;
            }
        }

        private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LoadingSpinner spinner)
            {
                if ((bool)e.NewValue)
                {
                    spinner.StartLoading();
                }
                else
                {
                    spinner.StopLoading();
                }
            }
        }

        private void OnFadeOutCompleted(object? sender, EventArgs e)
        {
            Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Start the loading animation
        /// </summary>
        public void StartLoading()
        {
            Visibility = Visibility.Visible;
            _fadeInAnimation?.Begin(this);
            _spinAnimation?.Begin(this);
        }

        /// <summary>
        /// Stop the loading animation
        /// </summary>
        public void StopLoading()
        {
            _spinAnimation?.Stop(this);
            _pulseAnimation?.Stop(this);
            _fadeOutAnimation?.Begin(this);
        }

        /// <summary>
        /// Start pulse animation (for emphasis)
        /// </summary>
        public void StartPulse()
        {
            _pulseAnimation?.Begin(this);
        }

        /// <summary>
        /// Stop pulse animation
        /// </summary>
        public void StopPulse()
        {
            _pulseAnimation?.Stop(this);
        }

        /// <summary>
        /// Update progress text with percentage
        /// </summary>
        public void UpdateProgress(int percentage)
        {
            ProgressText = $"%{percentage} tamamlandı";
            ShowProgressText = true;
        }

        /// <summary>
        /// Update progress text with custom message
        /// </summary>
        public void UpdateProgress(string progressMessage)
        {
            ProgressText = progressMessage;
            ShowProgressText = !string.IsNullOrEmpty(progressMessage);
        }

        /// <summary>
        /// Set loading state with custom text and icon
        /// </summary>
        public void SetLoadingState(string loadingText, string icon = "⚡")
        {
            LoadingText = loadingText;
            CenterIcon = icon;
            ShowLoadingText = !string.IsNullOrEmpty(loadingText);
            StartLoading();
        }

        /// <summary>
        /// Show success state briefly before hiding
        /// </summary>
        public void ShowSuccess(string successText = "Tamamlandı!")
        {
            LoadingText = successText;
            CenterIcon = "✅";
            _spinAnimation?.Stop(this);

            // Show success for 1 second then hide
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                StopLoading();
            };
            timer.Start();
        }

        /// <summary>
        /// Show error state briefly before hiding
        /// </summary>
        public void ShowError(string errorText = "Hata oluştu!")
        {
            LoadingText = errorText;
            CenterIcon = "❌";
            _spinAnimation?.Stop(this);

            // Show error for 2 seconds then hide
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                StopLoading();
            };
            timer.Start();
        }

        #endregion
    }
}