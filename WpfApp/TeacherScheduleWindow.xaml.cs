using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;
using System.Windows.Data;

namespace WpfApp;

public partial class TeacherScheduleWindow : Window
{
    private readonly User _currentUser;
    private readonly ITeacherService _teacherService = new TeacherService();
    private readonly IClassService _classService = new ClassService();
    private readonly ISemesterService _semesterService = new SemesterService();
    private readonly ISessionService _sessionService = new SessionService();

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
            // Auto-resolve teacher from logged-in user
            var teacher = _teacherService.GetAll()
                .FirstOrDefault(t => t.UserId == _currentUser.Id);

            if (teacher == null)
            {
                tbTeacherName.Text = "No teacher profile linked to this account.";
                dgSchedule.ItemsSource = null;
                return;
            }

            int teacherId = teacher.TeacherId;

            var semester = _semesterService.GetActive()
                ?? throw new InvalidOperationException("No active semester.");

            tbTeacherName.Text = $"Teacher: {teacher.FullName}";
            tbSemesterInfo.Text = $"Semester: {semester.Name}";

            // Generate sessions if in LEARNING phase
            var phase = _semesterService.GetPhase(semester);
            if (phase == Phase.LEARNING)
                _sessionService.EnsureSessionsForSemester(semester.SemesterId);

            // Get classes for this teacher in active semester
            var classes = _classService.GetBySemesterId(semester.SemesterId)
                .Where(c => c.TeacherId == teacherId)
                .ToList();

            if (!classes.Any())
            {
                dgSchedule.ItemsSource = null;
                tbSummary.Text = "No classes found for this teacher in the active semester.";
                return;
            }

            var classIds = classes.Select(c => c.ClassId).ToList();
            var sessions = _sessionService.GetByClassIds(classIds);

            var displayItems = sessions.Select(s => new ScheduleDisplay
            {
                SessionDate = s.SessionDate,
                DayName = s.SessionDate.DayOfWeek.ToString(),
                ClassName = s.Class?.Name ?? "",
                TimeDisplay = s.Schedule != null
                    ? $"{s.Schedule.StartTime:hh\\:mm} - {s.Schedule.EndTime:hh\\:mm}"
                    : "",
                RoomName = s.Class?.Classroom?.Name ?? "",
                CourseName = s.Class?.Course?.Name ?? "",
                Status = s.Status
            })
            .OrderBy(d => d.SessionDate)
            .ThenBy(d => d.TimeDisplay)
            .ToList();

            var cvs = (CollectionViewSource)FindResource("ScheduleViewSource");
            cvs.Source = displayItems;
            dgSchedule.ItemsSource = cvs.View;
            tbSummary.Text = $"{displayItems.Count} session(s) across {classes.Count} class(es)";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading schedule: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
