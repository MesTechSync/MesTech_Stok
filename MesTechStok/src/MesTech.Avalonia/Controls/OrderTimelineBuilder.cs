using MesTech.Domain.Enums;

namespace MesTech.Avalonia.Controls;

public static class OrderTimelineBuilder
{
    public static List<TimelineStepViewModel> BuildTimeline(
        OrderStatus status,
        DateTime createdAt,
        DateTime? confirmedAt = null,
        DateTime? preparedAt = null,
        DateTime? shippedAt = null,
        DateTime? deliveredAt = null,
        DateTime? cancelledAt = null)
    {
        var steps = new List<TimelineStepViewModel>
        {
            new() { StepTitle = "Oluşturuldu", CompletedAt = createdAt },
            new() { StepTitle = "Onaylandı", CompletedAt = confirmedAt },
            new() { StepTitle = "Hazırlandı", CompletedAt = preparedAt },
            new() { StepTitle = "Kargoda", CompletedAt = shippedAt },
            new() { StepTitle = "Teslim Edildi", CompletedAt = deliveredAt, IsLastStep = true },
        };

        if (status == OrderStatus.Cancelled)
        {
            steps.Add(new() { StepTitle = "İptal Edildi", CompletedAt = cancelledAt, IsCurrent = true, IsLastStep = true });
            // Clear IsLastStep from "Teslim Edildi"
            steps[4].IsLastStep = false;
        }
        else
        {
            // Find current step
            for (int i = steps.Count - 1; i >= 0; i--)
            {
                if (steps[i].IsCompleted && !steps[i].IsLastStep)
                {
                    if (i + 1 < steps.Count && !steps[i + 1].IsCompleted)
                        steps[i + 1].IsCurrent = true;
                    break;
                }
            }
        }

        return steps;
    }
}
