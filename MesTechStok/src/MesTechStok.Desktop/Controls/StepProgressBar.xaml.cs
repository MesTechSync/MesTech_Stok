using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MesTechStok.Desktop.Controls;

public partial class StepProgressBar : UserControl
{
    public static readonly DependencyProperty CurrentStepProperty =
        DependencyProperty.Register(nameof(CurrentStep), typeof(int), typeof(StepProgressBar),
            new PropertyMetadata(0, OnStepChanged));

    public static readonly DependencyProperty StepsProperty =
        DependencyProperty.Register(nameof(Steps), typeof(List<string>), typeof(StepProgressBar),
            new PropertyMetadata(null, OnStepChanged));

    public int CurrentStep
    {
        get => (int)GetValue(CurrentStepProperty);
        set => SetValue(CurrentStepProperty, value);
    }

    public List<string> Steps
    {
        get => (List<string>)GetValue(StepsProperty);
        set => SetValue(StepsProperty, value);
    }

    // Bitrix24 palette colors
    private static readonly Color PrimaryColor = Color.FromRgb(40, 85, 172);    // #2855AC
    private static readonly Color SuccessColor = Color.FromRgb(16, 185, 129);   // #10b981
    private static readonly Color MutedColor = Color.FromRgb(124, 141, 176);    // #7c8db0
    private static readonly Color MutedLineColor = Color.FromRgb(224, 228, 234);// #e0e4ea

    public StepProgressBar()
    {
        InitializeComponent();
    }

    private static void OnStepChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StepProgressBar bar)
            bar.RenderSteps();
    }

    private void RenderSteps()
    {
        StepsContainer.Items.Clear();
        var steps = Steps;
        if (steps == null || steps.Count == 0)
            return;

        for (int i = 0; i < steps.Count; i++)
        {
            bool isCompleted = i < CurrentStep;
            bool isCurrent = i == CurrentStep;

            // Step circle + label
            var stepPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = 80
            };

            // Circle
            var circle = new Ellipse
            {
                Width = 32,
                Height = 32,
                HorizontalAlignment = HorizontalAlignment.Center,
                StrokeThickness = 2
            };

            // Inner content (number or check)
            var circleContent = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold
            };

            if (isCompleted)
            {
                circle.Fill = new SolidColorBrush(SuccessColor);
                circle.Stroke = new SolidColorBrush(SuccessColor);
                circleContent.Text = "\u2713"; // checkmark
                circleContent.Foreground = Brushes.White;
            }
            else if (isCurrent)
            {
                circle.Fill = new SolidColorBrush(PrimaryColor);
                circle.Stroke = new SolidColorBrush(PrimaryColor);
                circleContent.Text = (i + 1).ToString();
                circleContent.Foreground = Brushes.White;
            }
            else
            {
                circle.Fill = Brushes.White;
                circle.Stroke = new SolidColorBrush(MutedColor);
                circleContent.Text = (i + 1).ToString();
                circleContent.Foreground = new SolidColorBrush(MutedColor);
            }

            // Grid to overlay circle + content
            var circleGrid = new Grid
            {
                Width = 32,
                Height = 32,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            circleGrid.Children.Add(circle);
            circleGrid.Children.Add(circleContent);

            // Label
            var label = new TextBlock
            {
                Text = steps[i],
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 0),
                Foreground = isCompleted || isCurrent
                    ? new SolidColorBrush(Color.FromRgb(26, 26, 46))  // #1a1a2e
                    : new SolidColorBrush(MutedColor)
            };
            if (isCurrent)
                label.FontWeight = FontWeights.SemiBold;

            stepPanel.Children.Add(circleGrid);
            stepPanel.Children.Add(label);
            StepsContainer.Items.Add(stepPanel);

            // Connector line (not after last step)
            if (i < steps.Count - 1)
            {
                var lineColor = i < CurrentStep ? SuccessColor : MutedLineColor;
                var line = new Border
                {
                    Width = 40,
                    Height = 2,
                    Background = new SolidColorBrush(lineColor),
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 16, 0, 0)  // align with circle center
                };
                StepsContainer.Items.Add(line);
            }
        }
    }
}
