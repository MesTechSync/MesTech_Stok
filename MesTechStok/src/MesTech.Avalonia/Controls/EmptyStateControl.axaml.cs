using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace MesTech.Avalonia.Controls;

public partial class EmptyStateControl : UserControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<EmptyStateControl, string?>(nameof(Title));

    public static readonly StyledProperty<string?> SubtitleProperty =
        AvaloniaProperty.Register<EmptyStateControl, string?>(nameof(Subtitle));

    public static readonly StyledProperty<string?> LottieSourceProperty =
        AvaloniaProperty.Register<EmptyStateControl, string?>(nameof(LottieSource));

    public static readonly StyledProperty<string?> ActionTextProperty =
        AvaloniaProperty.Register<EmptyStateControl, string?>(nameof(ActionText));

    public static readonly StyledProperty<ICommand?> ActionCommandProperty =
        AvaloniaProperty.Register<EmptyStateControl, ICommand?>(nameof(ActionCommand));

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string? LottieSource
    {
        get => GetValue(LottieSourceProperty);
        set => SetValue(LottieSourceProperty, value);
    }

    public string? ActionText
    {
        get => GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    public ICommand? ActionCommand
    {
        get => GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    public EmptyStateControl()
    {
        InitializeComponent();
    }
}
