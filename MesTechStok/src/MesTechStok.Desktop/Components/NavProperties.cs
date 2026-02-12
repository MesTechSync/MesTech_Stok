using System.Windows;
using System.Windows.Controls;

namespace MesTechStok.Desktop
{
    public static class NavProperties
    {
        public static readonly DependencyProperty IsActiveProperty = DependencyProperty.RegisterAttached(
            "IsActive",
            typeof(bool),
            typeof(NavProperties),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

        public static void SetIsActive(UIElement element, bool value) => element.SetValue(IsActiveProperty, value);
        public static bool GetIsActive(UIElement element) => (bool)element.GetValue(IsActiveProperty);

        public static readonly DependencyProperty HasBadgeProperty = DependencyProperty.RegisterAttached(
            "HasBadge",
            typeof(bool),
            typeof(NavProperties),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

        public static void SetHasBadge(UIElement element, bool value) => element.SetValue(HasBadgeProperty, value);
        public static bool GetHasBadge(UIElement element) => (bool)element.GetValue(HasBadgeProperty);

        public static readonly DependencyProperty BadgeTextProperty = DependencyProperty.RegisterAttached(
            "BadgeText",
            typeof(string),
            typeof(NavProperties),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.Inherits));

        public static void SetBadgeText(UIElement element, string value) => element.SetValue(BadgeTextProperty, value);
        public static string GetBadgeText(UIElement element) => (string)element.GetValue(BadgeTextProperty);
    }
}


