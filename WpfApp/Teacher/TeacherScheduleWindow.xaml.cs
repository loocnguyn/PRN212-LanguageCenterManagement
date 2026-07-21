using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  TeacherScheduleWindow — weekly timetable grid for the teacher.
//  CONTENTS:
//    1. Construction & LoadSchedule — sessions for the teacher
//    2. Week navigation             — prev/next week
//    3. RenderGrid / status text    — draw via ScheduleGridRenderer
// ============================================================
public partial class TeacherScheduleWindow : Window
{
    private readonly User _currentUser;
    private readonly ITeacherService _teacherService = new TeacherService();
    private readonly IClassService _classService = new ClassService();
    private readonly ISemesterService _semesterService = new SemesterService();
    private readonly ISessionService _sessionService = new SessionService();
    private readonly ISlotService _slotService = new SlotService();

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
                .Where(c => c.ClassTeachers.Any(ct => ct.TeacherId == _teacherId))
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

        var slots = _slotService.GetAll();
        var rows = ScheduleGridBuilder.Build(
            _allSessions,
            _weekStart,
            slots,
            counterpartNameSelector: s => s.Class?.Course?.Name ?? "",
            statusTextSelector: s => ResolveStatusText(s));

        ScheduleGridRenderer.Render(scheduleGrid, rows, _weekStart, slots);
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

}
