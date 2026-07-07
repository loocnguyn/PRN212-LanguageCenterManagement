using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class StudentScheduleWindow : Window
{
    private static readonly string[] DayHeaders = { "MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN" };

    private readonly User _currentUser;
    private readonly IStudentService _studentService = new StudentService();
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();
    private readonly ISemesterService _semesterService = new SemesterService();
    private readonly ISessionService _sessionService = new SessionService();

    private int _studentId;
    private List<Session> _allSessions = new();
    private DateOnly _weekStart;
    private Semester? _currentSemester;
    private List<Semester> _studentSemesters = new();
    private bool _isLoadingCombo = false;

    public StudentScheduleWindow(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        Loaded += (_, _) => LoadSchedule();
    }

    private void LoadSchedule()
    {
        try
        {
            var student = _studentService.GetAll().FirstOrDefault(s => s.UserId == _currentUser.Id);
            if (student == null)
            {
                tbStudentName.Text = "No student profile linked to this account.";
                cbSemester.Visibility = Visibility.Collapsed;
                return;
            }
            _studentId = student.StudentId;
            tbStudentName.Text = $"Student: {student.FullName}";

            // Load all semesters the student has enrollments in
            var enrollments = _enrollmentService.GetByStudentId(_studentId);
            if (!enrollments.Any())
            {
                tbSummary.Text = "No enrollments found.";
                cbSemester.Visibility = Visibility.Collapsed;
                return;
            }

            var enrolledSemesterIds = enrollments
                .Where(en => en.Class != null)
                .Select(en => en.Class.SemesterId)
                .Distinct()
                .ToList();

            var allSemesters = _semesterService.GetAll();
            _studentSemesters = allSemesters
                .Where(s => enrolledSemesterIds.Contains(s.SemesterId))
                .OrderByDescending(s => s.StartDate)
                .ToList();

            if (!_studentSemesters.Any())
            {
                tbSummary.Text = "No enrollments found.";
                cbSemester.Visibility = Visibility.Collapsed;
                return;
            }

            // Show ComboBox and populate it
            cbSemester.Visibility = Visibility.Visible;
            PopulateSemesterComboBox();

            // Select default semester: active if student is enrolled, else closest by date
            var activeSemester = _semesterService.GetActive();
            var defaultSemester = activeSemester != null && _studentSemesters.Any(s => s.SemesterId == activeSemester.SemesterId)
                ? activeSemester
                : _studentSemesters.First(); // Already sorted by date (newest first)

            _isLoadingCombo = true;
            cbSemester.SelectedItem = defaultSemester;
            _isLoadingCombo = false;

            LoadSemesterSchedule(defaultSemester);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading schedule: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PopulateSemesterComboBox()
    {
        cbSemester.ItemsSource = null;
        cbSemester.ItemsSource = _studentSemesters;
        cbSemester.DisplayMemberPath = "Name";
        cbSemester.SelectedValuePath = "SemesterId";
    }

    private void CbSemester_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingCombo || cbSemester.SelectedItem is not Semester semester)
            return;

        try
        {
            LoadSemesterSchedule(semester);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading semester schedule: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadSemesterSchedule(Semester semester)
    {
        _currentSemester = semester;
        tbSemesterInfo.Text = $"Semester: {semester.Name}";

        var phase = _semesterService.GetPhase(semester);
        if (phase == Phase.LEARNING)
            _sessionService.EnsureSessionsForSemester(semester.SemesterId);

        var enrollments = _enrollmentService.GetByStudentId(_studentId)
            .Where(en => en.Class?.SemesterId == semester.SemesterId)
            .ToList();

        if (!enrollments.Any())
        {
            tbSummary.Text = "No enrollments in this semester.";
            _allSessions = new List<Session>();
        }
        else
        {
            var classIds = enrollments.Select(en => en.ClassId).ToList();
            _allSessions = _sessionService.GetByClassIds(classIds);
            tbSummary.Text = $"{_allSessions.Count} session(s) across {enrollments.Count} class(es)";
        }

        // Jump to week containing semester start date
        _weekStart = ScheduleGridBuilder.GetWeekStart(semester.StartDate);
        RenderGrid();
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
            counterpartNameSelector: s => s.Class?.Teacher?.FullName ?? "",
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
        var attendance = s.Attendances.FirstOrDefault(a => a.StudentId == _studentId);
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
        var text_block = new TextBlock
        {
            Text = text,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            FontWeight = isHeader ? FontWeights.Bold : FontWeights.Normal,
            Foreground = isHeader ? Brushes.White : Brushes.Black
        };
        border.Child = text_block;
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
