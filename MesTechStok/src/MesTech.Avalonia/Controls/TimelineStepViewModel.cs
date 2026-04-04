using Avalonia;
using Avalonia.Media;

namespace MesTech.Avalonia.Controls;

public class TimelineStepViewModel
{
    private static Color T(string key) =>
        global::Avalonia.Application.Current?.FindResource(key) is Color c ? c : Colors.Gray;

    public string StepTitle { get; set; } = "";
    public DateTime? CompletedAt { get; set; }
    public bool IsCompleted => CompletedAt.HasValue;
    public bool IsCurrent { get; set; }
    public bool IsLastStep { get; set; }

    public string DateTimeText => CompletedAt?.ToString("dd.MM HH:mm") ?? "-";
    public FontWeight TitleWeight => IsCurrent ? FontWeight.Bold : FontWeight.Normal;

    public ISolidColorBrush StepColor => IsCompleted
        ? new SolidColorBrush(T("MesConnectedGreen"))
        : IsCurrent ? new SolidColorBrush(T("MesTimelineActive")) : new SolidColorBrush(Colors.Transparent);

    public ISolidColorBrush StepBorderColor => IsCompleted
        ? new SolidColorBrush(T("MesConnectedGreen"))
        : IsCurrent ? new SolidColorBrush(T("MesTimelineActive")) : new SolidColorBrush(T("MesTimelineInactive"));

    public ISolidColorBrush LineColor => IsCompleted
        ? new SolidColorBrush(T("MesConnectedGreen")) : new SolidColorBrush(T("MesTimelineTrack"));

    public ISolidColorBrush TitleColor => IsCompleted || IsCurrent
        ? new SolidColorBrush(T("MesTimelineText")) : new SolidColorBrush(T("MesNeutralGray"));
}
