using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace MesTech.Avalonia.Dialogs;

public class MappingPair
{
    public string SourceColumn { get; set; } = string.Empty;
    public string TargetColumn { get; set; } = string.Empty;
}

public partial class MappingDialog : Window
{
    private readonly List<string> _targetColumns;
    private readonly List<(TextBlock Source, ComboBox Target)> _rows = new();
    public bool Result { get; private set; }
    public List<MappingPair> Mappings { get; private set; } = new();

    public MappingDialog() : this(string.Empty, Enumerable.Empty<string>(), Enumerable.Empty<string>()) { }

    public MappingDialog(string title, IEnumerable<string> sourceColumns, IEnumerable<string> targetColumns)
    {
        InitializeComponent();
        TitleText.Text = title;
        _targetColumns = targetColumns.ToList();

        foreach (var source in sourceColumns)
        {
            var row = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("*,80,*")
            };

            var sourceLabel = new TextBlock
            {
                Text = source,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = (global::Avalonia.Application.Current?.Resources.TryGetResource("TextPrimaryBrush", null, out var vtp) == true ? vtp as IBrush : null) ?? Brushes.Black
            };
            Grid.SetColumn(sourceLabel, 0);

            var arrow = new TextBlock
            {
                Text = "\u2192",
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = (global::Avalonia.Application.Current?.Resources.TryGetResource("TextSecondaryBrush", null, out var vts) == true ? vts as IBrush : null) ?? Brushes.Gray
            };
            Grid.SetColumn(arrow, 1);

            var targetCombo = new ComboBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            targetCombo.Items.Add(new ComboBoxItem { Content = "(Eslestirme)" });
            foreach (var t in _targetColumns)
            {
                targetCombo.Items.Add(new ComboBoxItem { Content = t });
            }
            targetCombo.SelectedIndex = 0;
            Grid.SetColumn(targetCombo, 2);

            row.Children.Add(sourceLabel);
            row.Children.Add(arrow);
            row.Children.Add(targetCombo);

            _rows.Add((sourceLabel, targetCombo));
            MappingPanel.Children.Add(row);
        }
    }

    private void OnApply(object? sender, RoutedEventArgs e)
    {
        Mappings = _rows
            .Where(r => r.Target.SelectedIndex > 0)
            .Select(r => new MappingPair
            {
                SourceColumn = r.Source.Text ?? string.Empty,
                TargetColumn = (r.Target.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty
            })
            .ToList();
        Result = true;
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Result = false;
            Close();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }
}
