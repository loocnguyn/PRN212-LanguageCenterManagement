using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  StudentScheduleWindow — weekly timetable grid for the student.
//  CONTENTS:
//    1. Construction & LoadSchedule — sessions for the student
//    2. Semester select + week nav  — prev/next week
//    3. RenderGrid / status text    — draw via ScheduleGridRenderer
// ============================================================
public partial class StudentScheduleWindow : Window
{
    private readonly User _currentUser;
    private readonly IStudentService _studentService = new StudentService();
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();
    private readonly ISemesterService _semesterService = new SemesterService();
    private readonly ISessionService _sessionService = new SessionService();
    private readonly ISlotService _slotService = new SlotService();

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
            var student = _studentService.GetByUserId(_currentUser.Id);
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

        var slots = _slotService.GetAll();
        var rows = ScheduleGridBuilder.Build(
            _allSessions,
            _weekStart,
            slots,
            counterpartNameSelector: s => s.Class?.PrimaryTeacher?.FullName ?? "",
            statusTextSelector: s => ResolveStatusText(s));

        ScheduleGridRenderer.Render(scheduleGrid, rows, _weekStart, slots);
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

}
