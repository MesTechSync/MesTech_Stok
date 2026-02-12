// 🎨 **NEURAL THEME ENGINE - Modern Visual System**
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;

namespace MesTechStok.Desktop.Neural.Themes
{
    public static class NeuralTheme
    {
        // 🎨 Neural Color Palette
        public static class Colors
        {
            // Primary Colors
            public static readonly Color NeuralBlue = Color.FromArgb(255, 0, 122, 255);
            public static readonly Color DeepDark = Color.FromArgb(255, 30, 30, 30);
            public static readonly Color DarkGray = Color.FromArgb(255, 51, 51, 51);
            public static readonly Color ElevatedDark = Color.FromArgb(255, 45, 45, 45);

            // Accent Colors
            public static readonly Color BrightGreen = Color.FromArgb(255, 0, 255, 136);
            public static readonly Color NeuralOrange = Color.FromArgb(255, 255, 170, 0);
            public static readonly Color AlertRed = Color.FromArgb(255, 255, 0, 68);

            // Text Colors
            public static readonly Color PureWhite = Color.FromArgb(255, 255, 255, 255);
            public static readonly Color LightGray = Color.FromArgb(255, 204, 204, 204);
            public static readonly Color BorderGray = Color.FromArgb(255, 70, 70, 70);

            // State Colors
            public static readonly Color Success = BrightGreen;
            public static readonly Color Warning = NeuralOrange;
            public static readonly Color Error = AlertRed;
            public static readonly Color Info = NeuralBlue;
        }

        // 🔧 Brushes for easy use
        public static class Brushes
        {
            public static readonly SolidColorBrush NeuralBlue = new(Colors.NeuralBlue);
            public static readonly SolidColorBrush DeepDark = new(Colors.DeepDark);
            public static readonly SolidColorBrush DarkGray = new(Colors.DarkGray);
            public static readonly SolidColorBrush ElevatedDark = new(Colors.ElevatedDark);
            public static readonly SolidColorBrush BrightGreen = new(Colors.BrightGreen);
            public static readonly SolidColorBrush NeuralOrange = new(Colors.NeuralOrange);
            public static readonly SolidColorBrush AlertRed = new(Colors.AlertRed);
            public static readonly SolidColorBrush PureWhite = new(Colors.PureWhite);
            public static readonly SolidColorBrush LightGray = new(Colors.LightGray);
            public static readonly SolidColorBrush BorderGray = new(Colors.BorderGray);
            public static readonly SolidColorBrush Success = new(Colors.Success);
            public static readonly SolidColorBrush Warning = new(Colors.Warning);
            public static readonly SolidColorBrush Error = new(Colors.Error);
            public static readonly SolidColorBrush Info = new(Colors.Info);
        }

        // ✨ Neural Glow Effects
        public static class Effects
        {
            public static System.Windows.Media.Effects.DropShadowEffect CreateNeuralGlow(Color color, double opacity = 0.6)
            {
                return new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = color,
                    Direction = 315,
                    ShadowDepth = 0,
                    BlurRadius = 10,
                    Opacity = opacity
                };
            }

            public static System.Windows.Media.Effects.DropShadowEffect CreateNeuralShadow(Color color, double depth = 2)
            {
                return new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = color,
                    Direction = 315,
                    ShadowDepth = depth,
                    BlurRadius = 5,
                    Opacity = 0.5
                };
            }

            public static System.Windows.Media.Effects.DropShadowEffect NeuralBlueGlow => CreateNeuralGlow(Colors.NeuralBlue);
            public static System.Windows.Media.Effects.DropShadowEffect SuccessGlow => CreateNeuralGlow(Colors.Success);
            public static System.Windows.Media.Effects.DropShadowEffect WarningGlow => CreateNeuralGlow(Colors.Warning);
            public static System.Windows.Media.Effects.DropShadowEffect ErrorGlow => CreateNeuralGlow(Colors.Error);
        }

        // 📐 Spacing and Dimensions
        public static class Spacing
        {
            public static readonly Thickness XSmall = new(2);
            public static readonly Thickness Small = new(4);
            public static readonly Thickness Medium = new(8);
            public static readonly Thickness Large = new(16);
            public static readonly Thickness XLarge = new(24);
            public static readonly Thickness XXLarge = new(32);

            // Component-specific spacing
            public static readonly Thickness ButtonPadding = new(16, 8, 16, 8);
            public static readonly Thickness TextBoxPadding = new(8);
            public static readonly Thickness CardPadding = new(20);
            public static readonly Thickness PanelMargin = new(10);
        }

        // 🎯 Corner Radius
        public static class Radius
        {
            public static readonly CornerRadius Small = new(4);
            public static readonly CornerRadius Medium = new(8);
            public static readonly CornerRadius Large = new(12);
            public static readonly CornerRadius XLarge = new(16);
            public static readonly CornerRadius Round = new(50);
        }

        // 📝 Typography
        public static class Typography
        {
            // Font Sizes
            public const double TitleSize = 24;
            public const double HeadingSize = 18;
            public const double SubheadingSize = 16;
            public const double BodySize = 14;
            public const double CaptionSize = 12;
            public const double SmallSize = 10;

            // Font Weights
            public static readonly FontWeight Light = FontWeights.Light;
            public static readonly FontWeight Normal = FontWeights.Normal;
            public static readonly FontWeight Medium = FontWeights.Medium;
            public static readonly FontWeight Bold = FontWeights.Bold;
        }

        // ⏱️ Animations
        public static class Animations
        {
            public static readonly Duration Fast = new(TimeSpan.FromMilliseconds(150));
            public static readonly Duration Medium = new(TimeSpan.FromMilliseconds(300));
            public static readonly Duration Slow = new(TimeSpan.FromMilliseconds(500));

            public static System.Windows.Media.Animation.QuadraticEase EaseOut => new() { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut };
            public static System.Windows.Media.Animation.QuadraticEase EaseIn => new() { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn };
            public static System.Windows.Media.Animation.QuadraticEase EaseInOut => new() { EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut };
        }

        // 🎭 Pre-built Styles
        public static class Styles
        {
            public static Style CreateNeuralButtonStyle()
            {
                var style = new Style(typeof(Button));

                style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.DarkGray));
                style.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.PureWhite));
                style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
                style.Setters.Add(new Setter(Control.FontSizeProperty, Typography.BodySize));
                style.Setters.Add(new Setter(Control.FontWeightProperty, Typography.Medium));
                style.Setters.Add(new Setter(Control.PaddingProperty, Spacing.ButtonPadding));
                style.Setters.Add(new Setter(Control.MarginProperty, Spacing.Small));
                style.Setters.Add(new Setter(FrameworkElement.CursorProperty, System.Windows.Input.Cursors.Hand));
                style.Setters.Add(new Setter(UIElement.OpacityProperty, 0.9));
                style.Setters.Add(new Setter(UIElement.EffectProperty, Effects.CreateNeuralShadow(Colors.NeuralBlue)));

                return style;
            }

            public static Style CreateNeuralTextBoxStyle()
            {
                var style = new Style(typeof(TextBox));

                style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.ElevatedDark));
                style.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.PureWhite));
                style.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.BorderGray));
                style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
                style.Setters.Add(new Setter(Control.PaddingProperty, Spacing.TextBoxPadding));
                style.Setters.Add(new Setter(Control.FontSizeProperty, Typography.BodySize));

                return style;
            }

            public static Style CreateNeuralCardStyle()
            {
                var style = new Style(typeof(Border));

                style.Setters.Add(new Setter(Border.BackgroundProperty, Brushes.ElevatedDark));
                style.Setters.Add(new Setter(Border.BorderBrushProperty, Brushes.BorderGray));
                style.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(1)));
                style.Setters.Add(new Setter(Border.CornerRadiusProperty, Radius.Medium));
                style.Setters.Add(new Setter(Border.PaddingProperty, Spacing.CardPadding));
                style.Setters.Add(new Setter(UIElement.EffectProperty, Effects.CreateNeuralShadow(Colors.DeepDark)));

                return style;
            }
        }

        // 🌊 Gradient Brushes
        public static class Gradients
        {
            public static LinearGradientBrush CreateNeuralGradient(Color startColor, Color endColor)
            {
                return new LinearGradientBrush(startColor, endColor, 45);
            }

            public static readonly LinearGradientBrush NeuralBlueGradient = CreateNeuralGradient(
                Colors.NeuralBlue,
                Color.FromArgb(255, 0, 90, 200)
            );

            public static readonly LinearGradientBrush DarkGradient = CreateNeuralGradient(
                Colors.DeepDark,
                Colors.DarkGray
            );

            public static readonly LinearGradientBrush SuccessGradient = CreateNeuralGradient(
                Colors.Success,
                Color.FromArgb(255, 0, 200, 100)
            );
        }

        // 🎪 Helper Methods
        public static class Helpers
        {
            public static void ApplyNeuralThemeToWindow(Window window)
            {
                window.Background = Brushes.DeepDark;

                // Apply global styles
                if (window.Resources == null)
                    window.Resources = new ResourceDictionary();

                window.Resources.Add(typeof(Button), Styles.CreateNeuralButtonStyle());
                window.Resources.Add(typeof(TextBox), Styles.CreateNeuralTextBoxStyle());
                window.Resources.Add("NeuralCard", Styles.CreateNeuralCardStyle());
            }

            public static void ApplyNeuralGlow(UIElement element, Color color, double opacity = 0.6)
            {
                element.Effect = Effects.CreateNeuralGlow(color, opacity);
            }

            public static void RemoveGlow(UIElement element)
            {
                element.Effect = null;
            }

            public static SolidColorBrush GetStateColor(bool isSuccess, bool isWarning = false, bool isError = false)
            {
                if (isError) return Brushes.Error;
                if (isWarning) return Brushes.Warning;
                if (isSuccess) return Brushes.Success;
                return Brushes.Info;
            }

            public static string GetStateIcon(bool isSuccess, bool isWarning = false, bool isError = false)
            {
                if (isError) return "❌";
                if (isWarning) return "⚠️";
                if (isSuccess) return "✅";
                return "ℹ️";
            }
        }

        // 📱 Responsive Design
        public static class Responsive
        {
            public const double SmallScreen = 800;
            public const double MediumScreen = 1200;
            public const double LargeScreen = 1600;

            public static bool IsSmallScreen(double width) => width <= SmallScreen;
            public static bool IsMediumScreen(double width) => width > SmallScreen && width <= MediumScreen;
            public static bool IsLargeScreen(double width) => width > MediumScreen;

            public static double GetResponsiveFontSize(double baseSize, double screenWidth)
            {
                if (IsSmallScreen(screenWidth)) return baseSize * 0.9;
                if (IsLargeScreen(screenWidth)) return baseSize * 1.1;
                return baseSize;
            }
        }

        // 🎨 Theme Variants
        public static class Variants
        {
            // High Contrast Mode
            public static class HighContrast
            {
                public static readonly SolidColorBrush Background = new(Color.FromArgb(255, 0, 0, 0));
                public static readonly SolidColorBrush Foreground = new(Color.FromArgb(255, 255, 255, 255));
                public static readonly SolidColorBrush Accent = new(Color.FromArgb(255, 0, 255, 255));
            }

            // Light Mode (Optional)
            public static class Light
            {
                public static readonly SolidColorBrush Background = new(Color.FromArgb(255, 248, 249, 250));
                public static readonly SolidColorBrush Surface = new(Color.FromArgb(255, 255, 255, 255));
                public static readonly SolidColorBrush Text = new(Color.FromArgb(255, 33, 37, 41));
            }
        }
    }

    // 🎭 Theme Manager
    public static class NeuralThemeManager
    {
        private static readonly Dictionary<string, ResourceDictionary> _themes = new();
        private static string _currentTheme = "Neural";

        public static void RegisterTheme(string name, ResourceDictionary resources)
        {
            _themes[name] = resources;
        }

        public static void ApplyTheme(string themeName, Application app)
        {
            if (_themes.TryGetValue(themeName, out var theme))
            {
                app.Resources.MergedDictionaries.Clear();
                app.Resources.MergedDictionaries.Add(theme);
                _currentTheme = themeName;
            }
        }

        public static string CurrentTheme => _currentTheme;

        public static void InitializeDefaultThemes()
        {
            var neuralTheme = new ResourceDictionary();

            // Add default neural styles
            neuralTheme.Add(typeof(Button), NeuralTheme.Styles.CreateNeuralButtonStyle());
            neuralTheme.Add(typeof(TextBox), NeuralTheme.Styles.CreateNeuralTextBoxStyle());
            neuralTheme.Add("NeuralCard", NeuralTheme.Styles.CreateNeuralCardStyle());

            RegisterTheme("Neural", neuralTheme);
        }
    }
}
