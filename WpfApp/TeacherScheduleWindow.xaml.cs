using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class TeacherScheduleWindow : Window
{
    private static readonly string[] DayHeaders = { "MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN" };

    private readonly User _currentUser;
    private readonly ITeacherService _teacherService = new TeacherService();
    private readonly IClassService _classService = new ClassService();
    private readonly ISemesterService _semesterService = new SemesterService();
    private readonly ISessionService _sessionService = new SessionService();

    private int _teacherId;
    private List<Session> _allSessions = new();
    private DateOnly _weekStart;

    public TeacherScheduleWindow(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        Loaded += (_, _) => LoadSchedule();
    }

    private void LoadSchedule()
    {
        try
        {
            var teacher = _teacherService.GetByUserId(_currentUser.Id);
            if (teacher == null)
            {
                tbTeacherName.Text = "No teacher profile linked to this account.";
                return;
            }
            _teacherId = teacher.TeacherId;

            var semester = _semesterService.GetActive()
                ?? throw new InvalidOperationException("No active semester.");

            tbTeacherName.Text = $"Teacher: {teacher.FullName}";
            tbSemesterInfo.Text = $"Semester: {semester.Name}";

            var phase = _semesterService.GetPhase(semester);
            if (phase == Phase.LEARNING)
                _sessionService.EnsureSessionsForSemester(semester.SemesterId);

            var classes = _classService.GetBySemesterId(semester.SemesterId)
                .Where(c => c.TeacherId == _teacherId)
                .ToList();

            if (!classes.Any())
            {
                tbSummary.Text = "No classes found for this teacher in the active semester.";
                _allSessions = new List<Session>();
            }
            else
            {
                var classIds = classes.Select(c => c.ClassId).ToList();
                _allSessions = _sessionService.GetByClassIds(classIds);
                tbSummary.Text = $"{_allSessions.Count} session(s) across {classes.Count} class(es)";
            }

            _weekStart = ScheduleGridBuilder.GetWeekStart(DateOnly.FromDateTime(DateTime.Today));
            RenderGrid();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading schedule: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnPrevWeek_Click(object sender, RoutedEventArgs e)
    {
        _weekStart = _weekStart.AddDays(-7);
        RenderGrid();
    }

    private void BtnNextWeek_Click(object sender, RoutedEventArgs e)
    {
        _weekStart = _weekStart.AddDays(7);
        RenderGrid();
    }

    private void RenderGrid()
    {
        var weekEnd = _weekStart.AddDays(6);
        tbWeekRange.Text = $"{_weekStart:dd/MM} to {weekEnd:dd/MM/yyyy}";

        var rows = ScheduleGridBuilder.Build(
            _allSessions,
            _weekStart,
            counterpartNameSelector: s => s.Class?.Course?.Name ?? "",
            statusTextSelector: s => ResolveStatusText(s));

        scheduleGrid.RowDefinitions.Clear();
        scheduleGrid.ColumnDefinitions.Clear();
        scheduleGrid.Children.Clear();

        scheduleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        for (int i = 0; i < 7; i++)
            scheduleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Header row: day names + dates
        scheduleGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        AddCell(0, 0, "", isHeader: true);
        for (int d = 0; d < 7; d++)
        {
            var date = _weekStart.AddDays(d);
            AddCell(0, d + 1, $"{DayHeaders[d]}\n{date:dd/MM}", isHeader: true);
        }

        // Slot rows
        for (int r = 0; r < rows.Count; r++)
        {
            scheduleGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var row = rows[r];
            AddCell(r + 1, 0, $"Slot {row.SlotNumber}", isHeader: true);

            for (int d = 0; d < 7; d++)
            {
                var cell = row.Days[d];
                AddSessionCell(r + 1, d + 1, cell);
            }
        }
    }

    private string ResolveStatusText(Session s)
    {
        var attendance = s.TeacherAttendances.FirstOrDefault(a => a.TeacherId == _teacherId);
        if (attendance == null) return "Not yet";
        return attendance.Status switch
        {
            "PRESENT" => "attended",
            "ABSENT" => "absent",
            "LATE" => "late",
            _ => attendance.Status
        };
    }

    private void AddCell(int row, int col, string text, bool isHeader)
    {
        var border = new Border
        {
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(0.5),
            Background = isHeader ? new SolidColorBrush(Color.FromRgb(0x6E, 0x8E, 0xC7)) : Brushes.White,
            Padding = new Thickness(4)
        };
        var textBlock = new TextBlock
        {
            Text = text,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            FontWeight = isHeader ? FontWeights.Bold : FontWeights.Normal,
            Foreground = isHeader ? Brushes.White : Brushes.Black
        };
        border.Child = textBlock;
        Grid.SetRow(border, row);
        Grid.SetColumn(border, col);
        scheduleGrid.Children.Add(border);
    }

    private void AddSessionCell(int row, int col, ScheduleCell cell)
    {
        var border = new Border
        {
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(0.5),
            MinHeight = 90,
            Padding = new Thickness(4)
        };

        if (!cell.HasSession)
        {
            border.Child = new TextBlock { Text = "-", TextAlignment = TextAlignment.Center, Foreground = Brushes.Gray };
        }
        else
        {
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = cell.ClassName, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap });
            panel.Children.Add(new TextBlock { Text = $"at {cell.RoomName}", FontSize = 11 });
            panel.Children.Add(new TextBlock { Text = cell.CounterpartName, FontSize = 11, TextWrapping = TextWrapping.Wrap });
            panel.Children.Add(new TextBlock
            {
                Text = $"({cell.Status})",
                FontSize = 11,
                Foreground = cell.Status switch
                {
                    "attended" => Brushes.Green,
                    "absent" => Brushes.Red,
                    "late" => Brushes.Orange,
                    _ => Brushes.Gray
                }
            });
            panel.Children.Add(new TextBlock { Text = $"({cell.TimeDisplay})", FontSize = 11, Foreground = Brushes.DarkSlateGray });
            border.Child = panel;
        }

        Grid.SetRow(border, row);
        Grid.SetColumn(border, col);
        scheduleGrid.Children.Add(border);
    }
}
