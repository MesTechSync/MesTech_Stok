using Avalonia;
using Avalonia.Media;

namespace MesTech.Avalonia.Controls;

public class TimelineStepViewModel
{
    public string StepTitle { get; set; } = "";
    public DateTime? CompletedAt { get; set; }
    public bool IsCompleted => CompletedAt.HasValue;
    public bool IsCurrent { get; set; }
    public bool IsLastStep { get; set; }

    public string DateTimeText => CompletedAt?.ToString("dd.MM HH:mm") ?? "—";
    public FontWeight TitleWeight => IsCurrent ? FontWeight.Bold : FontWeight.Normal;

    public ISolidColorBrush StepColor => IsCompleted
        ? new SolidColorBrush(Color.Parse("#4CAF50"))
        : IsCurrent ? new SolidColorBrush(Color.Parse("#2196F3")) : new SolidColorBrush(Colors.Transparent);

    public ISolidColorBrush StepBorderColor => IsCompleted
        ? new SolidColorBrush(Color.Parse("#4CAF50"))
        : IsCurrent ? new SolidColorBrush(Color.Parse("#2196F3")) : new SolidColorBrush(Color.Parse("#BDBDBD"));

    public ISolidColorBrush LineColor => IsCompleted
        ? new SolidColorBrush(Color.Parse("#4CAF50")) : new SolidColorBrush(Color.Parse("#E0E0E0"));

    public ISolidColorBrush TitleColor => IsCompleted || IsCurrent
        ? new SolidColorBrush(Color.Parse("#212121")) : new SolidColorBrush(Color.Parse("#9E9E9E"));
}
