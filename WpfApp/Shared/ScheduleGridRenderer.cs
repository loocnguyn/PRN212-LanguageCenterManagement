using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BusinessObjects;

namespace WpfApp;

/// <summary>
/// Renders the FAP-style weekly schedule grid (rows built by ScheduleGridBuilder) as
/// styled WPF elements. Shared by StudentScheduleWindow and TeacherScheduleWindow so
/// both look identical and get visual polish in one place.
/// </summary>
public static class ScheduleGridRenderer
{
    private static readonly string[] DayHeaders = { "MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN" };

    public static void Render(Grid grid, List<ScheduleRow> rows, DateOnly weekStart)
    {
        grid.RowDefinitions.Clear();
        grid.ColumnDefinitions.Clear();
        grid.Children.Clear();

        var today = DateOnly.FromDateTime(DateTime.Today);

        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
        for (int i = 0; i < 7; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        AddHeaderCorner(grid);
        for (int d = 0; d < 7; d++)
        {
            var date = weekStart.AddDays(d);
            AddDayHeader(grid, d + 1, DayHeaders[d], date, isToday: date == today);
        }

        for (int r = 0; r < rows.Count; r++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var row = rows[r];
            var slotTime = ScheduleSlot.All.FirstOrDefault(s => s.Number == row.SlotNumber);
            AddSlotHeader(grid, r + 1, row.SlotNumber, slotTime.Start, slotTime.End);

            for (int d = 0; d < 7; d++)
            {
                var date = weekStart.AddDays(d);
                AddSessionCell(grid, r + 1, d + 1, row.Days[d], isToday: date == today);
            }
        }
    }

    private static Brush Res(string key) => (Brush)Application.Current.Resources[key];

    private static void AddHeaderCorner(Grid grid)
    {
        var border = new Border { Background = Res("PrimaryBrush"), CornerRadius = new CornerRadius(6, 0, 0, 0), Margin = new Thickness(1) };
        Grid.SetRow(border, 0);
        Grid.SetColumn(border, 0);
        grid.Children.Add(border);
    }

    private static void AddDayHeader(Grid grid, int col, string dayName, DateOnly date, bool isToday)
    {
        var border = new Border
        {
            Background = isToday ? Res("SecondaryBrush") : Res("PrimaryBrush"),
            CornerRadius = new CornerRadius(col == 7 ? 6 : 0, col == 7 ? 6 : 0, 0, 0),
            Margin = new Thickness(1),
            Padding = new Thickness(4, 5, 4, 5)
        };
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock
        {
            Text = dayName, FontWeight = FontWeights.Bold, FontSize = 11,
            Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center
        });
        panel.Children.Add(new TextBlock
        {
            Text = date.ToString("dd/MM"), FontSize = 10,
            Foreground = Brushes.White, Opacity = 0.9, HorizontalAlignment = HorizontalAlignment.Center
        });
        border.Child = panel;
        Grid.SetRow(border, 0);
        Grid.SetColumn(border, col);
        grid.Children.Add(border);
    }

    private static void AddSlotHeader(Grid grid, int row, int slotNumber, TimeOnly start, TimeOnly end)
    {
        var border = new Border
        {
            Background = Res("BackgroundBrush"),
            BorderBrush = Res("BorderBrush2"),
            BorderThickness = new Thickness(0, 0, 1, 1),
            Padding = new Thickness(3)
        };
        var panel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        panel.Children.Add(new TextBlock
        {
            Text = $"Slot {slotNumber}", FontWeight = FontWeights.SemiBold, FontSize = 10,
            Foreground = Res("TextPrimaryBrush"), HorizontalAlignment = HorizontalAlignment.Center
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"{start:hh\\:mm}-{end:hh\\:mm}", FontSize = 9,
            Foreground = Res("TextSecondaryBrush"), HorizontalAlignment = HorizontalAlignment.Center
        });
        border.Child = panel;
        Grid.SetRow(border, row);
        Grid.SetColumn(border, 0);
        grid.Children.Add(border);
    }

    private static void AddSessionCell(Grid grid, int row, int col, ScheduleCell cell, bool isToday)
    {
        var outer = new Border
        {
            Background = isToday ? Res("PrimaryLightBrush") : Res("BackgroundBrush"),
            BorderBrush = Res("BorderBrush2"),
            BorderThickness = new Thickness(0, 0, 1, 1),
            Padding = new Thickness(3),
            MinHeight = 56
        };

        if (!cell.HasSession)
        {
            outer.Child = new TextBlock
            {
                Text = "–", FontSize = 12, Foreground = Res("TextSecondaryBrush"),
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(outer, row);
            Grid.SetColumn(outer, col);
            grid.Children.Add(outer);
            return;
        }

        var card = new Border
        {
            Background = Res("SurfaceBrush"),
            BorderBrush = Res("PrimaryLightBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(6, 4, 6, 4)
        };

        var panel = new StackPanel();
        panel.Children.Add(new TextBlock
        {
            Text = cell.ClassName, FontWeight = FontWeights.Bold, FontSize = 11,
            Foreground = Res("PrimaryDarkBrush"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 1)
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"{cell.RoomName} · {cell.CounterpartName}", FontSize = 9,
            Foreground = Res("TextSecondaryBrush"), TextWrapping = TextWrapping.Wrap
        });

        var footer = new DockPanel { Margin = new Thickness(0, 3, 0, 0) };
        var timeText = new TextBlock { Text = cell.TimeDisplay, FontSize = 9, Foreground = Res("TextSecondaryBrush"), VerticalAlignment = VerticalAlignment.Center };
        DockPanel.SetDock(timeText, Dock.Right);
        footer.Children.Add(timeText);
        footer.Children.Add(StatusBadge(cell.Status));
        panel.Children.Add(footer);

        card.Child = panel;
        outer.Child = card;
        Grid.SetRow(outer, row);
        Grid.SetColumn(outer, col);
        grid.Children.Add(outer);
    }

    private static Border StatusBadge(string status)
    {
        var (bg, fg, label) = status switch
        {
            "attended" => ((Brush)Res("SecondaryLightBrush"), (Brush)Res("SecondaryBrush"), "Attended"),
            "absent" => ((Brush)Res("DangerBrush"), Brushes.White, "Absent"),
            "late" => (new SolidColorBrush(Color.FromRgb(0xFF, 0xE8, 0xCC)), new SolidColorBrush(Color.FromRgb(0xB8, 0x6A, 0x00)), "Late"),
            _ => ((Brush)Res("BorderBrush2"), (Brush)Res("TextSecondaryBrush"), "Not yet")
        };

        return new Border
        {
            Background = bg,
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(5, 0, 5, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock { Text = label, FontSize = 9, FontWeight = FontWeights.SemiBold, Foreground = fg }
        };
    }
}
