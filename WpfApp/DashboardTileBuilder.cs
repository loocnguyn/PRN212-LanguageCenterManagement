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
        var tintBrush = new SolidColorBrush(Color.FromArgb(30, accentColor.R, accentColor.G, accentColor.B));

        var iconBadge = new Border
        {
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(20),
            Background = tintBrush,
            Child = new SymbolIcon { Symbol = symbol, FontSize = 18, Foreground = accentBrush, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
        };

        var valueText = new System.Windows.Controls.TextBlock
        {
            Text = value,
            FontSize = 26,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 10, 0, 2)
        };

        var titleText = new System.Windows.Controls.TextBlock
        {
            Text = title,
            FontSize = 13,
            Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"]
        };

        var content = new StackPanel();
        content.Children.Add(iconBadge);
        content.Children.Add(valueText);
        content.Children.Add(titleText);

        if (!string.IsNullOrEmpty(subtitle))
        {
            content.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = subtitle,
                FontSize = 11,
                Margin = new Thickness(0, 4, 0, 0),
                Foreground = accentBrush,
                TextWrapping = TextWrapping.Wrap
            });
        }

        return new Border
        {
            Background = (Brush)Application.Current.Resources["SurfaceBrush"],
            BorderBrush = (Brush)Application.Current.Resources["BorderBrush2"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 12, 12),
            Width = 190,
            MinHeight = 130,
            Child = content
        };
    }
}
