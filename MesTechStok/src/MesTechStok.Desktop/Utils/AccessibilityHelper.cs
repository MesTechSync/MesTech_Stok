using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MesTechStok.Desktop.Utils
{
    /// <summary>
    /// Accessibility Helper - BRAVO TİMİ Phase 3
    /// WCAG 2.1 AA compliance utilities
    /// </summary>
    public static class AccessibilityHelper
    {
        #region Keyboard Navigation Support

        /// <summary>
        /// Enable keyboard navigation for custom controls
        /// </summary>
        public static void EnableKeyboardNavigation(UIElement element)
        {
            if (element == null) return;

            element.Focusable = true;

            // For controls that support IsTabStop
            if (element is Control control)
            {
                control.IsTabStop = true;
            }

            // Add keyboard event handlers
            element.PreviewKeyDown += OnPreviewKeyDown;
        }

        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var element = sender as UIElement;
            if (element == null) return;

            switch (e.Key)
            {
                case Key.Enter:
                case Key.Space:
                    // Trigger click for buttons and interactive elements
                    if (element is Button button)
                    {
                        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        e.Handled = true;
                    }
                    break;

                case Key.Escape:
                    // Close modals or cancel operations
                    if (element is Window window)
                    {
                        window.Close();
                        e.Handled = true;
                    }
                    break;

                case Key.F1:
                    // Show help
                    ShowContextualHelp(element);
                    e.Handled = true;
                    break;
            }
        }

        #endregion

        #region Screen Reader Support

        /// <summary>
        /// Set accessibility properties for screen readers
        /// </summary>
        public static void SetAccessibilityInfo(UIElement element, string name, string helpText, AutomationControlType controlType)
        {
            if (element == null) return;

            AutomationProperties.SetName(element, name);
            AutomationProperties.SetHelpText(element, helpText);
            // Note: SetControlType and SetIsKeyboardFocusable are not available in WPF
        }

        /// <summary>
        /// Set live region for dynamic content
        /// </summary>
        public static void SetLiveRegion(UIElement element, AutomationLiveSetting liveSetting = AutomationLiveSetting.Polite)
        {
            if (element == null) return;

            AutomationProperties.SetLiveSetting(element, liveSetting);
        }

        /// <summary>
        /// Announce message to screen readers
        /// </summary>
        public static void AnnounceToScreenReader(string message, UIElement? element = null)
        {
            if (string.IsNullOrEmpty(message)) return;

            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                if (element != null)
                {
                    AutomationProperties.SetName(element, message);
                    SetLiveRegion(element, AutomationLiveSetting.Assertive);
                }
            });
        }

        #endregion

        #region Color Contrast Validation

        /// <summary>
        /// Validate color contrast ratio for WCAG AA compliance (4.5:1)
        /// </summary>
        public static bool ValidateContrastRatio(Color foreground, Color background, bool isLargeText = false)
        {
            double contrastRatio = CalculateContrastRatio(foreground, background);
            double requiredRatio = isLargeText ? 3.0 : 4.5; // WCAG AA standards

            return contrastRatio >= requiredRatio;
        }

        /// <summary>
        /// Calculate contrast ratio between two colors
        /// </summary>
        public static double CalculateContrastRatio(Color color1, Color color2)
        {
            double luminance1 = GetLuminance(color1);
            double luminance2 = GetLuminance(color2);

            double lighter = Math.Max(luminance1, luminance2);
            double darker = Math.Min(luminance1, luminance2);

            return (lighter + 0.05) / (darker + 0.05);
        }

        private static double GetLuminance(Color color)
        {
            double r = GetLinearRGB(color.R / 255.0);
            double g = GetLinearRGB(color.G / 255.0);
            double b = GetLinearRGB(color.B / 255.0);

            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }

        private static double GetLinearRGB(double colorValue)
        {
            return colorValue <= 0.03928
                ? colorValue / 12.92
                : Math.Pow((colorValue + 0.055) / 1.055, 2.4);
        }

        #endregion

        #region Focus Management

        /// <summary>
        /// Set focus to first focusable element in container
        /// </summary>
        public static void SetFocusToFirstElement(DependencyObject container)
        {
            if (container == null) return;

            var firstFocusable = FindFirstFocusableChild(container);
            if (firstFocusable != null)
            {
                firstFocusable.Focus();
            }
        }

        private static UIElement? FindFirstFocusableChild(DependencyObject parent)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is UIElement uiElement && uiElement.Focusable && uiElement.IsEnabled)
                {
                    return uiElement;
                }

                var foundChild = FindFirstFocusableChild(child);
                if (foundChild != null)
                {
                    return foundChild;
                }
            }

            return null;
        }

        /// <summary>
        /// Create focus trap for modal dialogs
        /// </summary>
        public static void CreateFocusTrap(UIElement container)
        {
            if (container == null) return;

            container.PreviewKeyDown += (sender, e) =>
            {
                if (e.Key == Key.Tab)
                {
                    var focusableElements = GetFocusableChildren(container).ToList();
                    if (focusableElements.Count == 0) return;

                    var focusedElement = Keyboard.FocusedElement as UIElement;
                    var currentIndex = focusedElement != null ? focusableElements.IndexOf(focusedElement) : -1;

                    if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
                    {
                        // Shift+Tab (backward)
                        var prevIndex = currentIndex <= 0 ? focusableElements.Count - 1 : currentIndex - 1;
                        focusableElements[prevIndex].Focus();
                    }
                    else
                    {
                        // Tab (forward)
                        var nextIndex = currentIndex >= focusableElements.Count - 1 ? 0 : currentIndex + 1;
                        focusableElements[nextIndex].Focus();
                    }

                    e.Handled = true;
                }
            };
        }

        private static IEnumerable<UIElement> GetFocusableChildren(DependencyObject parent)
        {
            var children = new List<UIElement>();
            GetFocusableChildrenRecursive(parent, children);
            return children.Where(c => c.IsVisible && c.IsEnabled);
        }

        private static void GetFocusableChildrenRecursive(DependencyObject parent, List<UIElement> children)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is UIElement uiElement && uiElement.Focusable)
                {
                    children.Add(uiElement);
                }

                GetFocusableChildrenRecursive(child, children);
            }
        }

        #endregion

        #region High Contrast Support

        /// <summary>
        /// Check if high contrast mode is enabled
        /// </summary>
        public static bool IsHighContrastMode()
        {
            return SystemParameters.HighContrast;
        }

        /// <summary>
        /// Get high contrast friendly colors
        /// </summary>
        public static Color GetHighContrastColor(string colorRole)
        {
            if (!IsHighContrastMode()) return Color.FromRgb(0, 0, 0); // Black default

            return colorRole.ToLower() switch
            {
                "text" => SystemColors.WindowTextColor,
                "background" => SystemColors.WindowColor,
                "highlight" => SystemColors.HighlightColor,
                "highlighttext" => SystemColors.HighlightTextColor,
                "disabled" => SystemColors.GrayTextColor,
                _ => Color.FromRgb(0, 0, 0) // Black default
            };
        }

        #endregion

        #region Contextual Help

        private static void ShowContextualHelp(UIElement element)
        {
            var helpText = AutomationProperties.GetHelpText(element);
            if (!string.IsNullOrEmpty(helpText))
            {
                // Show tooltip or help dialog
                var tooltip = new ToolTip
                {
                    Content = helpText,
                    IsOpen = true
                };

                element.SetValue(FrameworkElement.ToolTipProperty, tooltip);

                // Auto-close after 5 seconds
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(5)
                };
                timer.Tick += (s, e) =>
                {
                    tooltip.IsOpen = false;
                    timer.Stop();
                };
                timer.Start();
            }
        }

        #endregion
    }
}