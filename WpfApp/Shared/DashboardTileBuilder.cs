using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace WpfApp;

/// <summary>Builds the "stat tile" cards shared by all 4 role dashboards, so the
/// card layout/markup is written once instead of repeated in every Window.</summary>
public static class DashboardTileBuilder
{
    public static Border BuildTile(string title, string value, string symbolName, Brush accentBrush, string? subtitle = null)
    {
        var symbol = Enum.TryParse<SymbolRegular>(symbolName, out var parsed) ? parsed : SymbolRegular.Circle24;

        var accentColor = accentBrush is SolidColorBrush solid ? solid.Color : Colors.Gray;
        var tintBrush = new SolidColorBrush(Color.FromArgb(28, accentColor.R, accentColor.G, accentColor.B));
        var secondary = (Brush)Application.Current.Resources["TextSecondaryBrush"];

        // Left: value + title (+ optional subtitle) stacked and vertically centered.
        var leftStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        leftStack.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = value,
            FontSize = 28,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap
        });
        leftStack.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = title,
            FontSize = 13,
            Margin = new Thickness(0, 2, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            Foreground = secondary
        });
        if (!string.IsNullOrEmpty(subtitle))
        {
            leftStack.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = subtitle,
                FontSize = 11,
                Margin = new Thickness(0, 6, 0, 0),
                Foreground = accentBrush,
                TextWrapping = TextWrapping.Wrap
            });
        }

        // Right: rounded-square icon badge, top-aligned like a typical KPI card.
        var iconBadge = new Border
        {
            Width = 46,
            Height = 46,
            CornerRadius = new CornerRadius(12),
            Background = tintBrush,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Right,
            Child = new SymbolIcon { Symbol = symbol, FontSize = 22, Foreground = accentBrush, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(leftStack, 0);
        Grid.SetColumn(iconBadge, 1);
        grid.Children.Add(leftStack);
        grid.Children.Add(iconBadge);

        return new Border
        {
            Background = (Brush)Application.Current.Resources["SurfaceBrush"],
            BorderBrush = (Brush)Application.Current.Resources["BorderBrush2"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(18),
            Margin = new Thickness(0, 0, 16, 16),
            Width = 250,
            MinHeight = 118,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Color.FromArgb(0x18, 0, 0, 0),
                BlurRadius = 10,
                ShadowDepth = 1,
                Direction = 270
            },
            Child = grid
        };
    }
}
